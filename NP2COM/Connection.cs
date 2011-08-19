using System;
using System.IO.Pipes;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace NP2COM
{
    public class Connection
    {
        private static string GetLogString(byte[] buffer, int length)
        {
            return Encoding.ASCII.GetString(buffer, 0, length).Replace("\r", "\\r").Replace("\n", "\\n");
        }

        public Connection(Settings settings)
        {
            SerialPort = new SerialPort(settings.ComPort, settings.BaudRate, settings.Parity, settings.DataBits,
                                        settings.StopBits);
            NamedPipe = new NamedPipeClientStream(settings.MachineName, settings.NamedPipe, PipeDirection.InOut,
                                                  PipeOptions.Asynchronous);

            SerialPortThread = new Thread(SerialPortRunner);
            NamedPipeThread = new Thread(NamedPipeRunner);
        }

        public void Start()
        {
            SerialPort.Open();
            NamedPipe.Connect();
            SerialPortThread.Start();
            NamedPipeThread.Start();
        }

        public void Stop()
        {
            SerialPortThread.Abort();
            NamedPipeThread.Abort();
            NamedPipe.Close();
            SerialPort.Close();
        }

        static void NamedPipeRunner(object connection)
        {
            var thisConnection = (Connection)connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            var buffer = new byte[65536];
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
                        Console.WriteLine("Read: " + numbytes + " from pipe. Have " + thisConnection.SerialPortBufferLength + " in buffer.");
                        //SerialPortBufferLength > 0
                        Buffer.BlockCopy(buffer, 0, thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength, numbytes);
                        //en hier wordt SerialPortBufferLength 0 aarg!
                        thisConnection.SerialPortBufferLength += numbytes;
                        //en dan heb je hier niet de juiste aantal gelezen bytes
                        numbytes = 0;
                        Console.WriteLine("Read (NP): " + GetLogString(thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength));
                    }
                }

                if (thisConnection.NamePipeBufferLength > 0)
                {
                    Console.WriteLine("Block 3");
                    if (iar2 == null)
                    {
                        iar2 = thisConnection.NamedPipe.BeginWrite(thisConnection.NamedPipeBuffer, 0, thisConnection.NamePipeBufferLength, null, null);
                        wroteBufLen = thisConnection.NamePipeBufferLength;
                        thisConnection.NamePipeBufferLength = 0;
                    }
                }

                if (iar2 !=null && iar2.IsCompleted)
                {
                    thisConnection.NamedPipe.EndWrite(iar2);
                    iar2 = null;
                    Console.WriteLine("Wrote (NP):" + GetLogString(thisConnection.NamedPipeBuffer, wroteBufLen));
                    thisConnection.NamedPipe.Flush();
                }
                

                if (iar == null)
                {
                    iar = thisConnection.NamedPipe.BeginRead(buffer, 0, 65536, null, null);
                }
                Thread.Sleep(100);
            }
        }

        protected NamedPipeClientStream NamedPipe { get; set; }

        private static void SerialPortRunner(object connection)
        {
            var thisConnection = (Connection) connection;
            if (thisConnection == null) throw new ArgumentException("connection must be of Type Connection!");
            var buffer = new byte[65536];
            var numbytes = 0;
           
            while (true)
            {
                if (thisConnection.SerialPort.BytesToRead > 1)
                    while (thisConnection.SerialPort.BytesToRead != 0)
                    {
                        buffer[numbytes] = (byte)thisConnection.SerialPort.ReadByte(); 
                        numbytes++;
                    }


                if (numbytes > 0)
                {
                    Array.Copy(buffer, thisConnection.NamedPipeBuffer, numbytes);
                    thisConnection.NamePipeBufferLength = numbytes;
                    Console.WriteLine("Read (CP): " + GetLogString(thisConnection.NamedPipeBuffer, thisConnection.NamePipeBufferLength));
                    numbytes = 0;
                }

                if (thisConnection.SerialPortBufferLength > 0)
                {
                    lock (thisConnection.SerialPortBufferLock)
                    {
                        thisConnection.SerialPort.Write(thisConnection.SerialPortBuffer, 0, thisConnection.SerialPortBufferLength);
                        Console.WriteLine("Wrote (CP): " +
                                          GetLogString(thisConnection.SerialPortBuffer, thisConnection.SerialPortBufferLength));
                        thisConnection.SerialPortBufferLength = 0;    
                    }
                }
                
                Thread.Sleep(100);
            }
        }

        protected byte[] SerialPortBuffer { get; set; }

        protected object SerialPortBufferLock { get; set; }

        protected int SerialPortBufferLength { get; set; }

        protected int NamePipeBufferLength { get; set; }

        protected byte[] NamedPipeBuffer { get; set; }

        protected SerialPort SerialPort { get; set; }

        protected Thread SerialPortThread { get; set; }

        protected Thread NamedPipeThread { get; set; }
    }
}