// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   Windows事件日志类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.Common
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Windows事件日志类.
    /// </summary>
    public static class WinLog
    {
        /// <summary>
        /// 写入日志.
        /// </summary>
        /// <param name="source">事件来源.</param>
        /// <param name="logName">日志名称.</param>
        /// <param name="message">事件内容.</param>
        /// <param name="type">事件类型.</param>
        /// <returns>成功返回True.</returns>
        public static bool Write(string source, string logName, string message, EventLogEntryType type)
        {
            try
            {
                //// Create the event source if it does not exist.
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource(source, logName);
                EventLog.WriteEntry(source, message, type);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
