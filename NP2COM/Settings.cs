using System.IO.Ports;
using log4net;

namespace NP2COM
{
    public class Settings
    {
        public ILog Logger { get; set; }
        public string MachineName { get; set; }
        public string NamedPipe { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
        public int DataBits { get; set; }
    }
}