using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using log4net;
using SuperWebSocket;

namespace WebSocketChannel
{
    class WebSocketServerChannel : ChannelBase, IDuplexSessionChannel
    {
        const int maxBufferSize = 2048000;

        MessageEncoder encoder;
        BufferManager bufferManager;

        WebSocketSession wsServer = null;
        EndpointAddress localAddress = null;
        EndpointAddress remoteAddress = null;

        Queue<byte[]> receivedDataQueue = new Queue<byte[]>();
        Queue<ReadDataAsyncResultServer> readDataAsyncResultQueue = new Queue<ReadDataAsyncResultServer>();

        /// <summary>
        /// Use the same locker to operate receivedDataQueue and readDataAsyncResultQueue. Otherwise, the data may be lost
        /// </summary>
        object recvLocker = new object();
        public WebSocketServerChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager,
            WebSocketSession wsServer)//, EndpointAddress localAddress, EndpointAddress remoteAddress)
            : base(channelManager)
        {
            this.encoder = encoder;
            this.bufferManager = bufferManager;

            this.wsServer = wsServer;
            this.localAddress = new EndpointAddress(wsServer.Origin);
            this.remoteAddress = new EndpointAddress(string.Format("{0}://{1}:{2}",
                wsServer.UriScheme, wsServer.RemoteEndPoint.Address, wsServer.RemoteEndPoint.Port));

            this.Session = new ConnectionDuplexSession();
        }
        protected override void OnAbort()
        {
            logger.Error("OnAbort()");

            if (this.wsServer.Connected)
            {
                this.wsServer.Close();
            }
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnClose(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            logger.Info("OnClose()...");

            if (this.wsServer.Connected)
            {
                this.wsServer.Close();
            }
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            logger.Info("OnOpen()...");
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ReadDataAsyncResultServer result = new ReadDataAsyncResultServer(this, callback, state);

            lock (this.recvLocker)
            {
                // if there is already data received, complete the read async result immediately 
                if (this.receivedDataQueue.Count > 0)
                {
                    result.Complete(true, this.receivedDataQueue.Dequeue());

                    return result;
                }

                // there is no data received, wait for data arrive
                this.readDataAsyncResultQueue.Enqueue(result);
            }

            return result;
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return this.BeginReceive(this.DefaultReceiveTimeout, callback, state);
        }

        public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new TryReceiveAsyncResult(timeout, this, callback, state);
        }

        public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public Message EndReceive(IAsyncResult result)
        {
            byte[] data = ReadDataAsyncResultServer.End(result);
            if (data == null)
            {
                return null;
            }

            return this.DecodeMessage(data);
        }

        public bool EndTryReceive(IAsyncResult result, out Message message)
        {
            return TryReceiveAsyncResult.End(result, out message);
        }

