using System;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Text;
using System.Threading;
using log4net;

namespace NP2COM
{
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    public class Connection
    {
        private SerialPort serialPort;

        private NamedPipeClientStream namedPipe;

        private Thread portForwarder;

        private AutoResetEvent stopEvent;

        #region Logging

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Connection));

        private static string GetLogString(byte[] buffer, int length)
        {
            return Encoding.UTF8.GetString(buffer, 0, length).Replace("\r", "\\r").Replace("\n", "\\n");
        }

        #endregion

        #region Public functions and .ctor

        public Connection(Settings settings)
        {
            this.stopEvent = new AutoResetEvent(false);

            CurrentSettings = settings;
            IsStarted = false;
        }
        
        public void Start()
        {
            this.serialPort = 
                new SerialPort(
                    CurrentSettings.ComPort, 
                    CurrentSettings.BaudRate, 
                    CurrentSettings.Parity, 
                    CurrentSettings.DataBits,
                    CurrentSettings.StopBits)
                    {
                        RtsEnable = true,
                        DtrEnable = true,
                        Encoding = Encoding.UTF8
                     };

            this.namedPipe = 
                new NamedPipeClientStream(
                    CurrentSettings.MachineName, 
                    CurrentSettings.NamedPipe, 
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

            this.portForwarder = new Thread(this.PortForwarder);          
            portForwarder.Start();

            IsStarted = true;
        }

        public void Stop()
        {
            // Signal the port forwarder thread to stop
            this.stopEvent.Set();

            // Wait for port forwarder thread to stop
            this.portForwarder.Join();
           
            IsStarted = false;
        }

        #endregion

        #region Static Thread functions

        private void PortForwarder()
        {
            byte[] serialBuffer = new byte[this.serialPort.ReadBufferSize];
            byte[] pipeBuffer = new byte[this.serialPort.ReadBufferSize];

            IAsyncResult pipeReadResult = null;
            IAsyncResult serialReadResult = null;

            this.serialPort.Open();
            this.namedPipe.Connect();
            this.namedPipe.ReadMode = PipeTransmissionMode.Byte;

            ManualResetEvent pipeEvent = new ManualResetEvent(true);
            ManualResetEvent serialEvent = new ManualResetEvent(true);

            int waitResult;

            do
            {
                if (pipeEvent.WaitOne(0))
                {
                    pipeEvent.Reset();

                    pipeReadResult = this.namedPipe.BeginRead(
                        pipeBuffer,
                        0,
                        pipeBuffer.Length,
                        delegate(IAsyncResult namedPipeAsyncResult)
                        {
                            try
                            {
                                int actualLength = this.namedPipe.EndRead(namedPipeAsyncResult);

                                Logger.Debug("Read (NP): " + GetLogString(pipeBuffer, actualLength));

                                this.serialPort.BaseStream.BeginWrite(
                                    pipeBuffer,
                                    0,
                                    actualLength,
                                    delegate(IAsyncResult serialPortAsyncResult)
                                    {
                                        this.serialPort.BaseStream.EndWrite(serialPortAsyncResult);

                                        Logger.Debug("Wrote (CP): " + GetLogString(pipeBuffer, actualLength));
                                    },
                                    null);
                            }
                            catch (IOException)
                            {
                            }
                            catch (ObjectDisposedException)
                            {
                                // Aborted due to close
                            }
                            catch (InvalidOperationException)
                            {
                                // Aborted due to close
                            } 

                            pipeEvent.Set();
                        },
                        null);
                }

                if (serialEvent.WaitOne(0))
                {
                    serialEvent.Reset();

                    serialReadResult = this.serialPort.BaseStream.BeginRead(
                        serialBuffer,
                        0,
                        serialBuffer.Length,
                        delegate(IAsyncResult serialPortAsyncResult)
                        {
                            try
                            {
                                int actualLength = this.serialPort.BaseStream.EndRead(serialPortAsyncResult);

                                Logger.Debug("Read (CP): " + GetLogString(serialBuffer, actualLength));

                                this.namedPipe.BeginWrite(
                                    serialBuffer,
                                    0,
                                    actualLength,
                                    delegate(IAsyncResult namedPipeAsyncResult)
                                    {
                                        this.namedPipe.EndWrite(namedPipeAsyncResult);

                                        Logger.Debug("Wrote (NP): " + GetLogString(serialBuffer, actualLength));
                                    },
                                    null);
                            }
                            catch (IOException)
                            {
                            }
                            catch (ObjectDisposedException)
                            {
                                // Aborted due to close
                            }
                            catch (InvalidOperationException)
                            {
                                // Aborted due to close
                            } 

                            serialEvent.Set();
                        },
                        null);
                }

                waitResult = 
                    WaitHandle.WaitAny(
                        new WaitHandle[]
                        {
                            serialEvent,
                            pipeEvent,
                            stopEvent
                        });

            }
            while (waitResult != 2);

            this.serialPort.Close();
            this.namedPipe.Close();
        }

        #endregion

        #region Properties

        public bool IsStarted { get; private set; }

        protected Settings CurrentSettings { get; private set; }

        #endregion
    }
}