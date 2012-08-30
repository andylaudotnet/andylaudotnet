// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   发布服务器(主服务)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.ConsoleServer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Common;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 发布服务器(主服务)
    /// </summary>
    public class PublishServer
    {
        #region Fields (3)

        public Program Instance;
        /// <summary>
        /// 在线用户列表.不
        /// </summary>
        public readonly List<UserState> OnlineUser = new List<UserState>();
        /// <summary>
        /// 处理器控制对象.
        /// </summary>
        public readonly ManualResetEvent ProcControler = new ManualResetEvent(false);

        #endregion Fields

        #region Constructors (1)

        public PublishServer(Program instance)
        {
            Instance = instance;
        }

        #endregion Constructors

        #region Methods (11)

        // Public Methods (1) 

        /// <summary>
        /// 启动服务器.
        /// </summary>
        public void PowerOn()
        {
            // 定义Socket Server对象.
            var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 绑定IP, 开始侦听.
            try
            {
                server.Bind(Instance.StartArgs.BindPointPublish);
                server.Listen(Instance.StartArgs.QueueLimit);
            }
            catch (Exception exp)
            {
                // 致命异常, 须退出程序.
                ServerLog.Write(string.Format("主服务侦听端口 {0} 失败: {1}", Instance.StartArgs.BindPointPublish, exp.Message), EventLogEntryType.Error);
                return;
            }

            ServerLog.Write("主服务已启动, 侦听端口: " + Instance.StartArgs.BindPointPublish, EventLogEntryType.Information);

            // 循环接受TCP连接.
            while (true)
            {
                ProcControler.Reset();
                try
                {
                    OnAccept(server.Accept());
                }
                catch (Exception exp)
                {
                    ServerLog.Write("主服务在接受新连接时发生错误: " + exp.Message, EventLogEntryType.Warning);
                }
                ServerLog.Write("主服务收到新的连接.", EventLogEntryType.Information);

                ProcControler.WaitOne();
            }
        }
        // Private Methods (10) 

        // Private Methods (10) 
        /// <summary>
        /// 备份网站.
        /// </summary>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        private string BackupWeb()
        {
            try
            {
                // ReSharper disable PossibleNullReferenceException
                Process.Start(Instance.StartArgs.SevenZipExe, string.Format(" a -ir!\"{0}\\*.*\" \"{1}\"", Instance.StartArgs.WebRoot, Instance.StartArgs.BackupFolder + "\\" + DateTime.Now.ToString("yyyy_MMdd_HHmm") + ".7z")).WaitForExit(20 * 60 * 1000);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception exp)
            {
                return exp.Message;
            }

            return null;
        }

        /// <summary>
        /// 开始接收数据.
        /// </summary>
        /// <param name="state">IAsyncResult对象.</param>
        /// <returns>成功返回True.</returns>
        private bool BeginReceive(UserState state)
        {
            try
            {
                state.WorkSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), state);
            }
            catch (Exception exp)
            {
                ServerLog.Write("主服务在准备接收数据时发生错误: " + exp.Message, EventLogEntryType.Warning);
                state.Close();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 收到新连接.
        /// </summary>
        private void OnAccept(Socket client)
        {
            // 创建了一个新的Socket.
            var state = new UserState(Instance) { WorkSocket = client };

            // 设置缓冲区大小
            state.WorkSocket.ReceiveBufferSize = state.WorkSocket.SendBufferSize = Instance.StartArgs.BufferSize;
            BeginReceive(state);
        }

        /// <summary>
        /// 收到数据.
        /// 在此函数中, 出现异常Socket将关闭, 退出函数并使Server继续Accept.
        /// </summary>
        /// <param name="ar">状态.</param>
        private void OnReceive(IAsyncResult ar)
        {
            var state = ar.AsyncState as UserState;
            // 本次读取字节长度.
            int readLength;
            try
            {
                // ReSharper disable PossibleNullReferenceException
                readLength = state.WorkSocket.EndReceive(ar);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception exp)
            {
                ServerLog.Write("主服务在接收数据中发生错误: " + exp.Message, EventLogEntryType.Warning);
                // ReSharper disable PossibleNullReferenceException
                state.Close();
                // ReSharper restore PossibleNullReferenceException
                return;
            }
            // 气数尽了, 丢弃.
            if (readLength <= 0)
            {
                ServerLog.Write("主服务在接收数据中发生错误: 从缓冲区中读取的字节长度为0.", EventLogEntryType.Warning);
                state.Close();
                return;
            }

            // 如果MsgHedaer是等候指令状态, 则认为本次消息为设置MsgHeader
            if (state.ReqHeader.Action == RequestHeaderAction.WaitAction)
            {
                string headerError;
                if ((headerError = ReceivedHeader(state, readLength)) != null)
                {
                    if (headerError.Length > 0)
                    {
                        // 告诉客户端,哪儿错了
                        try
                        {
                            new ResponseMessage { Action = ResponseMessageAction.Stop, Message = headerError }.SendToClient(state.WorkSocket);
                        }
                        catch (Exception exp)
                        {
                            ServerLog.Write(string.Format("主服务在发送信息至客户端时发生错误: {0}, 信息内容: {1}", exp.Message, headerError), EventLogEntryType.Warning);
                        }
                    }

                    state.Close();
                    return;
                }
            }
            else
            {
                string bodyError;
                if ((bodyError = ReceivedBody(state, readLength)) != null)
                {
                    if (bodyError.Length > 0)
                    {
                        // 告诉客户端,哪儿错了
                        try
                        {
                            new ResponseMessage { Action = ResponseMessageAction.Stop, Message = bodyError }.SendToClient(
                                state.WorkSocket);
                        }
                        catch (Exception exp)
                        {
                            ServerLog.Write(string.Format("主服务在发送信息至客户端时发生错误: {0}, 信息内容: {1}", exp.Message, bodyError), EventLogEntryType.Warning);
                        }
                    }

                    state.Close();
                    return;
                }
            }

            // 继续读取
            if (!BeginReceive(state))
                return;
        }

        /// <summary>
        /// 收到正文的处理过程.
        /// </summary>
        /// <param name="state">state对象.</param>
        /// <param name="length">已从缓冲区读取的字节数.</param>
        /// <returns>成功返回True, 失败须结束state.socket</returns>
        private string ReceivedBody(UserState state, int length)
        {
            // 要响应到客户端的消息
            string msgAck = null;

            // 用户未鉴权, 退出
            if (!state.IsAuthentication && state.ReqHeader.Action != RequestHeaderAction.Authentication)
                return "用户未鉴权.";

            // 第一次接收?
            var isFirstReceiveBody = state.ReceivedLength == 0;
            // 接收总字节数增加.
            state.ReceivedLength += length;

            // 根据报文头定义的正文长度读取数据
            switch (state.ReqHeader.Action)
            {
                case RequestHeaderAction.SendFileCreate:
                    // 如果是第一次读缓冲区, 则根据报头创建新文件., 否则追加文件
                    try
                    {
                        Util.WriteFile(Instance.StartArgs.UploadFile, isFirstReceiveBody ? FileMode.Create : FileMode.Append, state.Buffer, length);
                    }
                    catch (Exception exp)
                    {
                        ServerLog.Write(string.Format("压缩文件 {0} 写入失败: {1}", Instance.StartArgs.UploadFile, exp.Message), EventLogEntryType.Warning);
                        return "文件写入失败.";
                    }

                    break;
                // 追加模式
                case RequestHeaderAction.SendFileAppend:
                    try
                    {
                        Util.WriteFile(Instance.StartArgs.UploadFile, FileMode.Append, state.Buffer, length);
                    }
                    catch (Exception exp)
                    {
                        ServerLog.Write(string.Format("压缩文件 {0} 写入失败: {1}", Instance.StartArgs.UploadFile, exp.Message), EventLogEntryType.Warning);
                        return "文件写入失败.";
                    }

                    break;
                case RequestHeaderAction.Authentication:
                    state.StrContainer.Append(Encoding.UTF8.GetString(state.Buffer, 0, length));
                    if (state.ReceivedLength == state.ReqHeader.BodyLength)
                    {
                        if (state.StrContainer.ToString() != Instance.StartArgs.AuthKey)
                            // 验证失败了.
                            return "用户鉴权失败.";
                        // 鉴权成功
                        state.IsAuthentication = true;
                        msgAck = state.Id;
                    }

                    break;
                case RequestHeaderAction.VerifyFileMd5:
                    state.StrContainer.Append(Encoding.UTF8.GetString(state.Buffer, 0, length));
                    if (state.ReceivedLength == state.ReqHeader.BodyLength)
                    {
                        if (state.StrContainer.ToString() != Util.GetFileMd5(Instance.StartArgs.UploadFile))
                            // MD5验证失败了.
                            return "文件MD5验证失败.";
                    }

                    break;
                default:
                    return string.Empty;
            }

            // 如果接收的字节总长已经达到MsgHeader中指定. 则视为接收完毕.
            if (state.ReceivedLength == state.ReqHeader.BodyLength)
            {
                // 一个回合结束了, 清除State的已存信息, 恢复state状态为waitaction
                state.Refresh();

                // 响应客户端
                try
                {
                    new ResponseMessage { Action = ResponseMessageAction.Go, Message = msgAck }.SendToClient(state.WorkSocket);
                }
                catch (Exception exp)
                {
                    ServerLog.Write(string.Format("主服务在发送信息至客户端时发生错误: {0}, 信息内容: {1}", exp.Message, msgAck), EventLogEntryType.Warning);
                    return string.Empty;
                }
            }

            return null;
        }

        /// <summary>
        /// 收到报文头的处理过程.
        /// </summary>
        /// <param name="state">state对象.</param>
        /// <param name="length">已从缓冲区读取的字节数.</param>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        private string ReceivedHeader(UserState state, int length)
        {
            string headerError;
            if ((headerError = SetMsgHeader(length, state)) != null)
                return headerError;

            // 设置成功, 通知客户端可以继续.
            string sendMsg = null;
            try
            {
                var sendAct = ResponseMessageAction.Go;

                // 如果action为transferfileunknow, 提示用户
                if (state.ReqHeader.Action == RequestHeaderAction.SendFileUnknow)
                {
                    state.ReqHeader.Action = RequestHeaderAction.SendFileCreate;
                    if (File.Exists(Instance.StartArgs.UploadFile))
                    {
                        var fi = new FileInfo(Instance.StartArgs.UploadFile);
                        sendAct = ResponseMessageAction.Ask;
                        sendMsg = string.Format("服务器已存在此文件, 大小: {0}, 写入日期: {1}", fi.Length, fi.LastWriteTime);
                        state.Refresh();
                    }
                }
                // 没有消息正文的几种情况
                if (state.ReqHeader.Action == RequestHeaderAction.Backup ||
                    state.ReqHeader.Action == RequestHeaderAction.StopIIS ||
                    state.ReqHeader.Action == RequestHeaderAction.StartIIS ||
                    state.ReqHeader.Action == RequestHeaderAction.Update)
                {
                    var error = state.ReqHeader.Action == RequestHeaderAction.Backup
                                    ? BackupWeb()
                                    : state.ReqHeader.Action == RequestHeaderAction.Update
                                          ? UpdateWeb()
                                          : state.ReqHeader.Action == RequestHeaderAction.StopIIS
                                                ? StopIIS()
                                                : StartIIS();
                    if (error != null) return error;
                    state.Refresh();
                }

                new ResponseMessage { Action = sendAct, Message = sendMsg }.SendToClient(state.WorkSocket);
            }
            catch (Exception exp)
            {
                ServerLog.Write(string.Format("主服务在发送信息至客户端时发生错误: {0}, 信息内容: {1}", exp.Message, sendMsg), EventLogEntryType.Warning);
                return string.Empty;
            }

            return null;
        }

        /// <summary>
        /// 设置state对象的报文头.
        /// </summary>
        /// <param name="length">已从缓存区读取字节长度.</param>
        /// <param name="state">state对象.</param>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        private string SetMsgHeader(int length, UserState state)
        {
            // 长度错误
            if (length != 5)
                return string.Empty;

            // 设置MsgHeader, Action
            RequestHeaderAction act;
            if (!Enum.TryParse(state.Buffer[0].ToString(), out act))
                return string.Empty;
            state.ReqHeader.Action = act;

            // 当用户未鉴权时, Action只能为Authentication
            if (!state.IsAuthentication && state.ReqHeader.Action != RequestHeaderAction.Authentication)
                return "用户未鉴权.";

            // 设置MsgHeader, BodyLength
            state.ReqHeader.BodyLength = BitConverter.ToUInt32(state.Buffer, 1);

            return null;
        }

        /// <summary>
        /// 启动IIS.
        /// </summary>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        private string StartIIS()
        {
            try
            {
                // ReSharper disable PossibleNullReferenceException
                Process.Start(Environment.SystemDirectory + "\\iisreset.exe", " /start").WaitForExit(5 * 60 * 1000);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception exp)
            {
                return exp.Message;
            }

            return null;
        }

        /// <summary>
        /// 停止IIS.
        /// </summary>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        private string StopIIS()
        {
            try
            {
                // ReSharper disable PossibleNullReferenceException
                Process.Start(Environment.SystemDirectory + "\\iisreset.exe", " /stop").WaitForExit(5 * 60 * 1000);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception exp)
            {
                return exp.Message;
            }

            return null;
        }

        /// <summary>
        /// 更新网站.
        /// </summary>
        /// <returns>成功返回NULL, 失败返回错误信息.</returns>
        private string UpdateWeb()
        {
            try
            {
                // ReSharper disable PossibleNullReferenceException
                Process.Start(Instance.StartArgs.SevenZipExe, string.Format(" x -y -o\"{0}\" \"{1}\"", Instance.StartArgs.WebRoot, Instance.StartArgs.UploadFile)).WaitForExit(20 * 60 * 1000);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception exp)
            {
                return exp.Message;
            }

            return null;
        }

        #endregion Methods
    }
}
