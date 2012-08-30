using System;
using System.ServiceProcess;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Text;

namespace Asp.Net.Publisher.WindowsService
{
    public partial class PublisherService : ServiceBase
    {
        public PublisherService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            args = File.ReadAllLines(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\config.txt", Encoding.UTF8);
            const int serverArgsCount = 8;
            for (var i = 0; i != args.Length / serverArgsCount; ++i)
            {
                var serverArgs = new string[serverArgsCount];
                Array.Copy(args, serverArgsCount * i, serverArgs, 0, serverArgsCount);
                new Thread(new ParameterizedThreadStart((obj) => new Asp.Net.Publisher.ConsoleServer.Program().Run(obj as string[]))) { IsBackground = true }.Start(serverArgs);
            }

        }

        protected override void OnStop()
        {
        }
    }
}
