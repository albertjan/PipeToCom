using System;
using System.IO.Ports;

namespace NP2COM
{
    class Program
    {
        static void Main(string[] args)
        {
            var con =
                new Connection(new Settings()
                                   {
                                       BaudRate = 115200,
                                       ComPort = "COM3",
                                       DataBits = 8,
                                       MachineName = ".",
                                       NamedPipe = "comport",
                                       Parity = Parity.None,
                                       StopBits = StopBits.One
                                   });
            con.Start();
            Console.ReadLine();
            con.Stop();
        }
    }
}
