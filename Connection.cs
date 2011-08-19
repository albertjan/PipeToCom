using System;

public class Connection
{
	public Connection(Settings settings)
	{
    }

    static void NamedPipeThread(object connection)
    {
        var buffer = new byte[65536];
        int numbytes = 0, wroteBufLen = 0;
        IAsyncResult iar = null, iar2 = null;
        while (true)
        {
            if (iar != null)
                if (iar.IsCompleted)
                {
                    Console.WriteLine("Block 1");
                    numbytes = npcs.EndRead(iar);
                    iar = null;
                }

            if (numbytes > 0)
            {
                lock (cpBufLock)
                {
                    //Array.Copy(buffer, cpBuf, numbytes);
                    Console.WriteLine("Read: " + numbytes + " from pipe. Have " + cpBufLen + " in buffer.");
                    //cpBufLen > 0
                    Buffer.BlockCopy(buffer, 0, cpBuf, cpBufLen, numbytes);
                    //en hier wordt cpBufLen 0 aarg!
                    cpBufLen += numbytes;
                    //en dan heb je hier niet de juiste aantal gelezen bytes
                    numbytes = 0;
                    Console.WriteLine("Read (NP): " +
                                        Encoding.ASCII.GetString(cpBuf, 0, cpBufLen).Replace("\r", "\\r").Replace(
                                            "\n", "\\n"));
                }
            }
                
            if (npBufLen > 0)
            {
                Console.WriteLine("Block 3");
                if (iar2 == null)
                {
                    iar2 = npcs.BeginWrite(npBuf, 0, npBufLen,null,null);
                    wroteBufLen = npBufLen;
                    npBufLen = 0;
                }
            }

            if (iar2 !=null && iar2.IsCompleted)
            {
                npcs.EndWrite(iar2);
                iar2 = null;
                Console.WriteLine("Wrote (NP):" + Encoding.ASCII.GetString(npBuf, 0, wroteBufLen).Replace("\r", "\\r").Replace("\n", "\\n"));
                npcs.Flush();
            }
                

            if (iar == null)
            {

                Console.WriteLine("Block 2" + npcs.IsAsync);
                iar = npcs.BeginRead(buffer, 0, 65536, null, null);
            }
            Thread.Sleep(100);
        }
    }

    private static void ComportThread(object connection)
    {
        var buffer = new byte[65536];
        var buflen = 0;
           
        while (true)
        {
            if (cp.BytesToRead > 1)
                while (cp.BytesToRead != 0)
                {
                    //var tmpByte = 
                    //if ((char)tmpByte == '\r' && cp.BytesToRead) 
                    buffer[buflen] = (byte)cp.ReadByte(); 
                    buflen++;
                }


            if (buflen > 0)
            {
                Array.Copy(buffer, npBuf, buflen);
                npBufLen = buflen;
                Console.WriteLine("Read (CP): " + Encoding.ASCII.GetString(npBuf, 0, npBufLen).Replace("\r", "\\r").Replace("\n", "\\n"));
                buflen = 0;
            }

            if (cpBufLen > 0)
            {
                lock (cpBufLock)
                {
                    cp.Write(cpBuf, 0, cpBufLen);
                    Console.WriteLine("Wrote (CP): " + Encoding.ASCII.GetString(cpBuf, 0, cpBufLen).Replace("\r", "\\r").Replace("\n", "\\n"));
                    cpBufLen = 0;    
                }
            }
                
            Thread.Sleep(100);
        }
    }
}
