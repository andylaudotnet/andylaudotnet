// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   启动参数实体类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.ConsoleServer
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// 启动参数实体类.
    /// </summary>
    public class StartArgsEntity
    {
        #region Properties (9)

        /// <summary>
        /// 鉴权密钥
        /// </summary>
        public string AuthKey { get; private set; }

        /// <summary>
        /// 网站备份目录.
        /// </summary>
        public string BackupFolder { get; private set; }

        /// <summary>
        /// Heartbeat服务IP节点.
        /// </summary>
        public IPEndPoint BindPointHeart { get; private set; }

        /// <summary>
        /// Publish服务IP节点.
        /// </summary>
        public IPEndPoint BindPointPublish { get; private set; }

        /// <summary>
        /// 网络缓冲区大小.
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// 连接队列长度.
        /// </summary>
        public int QueueLimit { get; private set; }

        /// <summary>
        /// 7z.exe文件路.
        /// </summary>
        public string SevenZipExe { get; private set; }

        /// <summary>
        /// 上传的压缩包路径.
        /// </summary>
        public string UploadFile { get; private set; }

        /// <summary>
        /// 网站根目录.
        /// </summary>
        public string WebRoot { get; private set; }

        #endregion Properties

        #region Methods (1)

        // Public Methods (1) 

        // Public Methods (1) 
        /// <summary>
        /// 解析启动参数.
        /// </summary>
        /// <param name="args">参数列表.</param>
        /// <param name="startArgs">启动参数实体对象.</param>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        public static string ParseArgs(string[] args, out StartArgsEntity startArgs)
        {
            startArgs = null;
            if (args == null || args.Length != 8)
                return "缺少启动参数.";

            // [0] = 鉴权密钥
            startArgs = new StartArgsEntity { AuthKey = args[0] };

            // [1] = 绑定IP地址("any"表示绑定所有IP),Publish服务器端口,心跳服务器端口
            var ipPort = args[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (ipPort.Length != 3)
                return "args[1]: 缺少端口.";
            IPAddress ip;
            if (!IPAddress.TryParse(ipPort[0], out ip))
            {
                if (ipPort[0].ToLower() == "any")
                    ip = IPAddress.Any;
                else
                    return "args[1]: IP地址错误.";
            }
            int publishPort;
            int heartPort;
            if (!int.TryParse(ipPort[1], out publishPort) || publishPort > ushort.MaxValue)
                return "args[1]: 端口错误.";
            if (!int.TryParse(ipPort[2], out heartPort) || heartPort > ushort.MaxValue)
                return "args[1]: 端口错误.";
            startArgs.BindPointPublish = new IPEndPoint(ip, publishPort);
            startArgs.BindPointHeart = new IPEndPoint(ip, heartPort);

            // [2] = 连接队列长度
            int queueLimit;
            if (!int.TryParse(args[2], out queueLimit))
                return "args[2]: 连接队列长度错误.";
            startArgs.QueueLimit = queueLimit;

            // [3] = 缓冲区大小
            int bufferSize;
            if (!int.TryParse(args[3], out bufferSize))
                return "args[3]: 缓冲区大小错误.";
            startArgs.BufferSize = bufferSize;

            // [4] = 上传的压缩包路径
            if (args[4].Length == 0)
                return "args[4]: 上传的压缩包路径不能为空.";
            startArgs.UploadFile = args[4];

            // [5] = 网站根目录路径
            if (!Directory.Exists(args[5]))
                return "args[5]: 网站根目录路径不存在.";
            startArgs.WebRoot = args[5];

            // [6] = 7z.exe文件路径
            if (!File.Exists(args[6]))
                return "args[6]: 7z.exe文件不存在.";
            startArgs.SevenZipExe = args[6];

            // [7] = 网站备份路径
            if (!Directory.Exists(args[7]))
                return "args[7]: 网站备份路径不存在.";
            startArgs.BackupFolder = args[7];

            return null;
        }

        #endregion Methods
    }
}
