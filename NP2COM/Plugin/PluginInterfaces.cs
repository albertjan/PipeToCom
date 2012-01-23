using System;
using System.IO;

namespace NP2COM.Plugin
{
    /// <summary>
    /// Implement this interface if you want to edit the contents of a stream before sending it on to the serial port or namedpipe.
    /// 
    /// Rx and Tx are reasoned with the serial port as a starting point.
    /// Rx: Data received from the serialport going to namedpipe.
    /// Tx: Data received from the namedpipe going to the serial port. 
    /// </summary>
    public interface IStreamingPlugin
    {
        /// <summary>
        /// Maximum chunk size to be passed to the StreamInterceptors.
        /// </summary>
        int ChunckSize { get; }

        /// <summary>
        /// Rx stream interceptor is ment to intercept data comming from the serialport
        /// going towards the namedpipe. 
        /// </summary>
        Action<Stream> RxStreamInterceptor(byte[] chunk);

        /// <summary>
        /// Rx stream interceptor is ment to intercept data comming from the serialport
        /// going towards the namedpipe. 
        /// </summary>
        Action<Stream> TxStreamInterceptor (byte[] chunk);
    }

    /// <summary>
    /// Implement this interface if you want to edit the contents of a message before sending it on to the serial port or namedpipe.
    /// 
    /// Rx and Tx are reasoned with the serial port as a starting point.
    /// Rx: Messages received from the serialport going to namedpipe.
    /// Tx: Messages received from the namedpipe going to the serial port. 
    /// </summary>
    public interface IMessagePlugin
    {
        /// <summary>
        /// Use this function to edit the contents of the message and/or to choose wether or not you want it to be sent ot the namedpipe.
        /// </summary>
        /// <param name="incommingMessage">The message that came in from the serialport</param>
        /// <param name="outgoingMessage">The message taht will be sent on towards the namedpipe</param>
        /// <returns>a boolean indicatin wether or not the message should be sent on to the namedpipe</returns>
        bool InterceptRxMessage(byte[] incommingMessage, out byte[] outgoingMessage);

        /// <summary>
        /// Use this function to edit the contents of the message and/or to choose wether or not you want it to be sent ot the serialport.
        /// </summary>
        /// <param name="incommingMessage">The message that came in from the namedpipe</param>
        /// <param name="outgoingMessage">The message taht will be sent on towards the serialport</param>
        /// <returns>a boolean indicatin wether or not the message should be sent on to the serialport</returns>
        bool InterceptTxMessage (byte[] incommingMessage, out byte[] outgoingMessage);
    }
}