        public bool EndWaitForMessage(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public EndpointAddress LocalAddress
        {
            get { return this.localAddress; }
        }

        public Message Receive(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public Message Receive()
        {
            throw new NotImplementedException();
        }

        public bool TryReceive(TimeSpan timeout, out Message message)
        {
            throw new NotImplementedException();
        }

        public bool WaitForMessage(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public void EndSend(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public EndpointAddress RemoteAddress
        {
            get { return this.remoteAddress; }
        }

        public void Send(Message message, TimeSpan timeout)
        {
            logger.DebugFormat("sending message...");

            base.ThrowIfDisposedOrNotOpen();

            ArraySegment<byte> encodedBytes = default(ArraySegment<byte>);

            try
            {
                encodedBytes = this.EncodeMessage(message);

                List<ArraySegment<byte>> segments = this.SplitData(encodedBytes);
                byte[] lenArray = BitConverter.GetBytes(encodedBytes.Count);
                segments.Insert(0, new ArraySegment<byte>(lenArray, 0, 4));

                // send message
                foreach (var item in segments)
                {
                    this.wsServer.Send(item.Array, item.Offset, item.Count);
                }

                logger.DebugFormat("sent message [{0}].", encodedBytes.Count);
            }
            finally
            {
                if (encodedBytes.Array != null)
                {
                    this.bufferManager.ReturnBuffer(encodedBytes.Array);
                }
            }
        }

        public void Send(Message message)
        {
            this.Send(message, this.DefaultSendTimeout);
        }

        public Uri Via
        {
            get { throw new NotImplementedException(); }
        }

        byte[] recvBuffer = null;
        int bufferPos = -1;
        int expectLen = -1;

        public void ReceiveData(byte[] data)
        {
            if (data == null)
            {
                logger.Error("received null data");
                return;
            }

            // get length data
            if (this.bufferPos == -1)
            {
                if (data.Length != 4)
                {
                    logger.ErrorFormat("error length data. [{0}]", data.Length);
                    return;
                }

                // length data
                this.expectLen = BitConverter.ToInt32(data, 0);
                this.recvBuffer = new byte[this.expectLen];
                this.bufferPos = 0;

                return;
            }

            // fill buffer
            Array.Copy(data, 0, this.recvBuffer, this.bufferPos, data.Length);
            this.bufferPos += data.Length;
            if (this.bufferPos < this.expectLen)
            {
                return; // not full
            }
            this.bufferPos = -1;

            lock (this.recvLocker)
            {
                // complete the read async result
                if (this.readDataAsyncResultQueue.Count > 0)
                {
                    ReadDataAsyncResultServer result = this.readDataAsyncResultQueue.Dequeue();
                    if (data != null)
                    {
                        result.Complete(false, this.recvBuffer);
                    }
                    else
                    {
                        result.Complete(new TimeoutException());
                    }

                    return;
                }

                // there is no read data async result, cache the received data
                this.receivedDataQueue.Enqueue(this.recvBuffer);
            }
        }

        ArraySegment<byte> EncodeMessage(Message message)
        {
            logger.DebugFormat("Encoding message ...");
            try
            {
                ArraySegment<byte> bytes = encoder.WriteMessage(message, maxBufferSize, this.bufferManager);

                logger.DebugFormat("Encoded message.");

                return bytes;
            }
            finally
            {
                // we've consumed the message by serializing it, so clean up
                message.Close();
            }
        }

        Message DecodeMessage(byte[] data)
        {
            logger.DebugFormat("Decoding message ...");

            // take buffer from buffer manager
            // the message will be closed later and the buffer will be retured to buffer manager
            byte[] buffer = this.bufferManager.TakeBuffer(data.Length);
            data.CopyTo(buffer, 0);

            // Note that we must set the ArraySegment count as data length. The buffer taken from buffer Manager
            // may have more space for the length required.
            ArraySegment<byte> encodedBytes = new ArraySegment<byte>(buffer, 0, data.Length);

            Message msg = this.encoder.ReadMessage(encodedBytes, this.bufferManager);
            if (msg != null)
            {
                msg.Headers.To = this.LocalAddress.Uri;
                //RemoteEndpointMessageProperty prop = new RemoteEndpointMessageProperty(this.LocalAddress.Uri.Host, this.LocalAddress.Uri.Port);
                //msg.Properties.Add(RemoteEndpointMessageProperty.Name, prop);
            }

            logger.DebugFormat("Decoded.");

            return msg;
        }

        public IDuplexSession Session
        {
            get;
            private set;
        }

        private List<ArraySegment<byte>> SplitData(ArraySegment<byte> data)
        {
            List<ArraySegment<byte>> splitedList = new List<ArraySegment<byte>>();

            int pos = data.Offset;
            int maxLen = 65536; // 64K
            while (pos < data.Count)
            {
                splitedList.Add(new ArraySegment<byte>(data.Array, pos, maxLen < (data.Count - pos) ? maxLen : data.Count - pos));
                pos += maxLen;
            }

            return splitedList;
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketServerChannel));
    } // class WebSocketServerChannel : ChannelBase, IDuplexChannel

    class ReadDataAsyncResultServer : AsyncResult
    {
        byte[] data = null;
        public ReadDataAsyncResultServer(WebSocketServerChannel channel, AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public void Complete(bool completedSynchronously, byte[] data)
        {
            if (data != null)
            {
                logger.DebugFormat("Complete. data length[{0}].", data.Length);
            }
            else
            {
                logger.Error("null data received");
            }

            this.data = data;

            this.Complete(completedSynchronously);
        }

        public void Complete(Exception e)
        {
            this.Complete(false, e);
        }

        public static byte[] End(IAsyncResult result)
        {
            ReadDataAsyncResultServer thisPtr = AsyncResult.End<ReadDataAsyncResultServer>(result);
            return thisPtr.data;
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(ReadDataAsyncResultServer));
    } // class ReadDataAsyncResult : AsyncResult
}
