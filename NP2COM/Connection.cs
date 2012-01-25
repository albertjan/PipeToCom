using System;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Text;
using System.Threading;
using log4net;

namespace NP2COM
{
    public class Connection
    {
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
            CurrentSettings = settings;
            IsStarted = false;
            SerialPortBufferLock = new object();
            NamedPipeBufferLock = new object();
            SerialPortBuffer = new byte[65535];
            NamedPipeBuffer = new byte[65535];
            SerialPortBufferLength = 0;
            NamedPipeBufferLength = 0;            
        }
        
        public void Start()
        {
            SerialPort = new SerialPort (CurrentSettings.ComPort, CurrentSettings.BaudRate, CurrentSettings.Parity, CurrentSettings.DataBits,
                                     CurrentSettings.StopBits)
                             {
                                 RtsEnable = true,
                                 DtrEnable = true,
                                 Encoding = Encoding.UTF8
                             };
            NamedPipe = new NamedPipeClientStream(CurrentSettings.MachineName, CurrentSettings.NamedPipe, PipeDirection.InOut,
                                                  PipeOptions.Asynchronous);

            SerialPortThread = new Thread (SerialPortRunner);
            NamedPipeThread = new Thread (NamedPipeRunner);
            NamedPipeCopyThread = new Thread(NamedPipeCopier);

            NamedPipeBufferstream = new BufferedStream(NamedPipe);

            SerialPort.Open();
            NamedPipe.Connect();
            NamedPipe.ReadMode = PipeTransmissionMode.Byte;

            SerialPortThread.Start(this);
            NamedPipeThread.Start(this);
            NamedPipeCopyThread.Start(this);
            IsStarted = true;
        }

        public void Stop()
        {
            SerialPortThread.Abort();
            NamedPipeThread.Abort();
            NamedPipe.Close();
            SerialPort.Close();
            IsStarted = false;
        }

        #endregion

        #region Static Thread functions

        //make sure EVERY byte is read from the namedpipe to keep the UART buffer empty. and put into the local buffer.
        private static void NamedpipeCopier(object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException ("connection must be of Type Connection!");
        }

        //read from serialport, buffer, edit, write to namedpipe
        private static void RxStreamCopier(object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException ("connection must be of Type Connection!");
        }

        //readfrom the buffer (created by the namedpipe copier), edit, write
        private static void TxStreamCopier (object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException ("connection must be of Type Connection!");
        }


        private static void NamedPipeCopier(object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            
            var useTermChar = thisConnection.CurrentSettings.MessageTerminationCharacter != null;

            var localBufferSize = 0;
            
            if (useTermChar)
            {
                var termChar = thisConnection.CurrentSettings.MessageTerminationCharacter.Value;
                //size should probably come from settings. == maxMessageSize
                var localBuffer = new byte[73];
                while (true)
                {
                    var read = thisConnection.NamedPipeBufferstream.ReadByte();
                    //if this is true the namedpipe connection has failed.
                    if (read == -1) break;
                    if ((char)read != termChar)
                    {
                        //add byte to the localbuffer if it's not the termCharacter.
                        localBuffer[localBufferSize] = (byte)read;
                        localBufferSize++;
                    }
                    else
                    {
                        //add termchar to the buffer
                        localBuffer[localBufferSize] = (byte)read;
                        localBufferSize++;
                        //locl the serialport buffer so this thread is the only one allowed to write to it.
                        lock (thisConnection.SerialPortBufferLock)
                        {
                            //copy the local buffer to the serialportbuffer
                            Array.Copy(localBuffer, 0, thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength, localBufferSize);
                            thisConnection.SerialPortBufferLength = thisConnection.SerialPortBufferLength + localBufferSize;
                            //"empty" the localbuffer (start again at the beginning);
                            localBufferSize = 0;                            
                        }
                    }
                }
            }
            else 
            {
                while (true)
                {
                    var read = thisConnection.NamedPipeBufferstream.ReadByte();
                    //if this is true the namedpipe connection has failed.
                    if (read == -1) break;
                    lock (thisConnection.SerialPortBufferLock)
                    {
                        //Logger.Debug("Read byte from NP");
                        thisConnection.SerialPortBuffer[thisConnection.SerialPortBufferLength] = (byte)read;
                        thisConnection.SerialPortBufferLength++;
                    }
                }
            }
        }

        private static void NamedPipeRunner(object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            while (true)
            {
                if (thisConnection.NamedPipeBufferLength > 0)
                {
                    lock (thisConnection.NamedPipeBufferLock)
                    {
                        thisConnection.NamedPipe.Write(thisConnection.NamedPipeBuffer, 0,
                                                       thisConnection.NamedPipeBufferLength);
                        Logger.Debug("Wrote (NP):" +
                                     GetLogString(thisConnection.NamedPipeBuffer, thisConnection.NamedPipeBufferLength));
                        thisConnection.NamedPipeBufferLength = 0;
                    }
                }
                Thread.Sleep(5);
            }
        }

        private static void SerialPortRunner(object connection)
        {
            var thisConnection = (Connection) connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            var buffer = new byte[65536];
            var numbytes = 0;
            
            while (true)
            {

                if (thisConnection.SerialPort.BytesToRead > 0)
                {
                    lock (thisConnection.NamedPipeBufferLock)
                    {
                        while (thisConnection.SerialPort.BytesToRead != 0)
                        {
                            buffer[numbytes] = (byte) thisConnection.SerialPort.ReadByte();
                            numbytes++;
                        }
                        
                        if (numbytes > 0)
                        {
                            Buffer.BlockCopy(buffer, 0, thisConnection.NamedPipeBuffer, thisConnection.NamedPipeBufferLength, numbytes);
                            thisConnection.NamedPipeBufferLength += numbytes;
                            Logger.Debug("Read (CP): " +
                                        GetLogString(thisConnection.NamedPipeBuffer, thisConnection.NamedPipeBufferLength));
                            numbytes = 0;
                        }
                    }
                }

                if (thisConnection.SerialPortBufferLength > 0)
                {
                    lock (thisConnection.SerialPortBufferLock)
                    {
                        thisConnection.SerialPort.BaseStream.Write(thisConnection.SerialPortBuffer, 0, thisConnection.SerialPortBufferLength);
                        Logger.Debug("Wrote (CP): " +
                                            GetLogString(thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength));
                        thisConnection.SerialPortBufferLength = 0;
                    }
                }
                
                Thread.Sleep(5);
            }
        }

        #endregion

        #region Properties

        public bool IsStarted { get; private set; }

        protected byte[] SerialPortBuffer { get; set; }

        protected object SerialPortBufferLock { get; set; }

        protected int SerialPortBufferLength { get; set; }

        protected int NamedPipeBufferLength { get; set; }

        protected byte[] NamedPipeBuffer { get; set; }

        protected SerialPort SerialPort { get; set; }

        protected Thread SerialPortThread { get; set; }

        protected Thread NamedPipeThread { get; set; }

        protected Thread NamedPipeCopyThread { get; set; }

        protected object NamedPipeBufferLock { get; set; }

        protected NamedPipeClientStream NamedPipe { get; set; }

        protected Settings CurrentSettings { get; private set; }

        protected BufferedStream NamedPipeBufferstream { get; set; }

        #endregion
    }
}