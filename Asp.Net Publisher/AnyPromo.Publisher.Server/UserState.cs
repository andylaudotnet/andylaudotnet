// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   IAsyncResult的自定义StateObject
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.ConsoleServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Common;

    /// <summary>
    /// IAsyncResult的自定义StateObject(用户状态)
    /// </summary>
    public class UserState : IDisposable
    {
        #region Fields (2)

        /// <summary>
        /// 心跳时间.
        /// </summary>
        public DateTime Heartbeat;
        private Program Instance;

        #endregion Fields

        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="UserState"/> class.
        /// </summary>
        public UserState(Program instance)
        {
            Instance = instance;
            new Thread(Instance.HeartSvr.CheckHeartbeat) { IsBackground = true }.Start();
            WorkSocket = null;
            IsAuthentication = false;
            Id = Guid.NewGuid().ToString();
            Heartbeat = DateTime.Now;
            Refresh();
            Instance.PublishSvr.OnlineUser.Add(this);
        }

        #endregion Constructors

        #region Properties (7)

        /// <summary>
        /// 缓冲区.
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// 用户Id.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 用户已鉴权.
        /// </summary>
        public bool IsAuthentication { get; set; }

        /// <summary>
        /// 读取总长度.
        /// </summary>
        public long ReceivedLength { get; set; }

        /// <summary>
        /// 报文头.
        /// </summary>
        public RequestHeader ReqHeader { get; private set; }

        /// <summary>
        /// 字符串容器.
        /// </summary>
        public StringBuilder StrContainer { get; private set; }

        /// <summary>
        /// 关联Socket.
        /// </summary>
        public Socket WorkSocket { get; set; }

        #endregion Properties

        #region Methods (3)

        // Public Methods (3) 

        // Public Methods (4) 
        /// <summary>
        /// 关闭Socket连接.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// 关闭Socket连接.
        /// </summary>
        public void Dispose()
        {
            try
            {
                WorkSocket.Shutdown(SocketShutdown.Both);
                WorkSocket.Close();
            }
            catch (Exception)
            {
            }

            Instance.PublishSvr.OnlineUser.Remove(this);
            if (Instance.PublishSvr.OnlineUser.Count == 0)
                // 没有在线用户了, 服务器继续Accept;
                Instance.PublishSvr.ProcControler.Set();
        }

        /// <summary>
        /// 刷新(重置)StateObject.
        /// </summary>
        public void Refresh()
        {
            Buffer = new byte[Instance.StartArgs.BufferSize];
            StrContainer = new StringBuilder();
            ReqHeader = new RequestHeader();
            ReceivedLength = 0;
        }

        #endregion Methods
    }
}
