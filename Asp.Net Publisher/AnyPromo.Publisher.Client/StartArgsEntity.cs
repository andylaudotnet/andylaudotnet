// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   启动参数实体类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.Client
{
    using System;
    using System.IO;

    /// <summary>
    /// 启动参数实体类.
    /// </summary>
    internal class StartArgsEntity
    {
        #region Fields (8)

        /// <summary>
        /// 鉴权密钥.
        /// </summary>
        public string AuthKey { get; private set; }

        /// <summary>
        /// 网络缓冲区大小.
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// 连接主服务器IP节点.
        /// </summary>
        public string[] ConnectPoint { get; private set; }

        /// <summary>
        /// 连接心跳服务器IP节点
        /// </summary>
        public string[] HeartbeatPoint { get; private set; }

        /// <summary>
        /// 服务器是否备份网站.
        /// </summary>
        public bool IsBackup { get; private set; }

        /// <summary>
        /// 是否总是覆盖服务器已存在的压缩包.
        /// </summary>
        public bool IsFileOverriteAlway { get; private set; }

        /// <summary>
        /// 是否重启服务器IIS
        /// </summary>
        public bool IsResetIIS { get; private set; }

        /// <summary>
        /// 网站本地项目目录.
        /// </summary>
        public string ProjectFolder { get; private set; }

        /// <summary>
        /// 本地7z.exe文件路径.
        /// </summary>
        public string SevenZipExe { get; private set; }

        /// <summary>
        /// 传递给7z.exe的参数.
        /// </summary>
        public string SevenZipArgs { get; private set; }

        #endregion Fields

        #region Methods (1)

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
            if (args == null || args.Length < 8)
            {
                return "缺少启动参数.";
            }

            // [0] = 鉴权密钥
            startArgs = new StartArgsEntity { AuthKey = args[0] };

            // [1] = 服务器IP地址,主服务器端口,心跳服务器端口
            var ipPort = args[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (ipPort.Length != 3)
                return "args[1]: 缺少端口.";
            if (ipPort[0].Length == 0)
                return "args[1]: IP地址错误.";
            int publishPort;
            int heartPort;
            if (!int.TryParse(ipPort[1], out publishPort) || publishPort > ushort.MaxValue)
                return "args[1]: 端口错误.";
            if (!int.TryParse(ipPort[2], out heartPort) || heartPort > ushort.MaxValue)
                return "args[1]: 端口错误.";
            startArgs.ConnectPoint = new[] { ipPort[0], ipPort[1] };
            startArgs.HeartbeatPoint = new[] { ipPort[0], ipPort[2] };

            // [2] = 缓冲区大小
            int bufferSize;
            if (!int.TryParse(args[2], out bufferSize))
                return "args[2]: 缓冲区大小错误.";
            startArgs.BufferSize = bufferSize;

            // [3] = ShoppingApp项目目录
            if (!Directory.Exists(args[3]))
                return "args[3]: Asp.Net工程目录不存在.";
            startArgs.ProjectFolder = args[3];

            // [4] = 7z.exe文件路径
            if (!File.Exists(args[4]))
                return "args[4]: 7z.exe文件路径不存在.";
            startArgs.SevenZipExe = args[4];

            // [5] = 是否备份
            bool isBackup;
            if (!bool.TryParse(args[5], out isBackup))
                return "args[5]: 是否备份错误.";
            startArgs.IsBackup = isBackup;

            // [6] = 是否重启IIS
            bool isResetIIS;
            if (!bool.TryParse(args[6], out isResetIIS))
                return "args[6]: 是否重启IIS.";
            startArgs.IsResetIIS = isResetIIS;

            // [7] = 是否总是覆盖服务器上的压缩包
            bool isFileOverriteAlway;
            if (!bool.TryParse(args[7], out isFileOverriteAlway))
                return "args[7]: 是否总是覆盖服务器上的压缩包.";
            startArgs.IsFileOverriteAlway = isFileOverriteAlway;

            // [8] = 传递给7z.exe的参数[可选].
            if (args.Length >= 9)
            {
                string sevenZipArgs = "";
                for (int i = 8; i < args.Length; i++)
                {
                    sevenZipArgs =sevenZipArgs+args[i] + " ";
                }
                startArgs.SevenZipArgs = sevenZipArgs.Trim(); ; 
            }
          

            return null;
        }

        #endregion Methods
    }
}
