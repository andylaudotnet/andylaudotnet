// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   服务器主类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.ConsoleServer
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using Common;

    /// <summary>
    /// 服务器主类.
    /// </summary>
    public class Program
    {
        #region Fields (3)

        public HeartServer HeartSvr;
        public PublishServer PublishSvr;
        /// <summary>
        /// 启动参数.
        /// </summary>
        public StartArgsEntity StartArgs;

        #endregion Fields

        #region Methods (2)

        // Public Methods (1) 

        public void Run(string[] args)
        {
            Console.WriteLine(@"
***************************************************************
* {0}
* CLR Runtime Version: {1}
***************************************************************
", Assembly.GetExecutingAssembly().ToString(), Assembly.GetExecutingAssembly().ImageRuntimeVersion);
            // 解析启动参数
            var argsParseError = StartArgsEntity.ParseArgs(args, out StartArgs);
            if (argsParseError != null)
            {
                if (argsParseError.Length > 0)
                {
                    // 写入日志
                    ServerLog.Write("程序启动失败 " + argsParseError, EventLogEntryType.Error);
                    Util.Print2ConsoleLine(argsParseError);
                }

                Console.WriteLine(
                    @"
启动参数:
[0] = 鉴权密钥
[1] = 绑定IP地址(""any""表示绑定所有IP),主服务器端口,心跳服务器端口
[2] = 连接队列长度
[3] = 网络缓冲区大小
[4] = 上传的压缩包路径
[5] = 网站根目录路径
[6] = 7Z.EXE文件路径
[7] = 网站备份路径");
                Console.Write("按下回车键继续...");
                Console.Read();
                ServerLog.Write("程序已退出.", EventLogEntryType.Information);
                return;
            }

            // 启动主服务器
            new Thread((PublishSvr = new PublishServer(this)).PowerOn) { IsBackground = true }.Start();

            // 启动心跳服务器
            new Thread((HeartSvr = new HeartServer(this)).PowerOn) { IsBackground = true }.Start();

            string cmd;
            while ((cmd = Console.ReadLine()) == null || cmd.ToLower() != "exit")
            {
                if (cmd == null)
                    Thread.Sleep(Timeout.Infinite);
            }
            ServerLog.Write("程序已退出.", EventLogEntryType.Information);
        }
        // Private Methods (1) 

        /// <summary>
        /// 程序入口
        /// <param name="args">
        /// 启动参数:
        /// [0] = 鉴权密钥
        /// [1] = 绑定IP地址("any"表示绑定所有IP),主服务器端口,心跳服务器端口
        /// [2] = 连接队列长度
        /// [3] = 网络缓冲区大小
        /// [4] = 上传的压缩包路径
        /// [5] = 网站根目录路径
        /// [6] = 7Z.EXE文件路径
        /// [7] = 网站备份路径
        /// </param>
        /// </summary>
        private static void Main(string[] args)
        {
            new Program().Run(args);
        }

        #endregion Methods
    }
}