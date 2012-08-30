// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   服务器日志操作类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.ConsoleServer
{
    using System.Diagnostics;
    using Common;

    /// <summary>
    /// 服务器日志操作类.
    /// </summary>
    internal static class ServerLog
    {
        #region Methods (1)

        // Public Methods (1) 

        /// <summary>
        /// 写入Windows事件.
        /// </summary>
        /// <param name="message">事件内容.</param>
        /// <param name="type">事件类型.</param>
        /// <returns>成功返回True.</returns>
        public static bool Write(string message, EventLogEntryType type)
        {
            return WinLog.Write("Asp.Net.Publisher.Server", "Asp.Net", message, type);
        }

        #endregion Methods
    }
}
