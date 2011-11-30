using System.ServiceProcess;

namespace NP2COMS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new NP2COMService() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
