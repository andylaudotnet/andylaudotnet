// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   数据报文头, 5个字节.
//   [0]: 目标类型(byte).
//   [1-4]: 正文长度(uint).
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Asp.Net.Publisher.Common
{
    using System;
    using System.Net.Sockets;

    /// <summary>
    /// 数据报文头, 5个字节.
    /// [0]: 目标类型(byte).
    /// [1-4]: 正文长度(uint).
    /// </summary>
    public class RequestHeader
    {
        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestHeader"/> class.
        /// </summary>
        public RequestHeader()
        {
            Action = RequestHeaderAction.WaitAction;
            BodyLength = 0;
        }

        #endregion Constructors

        #region Properties (2)

        /// <summary>
        /// Gets or sets Action.
        /// </summary>
        public RequestHeaderAction Action { get; set; }

        /// <summary>
        /// Gets or sets BodyLength.
        /// </summary>
        public uint BodyLength { get; set; }

        #endregion Properties

        #region Methods (1)

        // Public Methods (1) 

        /// <summary>
        /// 将消息请求头发送到服务器.
        /// </summary>
        /// <param name="workSocket">
        /// The work socket.
        /// </param>
        public void SendToServer(Socket workSocket)
        {
            var sendBytes = new byte[5];
            sendBytes[0] = (byte)Action;
            BitConverter.GetBytes(BodyLength).CopyTo(sendBytes, 1);
            workSocket.Send(sendBytes);
        }

        #endregion Methods
    }
}
