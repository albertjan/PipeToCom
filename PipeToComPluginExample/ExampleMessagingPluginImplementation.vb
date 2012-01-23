Imports System.Runtime.InteropServices
Imports NP2COM.Plugin

Public Class ExampleMessagingPluginImplementation
    Implements IMessagePlugin

    Public Function InterceptRxMessage(ByVal incommingMessage As Byte(), <Out> ByRef outgoingMessage As Byte()) As Boolean Implements IMessagePlugin.InterceptRxMessage
        outgoingMessage = incommingMessage
        Return True
    End Function

    Public Function InterceptTxMessage(ByVal incommingMessage As Byte(), <Out> ByRef outgoingMessage As Byte()) As Boolean Implements IMessagePlugin.InterceptTxMessage
        outgoingMessage = incommingMessage
        Return True
    End Function
End Class
