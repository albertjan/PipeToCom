Hi, 

I've created this little program to be able to have all the 
joys of a serial port in most virtual machines by Microsoft. 

It reads from the namedpipe created by the vm on the host and
writes that to a comport of your choice.

I've tested it on hyperv and vpc (win7)

So far I've sent and received faxes and i've made a dialup 
connection. Although you should turn the logging off if you 
want to enjoy the full speed of the connection.

This makes it possible in theory to virtualize your fax server.

There's also a binary release here: https://github.com/downloads/albertjan/PipeToCom/NP2COM.zip

NP2COMV is a winforms app to test and make your settings. It
allows you to save configuration files that look like this:

```xml
<?xml version="1.0"?>
<Settings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <MachineName>.</MachineName>
  <NamedPipe>comport2</NamedPipe>
  <ComPort>COM4</ComPort>
  <BaudRate>115200</BaudRate>
  <StopBits>One</StopBits>
  <Parity>None</Parity>
  <DataBits>8</DataBits>
</Settings>
```

Which you can later on use to configure your service:

NP2COMS is a windows service that reads n config files like above
and creates as many named-pipe serialport connections as you want.


Enjoy,


Having problems start here: https://github.com/albertjan/PipeToCom/issues/10
