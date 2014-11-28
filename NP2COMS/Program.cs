using System.ServiceProcess;
using System.Threading;

namespace NP2COMS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        public static int Main(string[] args)
        {
            NP2COMService service = new NP2COMService();

            ServiceBase.Run(new NP2COMService());

            return 0;
        }
    }
}
