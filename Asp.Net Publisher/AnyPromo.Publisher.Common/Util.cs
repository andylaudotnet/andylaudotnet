// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   通用函数类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.Common
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// 通用函数类.
    /// </summary>
    public static class Util
    {
        #region Methods (4)

        // Public Methods (4) 

        /// <summary>
        /// 获取文件的MD5哈希值.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        /// <returns>
        /// 长度32的MD5字符串.
        /// </returns>
        public static string GetFileMd5(string file)
        {
            var md5Provider = new MD5CryptoServiceProvider();
            using (var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return BitConverter.ToString(md5Provider.ComputeHash(s)).Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// 打印到控制台.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public static void Print2Console(string message)
        {
            Console.Write("{0}\t{1}", DateTime.Now.ToString("HH:mm:ss"), message);
        }

        /// <summary>
        /// 打印到控制台(换行).
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public static void Print2ConsoleLine(string message)
        {
            Print2Console(message);
            Console.WriteLine();
        }

        /// <summary>
        /// 写磁盘文件.
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="mode">文件操作模式</param>
        /// <param name="buffer">缓冲区大小</param>
        /// <param name="length">真实写入长度</param>
        public static void WriteFile(string path, FileMode mode, byte[] buffer, int length)
        {
            using (var s = File.Open(path, mode, FileAccess.Write, FileShare.Read))
            {
                s.Write(buffer, 0, length);
            }
        }

        #endregion Methods
    }
}
