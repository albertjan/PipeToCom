using System;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Linq;
using log4net;
using log4net.Repository.Hierarchy;

namespace NP2COM
{
    public class Connection
    {
        private static string GetLogString(byte[] buffer, int length)
        {
            return Encoding.UTF8.GetString(buffer, 0, length).Replace("\r", "\\r").Replace("\n", "\\n");
        }

        public Connection(Settings settings)
        {
            CurrentSettings = settings;
            IsStarted = false;

            

            BinaryFile = new BinaryWriter(File.Open("test",FileMode.OpenOrCreate));
            SerialPortBufferLock = new object();
            NamedPipeBufferLock = new object();
            SerialPortBuffer = new byte[65535];
            NamedPipeBuffer = new byte[65535];
            SerialPortBufferLength = 0;
            NamedPipeBufferLength = 0;
        }

        protected BinaryWriter BinaryFile { get; set; }

        protected object NamedPipeBufferLock { get; set; }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (Connection));

        public void Start()
        {
            SerialPort = new SerialPort (CurrentSettings.ComPort, CurrentSettings.BaudRate, CurrentSettings.Parity, CurrentSettings.DataBits,
                                     CurrentSettings.StopBits);
            SerialPort.RtsEnable = true;
            SerialPort.DtrEnable = true;
            SerialPort.Encoding = Encoding.UTF8;
            NamedPipe = new NamedPipeClientStream(settings.MachineName, settings.NamedPipe, PipeDirection.InOut,
                                                  PipeOptions.Asynchronous | PipeOptions.WriteThrough);

            SerialPortThread = new Thread (SerialPortRunner);
            NamedPipeThread = new Thread (NamedPipeRunner);
            SerialPort.Open();
            NamedPipe.Connect();
            NamedPipe.ReadMode = PipeTransmissionMode.Byte;
            SerialPortThread.Start(this);
            NamedPipeThread.Start(this);
            IsStarted = true;
        }

        public bool IsStarted { get; private set; }
        
        public void Stop()
        {
            SerialPortThread.Abort();
            NamedPipeThread.Abort();
            NamedPipe.Close();
            SerialPort.Close();
            IsStarted = false;
        }

        static void NamedPipeRunner(object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            var buffer = new byte[65536];
            var pppbuffer = new byte[100];
            var pppbuflen = 0;
            int numbytes = 0, wroteBufLen = 0;
            IAsyncResult iar = null, iar2 = null;
            while (true)
            {
                if (iar != null)
                    if (iar.IsCompleted)
                    {
                        numbytes = thisConnection.NamedPipe.EndRead(iar);
                        iar = null;
                    }
                
                if (numbytes > 0)
                {
                    lock (thisConnection.SerialPortBufferLock)
                    {
                        //Array.Copy(buffer, cpBuf, numbytes);
                        if (thisConnection.SerialPortBufferLength > 0) Logger.Debug("Read: " + numbytes + " from pipe. Have " + thisConnection.SerialPortBufferLength + " in buffer.");
                        //SerialPortBufferLength > 0
                        //if (buffer[0] == 0x7E || pppbuflen > 0)
                        //{
                        //    Buffer.BlockCopy(buffer, 0, pppbuffer, pppbuflen, numbytes);
                        //    if (pppbuffer.Count(b => b == 0x7E) == 2)
                        //    {
                        //        if (pppbuffer[pppbuflen] == 0x7E)
                        //            Array.Copy(pppbuffer, thisConnection.SerialPortBuffer, pppbuflen);

                        //        thisConnection.SerialPortBufferLength = pppbuflen;
                        //        pppbuflen = 0;
                        //    }
                        //}
                        //else
                        Buffer.BlockCopy(buffer, 0, thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength, numbytes);
                        //en hier wordt SerialPortBufferLength 0 aarg!
                        thisConnection.SerialPortBufferLength += numbytes;
                        //en dan heb je hier niet de juiste aantal gelezen bytes
                        numbytes = 0;
                        Logger.Debug("Read (NP): " + string.Concat(thisConnection.SerialPortBuffer.Take(thisConnection.SerialPortBufferLength).Select(b=>b.ToString("x2")).ToArray()));// .ToString("x2"))));//GetLogString(thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength));
                    }
                }

                if (thisConnection.NamedPipeBufferLength > 1)// && iar == null)
                {
                    Logger.Debug("Block 3");
                    lock (thisConnection.NamedPipeBufferLock)
                    {
                        thisConnection.NamedPipe.Write(thisConnection.NamedPipeBuffer, 0,
                                                       thisConnection.NamedPipeBufferLength);
                        Logger.Debug("Wrote (NP):" +
                                     GetLogString(thisConnection.NamedPipeBuffer, thisConnection.NamedPipeBufferLength));
                        thisConnection.NamedPipeBufferLength = 0;
                    }
                }

                //if (iar2 !=null && iar2.IsCompleted)
                //{
                //    thisConnection.NamedPipe.EndWrite(iar2);
                //    iar2 = null;
                //    thisConnection.NamedPipe.Flush();
                //}
                

                if (iar == null)
                {
                    iar = thisConnection.NamedPipe.BeginRead(buffer, 0, 65536, null, null);
                }
                Thread.Sleep(5);
            }
        }

