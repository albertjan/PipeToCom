Imports System.IO
Imports NP2COM.Plugin

Public Class ExampleStreamedPluginImplementation
    Implements IStreamingPlugin

    Public ReadOnly Property ChunckSize() As Integer Implements IStreamingPlugin.ChunckSize
        Get
            Return 32 * 1024
        End Get
    End Property

    Public Function RxStreamInterceptor(ByVal chunk As Byte()) As Action(Of Stream) Implements IStreamingPlugin.RxStreamInterceptor
        Return Function(stream As Stream) stream.Write(chunk, 0, chunk.Length)
    End Function

    Public Function TxStreamInterceptor(ByVal chunk As Byte()) As Action(Of Stream) Implements IStreamingPlugin.TxStreamInterceptor
        Return Function(stream As Stream) stream.Write(chunk, 0, chunk.Length)
    End Function
End Class