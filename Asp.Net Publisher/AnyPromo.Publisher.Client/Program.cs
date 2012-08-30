// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   客户端主类.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Asp.Net.Publisher.Client
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Common;
    using System.Reflection;

    /// <summary>
    /// 客户端主类.
    /// </summary>
    internal static class Program
    {
        #region Fields (5)

        /// <summary>
        /// 心跳循环指示器
        /// </summary>
        private static bool IsHeartbeatRunning;
        private static int LastOutLen = 0;
        /// <summary>
        /// 数据接收缓冲区.
        /// </summary>
        private static byte[] ReceiveBuffer;
        /// <summary>
        /// 数据发送缓冲区.
        /// </summary>
        private static byte[] SendBuffer;
        /// <summary>
        /// 启动参数.
        /// </summary>
        private static StartArgsEntity StartArgs;

        #endregion Fields

        #region Methods (12)

        // Private Methods (12) 

        /// <summary>
        /// 鉴权.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="userId">用户id</param>
        /// <returns>
        /// 成功返回NULL, 失败返回错误信息.
        /// </returns>
        private static string Authenticate(Socket client, out string userId)
        {
            userId = null;
            string error;
            ResponseMessage repMsg = null;
            try
            {
                // 发送报头
                new RequestHeader { Action = RequestHeaderAction.Authentication, BodyLength = (uint)StartArgs.AuthKey.Length }.SendToServer(client);

                // 接收, 解析应答
                error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                if (error != null)
                    return error;
                if (repMsg.Action == ResponseMessageAction.Stop)
                    return repMsg.Message;

                // 成功, 继续发送报身
                var bodyBytes = Encoding.UTF8.GetBytes(StartArgs.AuthKey);
                client.Send(bodyBytes);

                // 接收, 解析应答
                error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                if (error != null)
                    return error;
                if (repMsg.Action == ResponseMessageAction.Stop)
                    return repMsg.Message;
            }
            catch (Exception exp)
            {
                error = exp.Message;
            }

            if (repMsg != null && repMsg.Action == ResponseMessageAction.Go) userId = repMsg.Message;
            return error;
        }

        /// <summary>
        /// 压缩项目
        /// </summary>
        /// <returns>
        /// 成功返回NULL, 失败返回错误信息.
        /// </returns>
        private static string CompressProject()
        {
            var compressFile = string.Format("{0}\\ShoppingApp.7z", Environment.GetEnvironmentVariable("temp"));
            var compressProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = StartArgs.SevenZipExe,
                    Arguments = string.Format(" a -ir!\"{0}\\*.*\" -xr!thumbs.db -xr!*.cs -xr!*.psd -xr!*.csproj -x!obj -x!3dll -xr!_svn -xr!.svn -xr!.git {2} \"{1}\"", StartArgs.ProjectFolder, compressFile, StartArgs.SevenZipArgs),
                    //RedirectStandardOutput = true,
                    UseShellExecute = true,
                    //CreateNoWindow = true,
                }
            };
            //compressProc.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            compressProc.Start();
            //compressProc.BeginOutputReadLine();
            // ReSharper disable PossibleNullReferenceException
            compressProc.WaitForExit();
            // ReSharper restore PossibleNullReferenceException
            return compressFile;
        }

        /// <summary>
        /// 连接到服务器.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <returns>
        /// 成功返回NULL, 失败返回错误信息.
        /// </returns>
        private static string Connect2Server(out Socket client)
        {
            client = null;
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(StartArgs.ConnectPoint[0], int.Parse(StartArgs.ConnectPoint[1]));
            }
            catch (Exception exp)
            {
                return exp.Message;
            }

            return null;
        }

        /// <summary>
        /// 文件MD5验证.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="compressFile">
        /// The compress file.
        /// </param>
        /// <returns>
        /// 成功返回NULL, 失败返回错误信息.
        /// </returns>
        private static string FileMd5Verify(Socket client, string compressFile)
        {
            string error;
            try
            {
                var md5 = Util.GetFileMd5(compressFile);
                // 发送报头
                new RequestHeader { Action = RequestHeaderAction.VerifyFileMd5, BodyLength = (uint)md5.Length }.SendToServer(client);

                // 接收, 解析应答
                ResponseMessage repMsg;
                error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                if (error != null)
                    return error;
                if (repMsg.Action == ResponseMessageAction.Stop)
                    return repMsg.Message;

                // 成功, 继续发送报身
                var bodyBytes = Encoding.UTF8.GetBytes(md5);
                client.Send(bodyBytes);

                // 接收, 解析应答
                error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                if (error != null)
                    return error;
                if (repMsg.Action == ResponseMessageAction.Stop)
                    return repMsg.Message;
            }
            catch (Exception exp)
            {
                error = exp.Message;
            }

            return error;
        }

        /// <summary>
        /// 向服务器发送心跳
        /// </summary>
        private static void Heartbeat(object userId)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(StartArgs.HeartbeatPoint[0], int.Parse(StartArgs.HeartbeatPoint[1]));
            }
            catch (Exception exp)
            {
                ClientLog.Write("连接至心跳服务器失败: " + exp.Message, EventLogEntryType.Error);
                return;
            }
            ClientLog.Write("成功连接至心跳服务器: " + StartArgs.HeartbeatPoint, EventLogEntryType.Information);

            while (IsHeartbeatRunning)
            {
                try
                {
                    // ReSharper disable AssignNullToNotNullAttribute
                    client.Send(Encoding.UTF8.GetBytes(userId as string));
                    // ReSharper restore AssignNullToNotNullAttribute
                    Console.Beep(100, 10);
                    client.Receive(new byte[1], 0, 1, SocketFlags.None);
                    Console.Beep(1000, 10);
                }
                catch (Exception exp)
                {
                    ClientLog.Write("发送心跳失败: " + exp.Message, EventLogEntryType.Warning);
                }

                Thread.Sleep(5000);
            }
            // ReSharper disable FunctionNeverReturns
        }

        /// <summary>
        /// 程序入口
        /// <param name="args">
        /// 启动参数:
        /// [0] = 鉴权密钥
        /// [1] = 服务器IP地址,主服务器端口,心跳服务器端口
        /// [2] = 网络缓冲区大小
        /// [3] = ShoppingApp项目目录
        /// [4] = 7Z.EXE文件路径
        /// [5] = 是否备份服务器原网站
        /// [6] = 是否重启服务器IIS
        /// [7] = 是否总是覆盖服务器上的压缩包
        /// [8] = 传递给7Z.EXE的参数
        /// </param>
        /// </summary>
        private static void Main(string[] args)
        {
            Console.WriteLine(@"
***************************************************************
* {0}
* CLR Runtime Version: {1}
***************************************************************
", Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly().ImageRuntimeVersion);
            // 解析启动参数
            var argsParseError = StartArgsEntity.ParseArgs(args, out StartArgs);
            if (argsParseError != null)
            {
                if (argsParseError.Length > 0)
                {
                    ClientLog.Write("程序启动失败 " + argsParseError, EventLogEntryType.Error);
                    Util.Print2ConsoleLine(argsParseError);
                }

                Console.WriteLine(@"
启动参数:
[0] = 鉴权密钥
[1] = 服务器IP地址,主服务器端口,心跳服务器端口
[2] = 网络缓冲区大小
[3] = ShoppingApp项目目录
[4] = 7Z.EXE文件路径
[5] = 是否备份服务器原网站
[6] = 是否重启服务器IIS
[7] = 是否总是覆盖服务器上的压缩包
[8] = 传递给7Z.EXE的参数[可选,非安全参数,可能导致不可预料结果,请谨慎使用], 用法举例(不带首尾引号):""-xr!*.xls"":排除所有xls文件(循环子目录); ""-x!*.xls"":排除根目录所有xls文件(不循环子目录); ""-xr!xls"":排除所有xls目录及其目录下文件(循环子目录); ""-x!xls"":排除根目录下的xls目录及其目录下文件(不循环子目录);");
            }
            else
                StartPublish();
            system("pause");
            ClientLog.Write("程序已退出.", EventLogEntryType.Information);
        }

        /// <summary>
        /// 解析服务器响应消息.
        /// </summary>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <param name="bytes">
        /// The bytes.
        /// </param>
        /// <param name="repMsg">
        /// The rep msg.
        /// </param>
        /// <returns>
        /// 成功返回NULL, 失败返回错误消息.
        /// </returns>
        private static string ParseResponse(int length, byte[] bytes, out ResponseMessage repMsg)
        {
            repMsg = null;
            if (length != 1024)
                return "服务器应答长度发生错误.";
            ResponseMessageAction act;
            if (!Enum.TryParse(bytes[0].ToString(), out act))
                return "服务器应答类型发生错误.";
            repMsg = new ResponseMessage
            {
                Action = act,
                Message = Encoding.UTF8.GetString(bytes, 1, 1023).Trim()
            };
            return null;
        }

        /// <summary>
        /// 发送消息头(没有正文的情况)
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="act">
        /// The act.
        /// </param>
        /// <returns>
        /// 成功返回NULL, 失败返回错误信息.
        /// </returns>
        private static string SendHeaderSimple(Socket client, RequestHeaderAction act)
        {
            string error;
            try
            {
                // 发送报头
                new RequestHeader { Action = act, BodyLength = 0 }.SendToServer(client);

                // 接收, 解析应答
                ResponseMessage repMsg;
                error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                if (error != null)
                    return error;
                if (repMsg.Action == ResponseMessageAction.Stop)
                    return repMsg.Message;
            }
            catch (Exception exp)
            {
                error = exp.Message;
            }

            return error;
        }

        // ReSharper restore FunctionNeverReturns
        /// <summary>
        /// 开始发布处理.
        /// </summary>
        private static void StartPublish()
        {
            // 启动时间
            var startTime = DateTime.Now;

            //loading 效果
            var isShowLoading = false;
            new Thread(() =>
            {
                var i = 0;
                var loadingChars = @"-\|/".ToCharArray();
                while (true)
                {
                    // ReSharper disable AccessToModifiedClosure
                    if (isShowLoading)
                        // ReSharper restore AccessToModifiedClosure
                        Console.Write(loadingChars[i++ % 4] + "\b");
                    Thread.Sleep(200);
                }
            }) { IsBackground = true }.Start();

            // 连接到服务器.
            Util.Print2Console("连接服务器("+StartArgs.ConnectPoint[0]+":"+StartArgs.ConnectPoint[1]+")...");
            isShowLoading = true;
            Socket client;
            var connectError = Connect2Server(out client);
            if (connectError != null)
            {
                isShowLoading = false;
                Console.WriteLine("失败: " + connectError);
                ClientLog.Write(string.Format("连接至主服务器失败: {0}", connectError), EventLogEntryType.Error);
                return;
            }
            isShowLoading = false;
            Console.WriteLine("成功!");
            ClientLog.Write(string.Format("成功连接至主服务器 {0}", StartArgs.ConnectPoint.ToString()), EventLogEntryType.Information);

            // 设置缓冲区大小
            client.SendBufferSize = client.ReceiveBufferSize = StartArgs.BufferSize;
            SendBuffer = new byte[client.SendBufferSize];
            ReceiveBuffer = new byte[client.ReceiveBufferSize];
            // 用户认证.
            string userId;
            Util.Print2Console("用户口令验证...");
            isShowLoading = true;
            var authError = Authenticate(client, out userId);
            if (authError != null)
            {
                isShowLoading = false;
                Console.WriteLine("失败: " + authError);
                ClientLog.Write(string.Format("使用密钥 {0} 鉴权失败.", StartArgs.AuthKey), EventLogEntryType.Error);
                return;
            }
            isShowLoading = false;
            Console.WriteLine("成功!");

            // 开始向服务器发送心跳
            IsHeartbeatRunning = true;
            new Thread(Heartbeat) { IsBackground = true }.Start(userId);

            // 打包项目
            Util.Print2Console("程序打包中...");
            isShowLoading = true;
            var compressFile = CompressProject();
            isShowLoading = false;
            Console.WriteLine("完成!");
            ClientLog.Write(string.Format("压缩项目完成, 文件保存于 {0}", compressFile), EventLogEntryType.Information);

            // 发送文件
            Util.Print2Console("文件传输中...");
            string ask;
            var fileTransferError = TransferFile(client, compressFile, 0, out ask, StartArgs.IsFileOverriteAlway ? RequestHeaderAction.SendFileCreate : RequestHeaderAction.SendFileUnknow, TransferFileOnProgress);
            if (fileTransferError != null)
            {
                Console.WriteLine("失败: " + fileTransferError);
                ClientLog.Write(string.Format("传输 {0} 失败 {1}", compressFile, fileTransferError), EventLogEntryType.Error);
                try
                {
                    File.Delete(compressFile);
                    ClientLog.Write(string.Format("删除 {0} 成功.", compressFile), EventLogEntryType.Information);
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch (Exception exp)
                // ReSharper restore EmptyGeneralCatchClause
                { ClientLog.Write(string.Format("删除 {0} 失败 {1}", compressFile, exp.Message), EventLogEntryType.Warning); }
                IsHeartbeatRunning = false;
                return;
            }

            if (ask != null)
            {
                string yn;
                do
                {
                    Console.WriteLine(ask + ", 覆盖或续传(Y/N)?");
                    // ReSharper disable PossibleNullReferenceException
                    yn = Console.ReadLine().ToLower();
                    // ReSharper restore PossibleNullReferenceException
                }
                while (yn != "y" && yn != "n");

                // 续传, 取得服务器已存文件字节长度.
                var serverFileLength = 0;
                if (yn == "n")
                {
                    var m = new Regex("大小: (.*?),").Match(ask);
                    if (!m.Success || !int.TryParse(m.Groups[1].Value, out serverFileLength))
                    {
                        Console.WriteLine("失败: 无法取得服务器已存文件字节长度");
                        ClientLog.Write(string.Format("传输 {0} 失败 无法取得服务器已存文件字节长度.", compressFile), EventLogEntryType.Error);
                        IsHeartbeatRunning = false;
                        return;
                    }
                }

                fileTransferError = TransferFile(client, compressFile, serverFileLength, out ask, yn == "y" ? RequestHeaderAction.SendFileCreate : RequestHeaderAction.SendFileAppend, TransferFileOnProgress);
                if (fileTransferError != null)
                {
                    Console.WriteLine("失败: " + fileTransferError);
                    ClientLog.Write(string.Format("传输 {0} 失败 {1}", compressFile, fileTransferError), EventLogEntryType.Error);
                    try
                    {
                        File.Delete(compressFile);
                        ClientLog.Write(string.Format("删除 {0} 成功.", compressFile), EventLogEntryType.Information);
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch (Exception exp)
                    // ReSharper restore EmptyGeneralCatchClause
                    { ClientLog.Write(string.Format("删除 {0} 失败 {1}", compressFile, exp.Message), EventLogEntryType.Warning); }
                    IsHeartbeatRunning = false;
                    return;
                }
            }

            Console.WriteLine("完成!");
            ClientLog.Write(string.Format("传输 {0} 成功.", compressFile), EventLogEntryType.Information);

            // 验证文件MD5
            Util.Print2Console("验证文件MD5...");
            isShowLoading = true;
            var md5Error = FileMd5Verify(client, compressFile);
            if (md5Error != null)
            {
                isShowLoading = false;
                Console.WriteLine("失败: " + md5Error);
                ClientLog.Write(string.Format("验证MD5 {0} 失败 {1}", compressFile, md5Error), EventLogEntryType.Information);
                IsHeartbeatRunning = false;
                return;
            }
            isShowLoading = false;
            Console.WriteLine("成功!");
            ClientLog.Write(string.Format("验证MD5 {0} 成功.", compressFile), EventLogEntryType.Information);

            // 可以删掉本地压缩包了
            Util.Print2Console("删除本地压缩包...");
            isShowLoading = true;
            try
            {
                File.Delete(compressFile);
                isShowLoading = false;
                Console.WriteLine("成功");
                ClientLog.Write(string.Format("删除 {0} 成功.", compressFile), EventLogEntryType.Information);
            }
            catch (Exception exp)
            {
                isShowLoading = false;
                Console.WriteLine("失败: " + exp);
                ClientLog.Write(string.Format("删除 {0} 失败 {1}", compressFile, exp.Message), EventLogEntryType.Warning);
            }

            // 服务器备份
            if (StartArgs.IsBackup)
            {
                Util.Print2Console("原网站备份中...");
                isShowLoading = true;
                var backupError = SendHeaderSimple(client, RequestHeaderAction.Backup);
                if (backupError != null)
                {
                    isShowLoading = false;
                    Console.WriteLine("失败: " + backupError);
                    ClientLog.Write(string.Format("原网站备份失败 {0}", backupError), EventLogEntryType.Error);
                    IsHeartbeatRunning = false;
                    return;
                }
                isShowLoading = false;
                Console.WriteLine("成功!");
                ClientLog.Write("原网站备份成功.", EventLogEntryType.Information);
            }

            // 停止IIS
            if (StartArgs.IsResetIIS)
            {
                Util.Print2Console("停止IIS...");
                isShowLoading = true;
                var stopIISError = SendHeaderSimple(client, RequestHeaderAction.StopIIS);
                if (stopIISError != null)
                {
                    isShowLoading = false;
                    Console.WriteLine("失败: " + stopIISError);
                    ClientLog.Write(string.Format("停止IIS失败 {0}", stopIISError), EventLogEntryType.Error);
                    IsHeartbeatRunning = false;
                    return;
                }
                isShowLoading = false;
                Console.WriteLine("成功!");
                ClientLog.Write("停止IIS成功.", EventLogEntryType.Information);
            }

            // 更新网站
            Util.Print2Console("网站更新中...");
            isShowLoading = true;
            var updateError = SendHeaderSimple(client, RequestHeaderAction.Update);
            if (updateError != null)
            {
                isShowLoading = false;
                Console.WriteLine("失败: " + updateError);
                ClientLog.Write(string.Format("更新网站失败 "), EventLogEntryType.Error);
                IsHeartbeatRunning = false;
                return;
            }
            isShowLoading = false;
            Console.WriteLine("成功!");
            ClientLog.Write("更新网站成功.", EventLogEntryType.Information);

            // 启动IIS
            if (StartArgs.IsResetIIS)
            {
                Util.Print2Console("启动IIS...");
                isShowLoading = true;
                var startIISError = SendHeaderSimple(client, RequestHeaderAction.StartIIS);
                if (startIISError != null)
                {
                    isShowLoading = false;
                    Console.WriteLine("失败: " + startIISError);
                    ClientLog.Write(string.Format("启动IIS失败 {0}", startIISError), EventLogEntryType.Error);
                    IsHeartbeatRunning = false;
                    return;
                }
                isShowLoading = false;
                Console.WriteLine("成功!");
                ClientLog.Write("启动IIS成功.", EventLogEntryType.Information);
            }

            var allOk = string.Format("全部任务运行完毕, 共耗时: {0}s", Math.Round((DateTime.Now - startTime).TotalSeconds, 1));
            Util.Print2ConsoleLine(allOk);
            ClientLog.Write(allOk, EventLogEntryType.Information);
            Util.Print2Console("程序将在9秒后自动关闭！  ");
            LastOutLen = 0;
            for (int i = 9; i >0; i--)
            {
                if (LastOutLen != 0) Console.Write(new string('\b', LastOutLen));
                Console.Write(i.ToString());
                LastOutLen = i.ToString().Length;
                Thread.Sleep(1000);
            }
            Environment.Exit(1);
        }

        [DllImport("msvcrt.dll")]
        static extern bool system(string str);

        /// <summary>
        /// 传送文件到服务器.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="compressFile">
        /// The compress file.
        /// </param>
        /// <param name="offset">
        /// The offset.
        /// </param>
        /// <param name="ask">
        /// The ask.
        /// </param>
        /// <param name="act">
        /// The act.
        /// </param>
        /// <param name="onprogress">
        /// The onprogress.
        /// </param>
        /// <returns>
        /// 成功返回NULL, 失败返回错误信息.
        /// </returns>
        private static string TransferFile(Socket client, string compressFile, int offset, out string ask, RequestHeaderAction act, EventHandler onprogress)
        {
            ask = null;
            string error;
            try
            {
                // 文件长度
                var fileLength = (int)new FileInfo(compressFile).Length;

                // 发送报头
                new RequestHeader { Action = act, BodyLength = (uint)(fileLength - offset) }.SendToServer(client);

                // 接收, 解析应答
                ResponseMessage repMsg;
                error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                if (error != null)
                    return error;
                if (repMsg.Action == ResponseMessageAction.Stop)
                    return repMsg.Message;

                // 服务器询问文件覆盖模式
                if (repMsg.Action == ResponseMessageAction.Ask)
                {
                    ask = repMsg.Message;
                }
                else
                {
                    // 成功, 继续发送报身
                    using (var fs = File.Open(compressFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // fs指针移动到指定偏移量
                        fs.Seek(offset, SeekOrigin.Begin);

                        int readLength;
                        var sendAllLength = 0;

                        while ((readLength = fs.Read(SendBuffer, 0, SendBuffer.Length)) > 0)
                        {
                            sendAllLength += client.Send(SendBuffer, 0, readLength, SocketFlags.None);
                            // 进度事件
                            onprogress(new[] { sendAllLength + offset, fileLength }, null);
                        }
                    }

                    // 接收, 解析应答
                    error = ParseResponse(client.Receive(ReceiveBuffer), ReceiveBuffer, out repMsg);
                    if (error != null)
                        return error;
                    if (repMsg.Action == ResponseMessageAction.Stop)
                        return repMsg.Message;
                }
            }
            catch (Exception exp)
            {
                error = exp.Message;
            }

            return error;
        }

        /// <summary>
        /// 文件传送进度事件.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void TransferFileOnProgress(object sender, EventArgs e)
        {
            var data = sender as int[];
            // ReSharper disable PossibleNullReferenceException
            var outStr = Math.Round((float)data[0] / data[1] * 100, 2) + "% ";
            // ReSharper restore PossibleNullReferenceException

            if (LastOutLen != 0) Console.Write(new string('\b', LastOutLen));
            Console.Write(outStr);
            LastOutLen = outStr.Length;
        }

        #endregion Methods
    }
}
