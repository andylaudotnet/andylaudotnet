// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   客户端请求报文头的目标
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.Common
{
    /// <summary>
    /// 客户端请求报文头的目标
    /// </summary>
    public enum RequestHeaderAction : byte
    {
        /// <summary>
        /// 等候指令(初始状态).
        /// </summary>
        WaitAction = 0,

        /// <summary>
        /// 鉴权.
        /// </summary>
        Authentication = 1,

        /// <summary>
        /// 发送文件,(询问模式)
        /// </summary>
        SendFileUnknow = 2,

        /// <summary>
        /// 发送文件,(追加模式)
        /// </summary>
        SendFileAppend = 3,

        /// <summary>
        /// 发送文件,(覆盖模式)
        /// </summary>
        SendFileCreate = 4,

        /// <summary>
        /// 检查文件的Md5哈希值
        /// </summary>
        VerifyFileMd5 = 5,

        /// <summary>
        /// 备份网站
        /// </summary>
        Backup = 6,

        /// <summary>
        /// 停止IIS
        /// </summary>
        StopIIS = 7,

        /// <summary>
        /// 启动IIS
        /// </summary>
        StartIIS = 8,

        /// <summary>
        /// 更新网站
        /// </summary>
        Update = 9
    }
}
