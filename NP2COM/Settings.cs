using System.IO.Ports;

namespace NP2COM
{
    public class Settings
    {
        public string MachineName { get; set; }
        public string NamedPipe { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
        public int DataBits { get; set; }
    }
}