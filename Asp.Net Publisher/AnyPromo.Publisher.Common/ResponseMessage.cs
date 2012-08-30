// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   服务器响应客户端的消息类.
//   协议:总长1024字节,[0]=目标类型,[1-1023]=附加消息.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.Common
{
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// 服务器响应客户端的消息类.
    /// 协议:总长1024字节,[0]=目标类型,[1-1023]=附加消息.
    /// </summary>
    public class ResponseMessage
    {
        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMessage"/> class.
        /// </summary>
        public ResponseMessage()
        {
            Action = ResponseMessageAction.Go;
            Message = null;
        }

        #endregion Constructors

        #region Properties (2)

        /// <summary>
        /// 目标类型.
        /// </summary>
        public ResponseMessageAction Action { get; set; }

        /// <summary>
        /// 附加消息.
        /// </summary>
        public string Message { get; set; }

        #endregion Properties

        #region Methods (2)

        // Public Methods (2) 

        /// <summary>
        /// 发送响应到客户端.
        /// </summary>
        /// <param name="workSocket">工作Socket.</param>
        public void SendToClient(Socket workSocket)
        {
            var sendBytes = new byte[1024];
            sendBytes[0] = (byte)Action;
            // 空格填充
            Encoding.UTF8.GetBytes(new string((char)0x20, 1023)).CopyTo(sendBytes, 1);
            if (Message != null)
                Encoding.UTF8.GetBytes(Message).CopyTo(sendBytes, 1);
            workSocket.Send(sendBytes);
        }

        #endregion Methods
    }
}
