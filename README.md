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

=========================
How to Install NP2COM
=========================
Installing: (by [@vibac](http://github.com/vibac)

Give your user full rights on the NP2COM folder after extracting it:

- Right click the NP2COM folder>properties and give your user full rights
- You can put this folder anywhere on your server, ie: C:\NP2COM\

Ensure that the NP2COMS general properties are unblocked:

- Right click the NP2COMS.exe>properties at the bottom hit the "unblock" button

Register the service NP2COMS:

- After you've installed .net 3, 3.5, 4 or 4.5 you can register NP2COMS.exe as a service by applying the following cmd

Start cmd with "run as Admin"
c:\windows\microsoft.net\framework\v4.0.30319\installutil.exe -t C:\NP2COM\NP2COMS.exe
or use run
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\InstallUtil -t C:\NP2COM\NP2COMS.exe

Create the Named pipe in the guest VM settings:

- Right click the vm from within the host's Hyper-V, select "settings", then select com1 or com2, tic "named pipe" and enter a name, ie: comport

Create a config file for NP2COMs service
- Run the NP2COMV.exe as admin to create a .n2c config file and save it in the Path\To\NP2COMS.exe, ie: C:\NP2COM\
- On the left hand side select the NamedPipe you created previously in step 4 from the drop down list, ie: \\.\pipe\comport
- Set the serial port settings to your needs, ie: baudrate 9600
- Now click "Write config file" to save it and save it in the NP2COM location, ie: C:\NP2COM\

Delete the example config file
- Delete example.n2c

Start the NP2COMS service
- Run: c:\>net start NP2COMService or go to services and start it

DONE!

Test communication
1. Stop the NP2COM service on host
2. Launch the NP2COMV.exe as admin
3. Go to the guest and launch hyper terminal or putty terminal
4. Once you connect to the setup COM port hit enter
5. Go to the NP2COMV window on the host and see if the data is written
6. Once you are done, kill the NP2COMV process then start the NP2COM service

