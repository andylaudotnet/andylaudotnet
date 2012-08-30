using System.ServiceProcess;

namespace Asp.Net.Publisher.WindowsService
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        private static void Main()
        {
            ServiceBase.Run(new[] { new PublisherService() });
        }
    }
}