        protected NamedPipeClientStream NamedPipe { get; set; }

        private static void SerialPortRunner(object connection)
        {
            var thisConnection = (Connection) connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            var buffer = new byte[65536];
            var numbytes = 0;
            var byteswritten = 0;

            bool write = true;
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
                        
                        if (thisConnection.NamedPipeBufferLength > 0) Logger.Debug("Read: " + numbytes + " from serialport. Have " + thisConnection.NamedPipeBufferLength + " in buffer.");

                        if (numbytes > 0)
                        {
                            Buffer.BlockCopy(buffer, 0, thisConnection.NamedPipeBuffer, thisConnection.NamedPipeBufferLength, numbytes);
                            //Array.Copy(buffer, thisConnection.NamedPipeBuffer, numbytes);
                            thisConnection.NamedPipeBufferLength += numbytes;
                            Logger.Debug("Read (CP): " +
                                         GetLogString(thisConnection.NamedPipeBuffer, thisConnection.NamedPipeBufferLength));
                            numbytes = 0;
                        }
                    }
                }

                if (thisConnection.SerialPortBufferLength > 1)
                {
                    //var mod = (thisConnection.SerialPortBuffer.Count(b => b == 0x7E)%2);
                    //if (write != (mod==0)) Logger.Info("Count(~)%2 = " + mod);
                    //write = mod == 0;
                    
                    //if (write)
                    lock (thisConnection.SerialPortBufferLock)
                    {
                        thisConnection.BinaryFile.Write(thisConnection.SerialPortBuffer, 0 , thisConnection.SerialPortBufferLength);
                        thisConnection.SerialPort.BaseStream.Write(thisConnection.SerialPortBuffer, 0, thisConnection.SerialPortBufferLength);
                        Logger.Debug("Wrote (CP): " +
                                            GetLogString(thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength));
                        thisConnection.SerialPortBuffer = new byte[65535];
                        thisConnection.SerialPortBufferLength = 0;    
                    }
                }
                
                Thread.Sleep(50);
            }
        }

        protected byte[] SerialPortBuffer { get; set; }

        protected object SerialPortBufferLock { get; set; }

        protected int SerialPortBufferLength { get; set; }

        protected int NamedPipeBufferLength { get; set; }

        protected byte[] NamedPipeBuffer { get; set; }

        protected SerialPort SerialPort { get; set; }

        protected Thread SerialPortThread { get; set; }

        protected Thread NamedPipeThread { get; set; }

        protected Settings CurrentSettings { get; private set; }
    }
}