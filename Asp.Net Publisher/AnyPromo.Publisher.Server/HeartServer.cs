// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   心跳服务器
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.ConsoleServer
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// 心跳服务器
    /// </summary>
    public class HeartServer
    {
        #region Fields (1)

        Program Instance;

        #endregion Fields

        #region Constructors (1)

        public HeartServer(Program instance)
        {
            Instance = instance;
        }

        #endregion Constructors

        #region Methods (7)

        // Public Methods (3) 

        // Private Methods (1) 
        /// <summary>
        /// 检查心跳.
        /// </summary>
        public void CheckHeartbeat()
        {
            while (true)
            {
                // 开始检查心跳
                // ReSharper disable ForCanBeConvertedToForeach
                for (var i = 0; i < Instance.PublishSvr.OnlineUser.Count; i++)
                // ReSharper restore ForCanBeConvertedToForeach
                {
                    if ((DateTime.Now - Instance.PublishSvr.OnlineUser[i].Heartbeat).TotalSeconds < 50) continue;
                    Console.WriteLine("CLOSE-" + Instance.PublishSvr.OnlineUser[i].Id + "-" + Instance.PublishSvr.OnlineUser[i].Heartbeat);
                    Instance.PublishSvr.OnlineUser[i].Close();
                    i = 0;
                }

                Thread.Sleep(10000);
            }
            // ReSharper disable FunctionNeverReturns
        }

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
                server.Bind(Instance.StartArgs.BindPointHeart);
                server.Listen(Instance.StartArgs.QueueLimit);
            }
            catch (Exception exp)
            {
                // 致命异常, 须退出程序.
                ServerLog.Write(string.Format("心跳服务侦听端口 {0} 失败: {1}", Instance.StartArgs.BindPointHeart, exp.Message), EventLogEntryType.Error);
                return;
            }
            ServerLog.Write("心跳服务已启动, 侦听端口: " + Instance.StartArgs.BindPointHeart, EventLogEntryType.Information);

            BeginAccept(server);
        }

        /// <summary>
        /// 设置心跳.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// 成功返回True, 找不到ID返回False;
        /// </returns>
        public bool SetHeartbeat(string id)
        {
            foreach (var t in Instance.PublishSvr.OnlineUser.Where(t => t.Id == id))
            {
                t.Heartbeat = DateTime.Now;
                Console.WriteLine("SET-" + t.Id + "-" + t.Heartbeat);
                return true;
            }

            return false;
        }
        // Private Methods (4) 

        /// <summary>
        /// 开始接受连接.
        /// </summary>
        /// <param name="socket">server socket</param>
        private void BeginAccept(Socket socket)
        {
            try
            {
                socket.BeginAccept(new AsyncCallback(OnAccept), socket);
            }
            catch (Exception exp)
            {
                ServerLog.Write("心跳服务在准备接受新连接时发生错误: " + exp.Message, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// 开始接收数据.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        private void BeginReceive(Socket socket, byte[] buffer)
        {
            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), new object[] { socket, buffer });
            }
            catch (Exception exp)
            {
                ServerLog.Write("心跳服务在准备接收数据时发生错误: " + exp.Message, EventLogEntryType.Warning);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        /// <summary>
        /// 收到新连接.
        /// </summary>
        /// <param name="ar">状态.</param>
        private void OnAccept(IAsyncResult ar)
        {
            var server = ar.AsyncState as Socket;
            BeginAccept(server);

            Socket client;
            try
            {
                // 创建了一个新的Socket.
                // ReSharper disable PossibleNullReferenceException
                client = server.EndAccept(ar);
                // ReSharper restore PossibleNullReferenceException

                // 设置缓冲区大小
                client.ReceiveBufferSize = Instance.StartArgs.BufferSize;
            }
            catch (Exception exp)
            {
                ServerLog.Write("心跳服务在接受新连接时发生错误: " + exp.Message, EventLogEntryType.Warning);
                return;
            }
            ServerLog.Write("心跳服务收到新的连接.", EventLogEntryType.Information);

            BeginReceive(client, new byte[Instance.StartArgs.BufferSize]);
        }

        /// <summary>
        /// 收到数据.
        /// 在此函数中, 出现异常Socket将关闭.
        /// </summary>
        /// <param name="ar">状态.</param>
        private void OnReceive(IAsyncResult ar)
        {
            var state = ar.AsyncState as object[];
            // ReSharper disable PossibleNullReferenceException
            var socket = state[0] as Socket;
            // ReSharper restore PossibleNullReferenceException
            var buffer = state[1] as byte[];

            // 本次读取字节长度.
            int readLength;
            try
            {
                // ReSharper disable PossibleNullReferenceException
                readLength = socket.EndReceive(ar);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception exp)
            {
                ServerLog.Write("心跳服务在接收数据中发生错误: " + exp.Message, EventLogEntryType.Warning);
                // ReSharper disable PossibleNullReferenceException
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                // ReSharper restore PossibleNullReferenceException
                return;
            }

            // 错误数据
            if (readLength != 36)
            {
                ServerLog.Write("心跳服务在接收数据中发生错误: 从缓冲区中读取的字节长度不符合协议.", EventLogEntryType.Warning);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                return;
            }

            // 设置userstate心跳时间 , 响应客户端
            try
            {
                socket.Send(new[] { (byte)(SetHeartbeat(Encoding.UTF8.GetString(buffer, 0, readLength)) ? 1 : 0) });
            }
            catch (Exception)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                return;
            }

            // 继续读取
            BeginReceive(socket, buffer);
        }

        #endregion Methods


        // ReSharper restore FunctionNeverReturns
    }
}
