using System;
using System.IO;
using NP2COM.Plugin;

namespace NP2COMPluginExampleCS
{
    public class ExampleStreamingPluginImplementation : IStreamingPlugin
    {
        /// <summary>
        /// Use a 32 byte chunksize for example 
        /// </summary>
        public int ChunckSize { get { return 32*1024; } }

        public Action<Stream> RxStreamInterceptor(byte[] chunk)
        {
            return stream => stream.Write(chunk, 0, chunk.Length);
        }

        public Action<Stream> TxStreamInterceptor(byte[] chunk)
        {
            return stream => stream.Write (chunk, 0, chunk.Length);
        }
    }

    public class ExampleMessagePluginImplementation : IMessagePlugin
    {
        /// <summary>
        /// Intercepts a message comming from the serialport bound for the named pipe.
        /// In this example the message just gets copied, and pipetocom is told to send 
        /// it on to the namedpipe.
        /// </summary>
        /// <param name="incommingMessage">Message from the serialport</param>
        /// <param name="outgoingMessage">Message to the namedpipe</param>
        /// <returns></returns>
        public bool InterceptRxMessage(byte[] incommingMessage, out byte[] outgoingMessage)
        {
            // copy the incomming message to the outgoing.
            outgoingMessage = incommingMessage;
            // return true to tell pipetocom to send the message to the namedpipe.
            return true;
        }

        public bool InterceptTxMessage(byte[] incommingMessage, out byte[] outgoingMessage)
        {
            outgoingMessage = incommingMessage;
            return true;
        }
    }
}