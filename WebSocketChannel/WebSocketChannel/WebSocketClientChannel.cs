using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using log4net;
using WebSocket4Net;

namespace WebSocketChannel
{
    class WebSocketClientChannel : ChannelBase, IDuplexSessionChannel
    {
        const int maxBufferSize = 2 * 1024000;

        MessageEncoder encoder;
        BufferManager bufferManager;

        WebSocket wsClient = null;
        EndpointAddress remoteAddress = null;
        Uri via = null;

        OpenAsyncResult openAsyncResult;

        Queue<byte[]> receivedDataQueue = new Queue<byte[]>();
        Queue<ReadDataAsyncResultClient> readDataAsyncResultQueue = new Queue<ReadDataAsyncResultClient>();
        public WebSocketClientChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager,
            WebSocket wsClient, EndpointAddress remoteAddress, Uri via)
            : base(channelManager)
        {
            this.encoder = encoder;
            this.bufferManager = bufferManager;

            this.wsClient = wsClient;
            this.remoteAddress = remoteAddress;
            this.via = via;

            this.wsClient.Closed += wsClient_Closed;
            this.wsClient.DataReceived += wsClient_DataReceived;
            this.wsClient.MessageReceived += wsClient_MessageReceived;
            this.wsClient.Error += wsClient_Error;
            this.wsClient.Opened += wsClient_Opened;

            this.Session = new ConnectionDuplexSession();
        }


        void wsClient_Opened(object sender, EventArgs e)
        {
            if (this.openAsyncResult != null)
            {
                this.openAsyncResult.Complete();
            }
        }

        void wsClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            // log the error
            logger.ErrorFormat("websocket error happened. message[{0}]\r\nStackTrace[{1}]",
                e.Exception.Message, e.Exception.StackTrace);

            // put the channel into Faulted state
            if ((this.openAsyncResult != null) && !this.openAsyncResult.IsCompleted)
            {
                this.openAsyncResult.Complete();
            }
            this.Fault();
        }

        byte[] recvBuffer = null;
        int bufferPos = -1;
        int expectLen = -1;

        DateTime t0, t1;
        void wsClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                logger.Error("null data received.");
                return;
            }

            // get length data
            if (this.bufferPos == -1)
            {
                if (e.Data.Length != 4)
                {
                    logger.ErrorFormat("error length data. [{0}]", e.Data.Length);
                    return;
                }

                // length data
                this.expectLen = BitConverter.ToInt32(e.Data, 0);
                this.recvBuffer = new byte[this.expectLen];
                this.bufferPos = 0;

                return;
            }

            // fill buffer
            Array.Copy(e.Data, 0, this.recvBuffer, this.bufferPos, e.Data.Length);
            this.bufferPos += e.Data.Length;
            if (this.bufferPos < this.expectLen)
            {
                return; // not full
            }
            this.bufferPos = -1;

            // complete the read async result
            lock (this.readDataAsyncResultQueue)
            {
                if (this.readDataAsyncResultQueue.Count > 0)
                {
                    ReadDataAsyncResultClient result = this.readDataAsyncResultQueue.Dequeue();
                    result.Complete(false, this.recvBuffer);

                    return;
                }
            }

            // there is no read data async result, cache the received data
            lock (this.receivedDataQueue)
            {
                this.receivedDataQueue.Enqueue(this.recvBuffer);
            }
        }

        void wsClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void wsClient_Closed(object sender, EventArgs e)
        {
            logger.InfoFormat("Channel[{0}] closed.", sender.GetHashCode());
        }

        protected override void OnAbort()
        {
            logger.Error("OnAbort()");

            if (wsClient.State == WebSocketState.Open)
            {
                wsClient.Close();
            }
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnClose(timeout);

            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.openAsyncResult = new OpenAsyncResult(callback, state);

            this.wsClient.Open();

            return this.openAsyncResult;
        }

        protected override void OnClose(TimeSpan timeout)
        {
            logger.Info("OnClose()");

            if (wsClient.State == WebSocketState.Open)
            {
                wsClient.Close();
            }
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            OpenAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.openAsyncResult = new OpenAsyncResult(null, null);
            this.wsClient.Open();

            OpenAsyncResult.End(this.openAsyncResult);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ReadDataAsyncResultClient result = new ReadDataAsyncResultClient(callback, state);

            // if there is already data received, complete the read async result immediately 
            lock (this.receivedDataQueue)
            {
                if (this.receivedDataQueue.Count > 0)
                {
                    result.Complete(true, this.receivedDataQueue.Dequeue());

                    return result;
                }
            }

            // there is no data received, wait for data arrive
            lock (this.readDataAsyncResultQueue)
            {
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
            byte[] data = ReadDataAsyncResultClient.End(result);
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
            get { return null; }
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

        bool inited = false;
        public void Send(Message message, TimeSpan timeout)
        {
            logger.DebugFormat("sending message...");

            base.ThrowIfDisposedOrNotOpen();

            ArraySegment<byte> encodedBytes = default(ArraySegment<byte>);

            try
            {
                encodedBytes = this.EncodeMessage(message);


                //// send pad data
                //if (!inited)
                //{
                //    inited = true;
                //    byte[] pad = new byte[1024999];
                //    this.wsClient.Send(pad, 0, 1024999);
                //}                

                List<ArraySegment<byte>> segments = this.SplitData(encodedBytes);
                byte[] lenArray = BitConverter.GetBytes(encodedBytes.Count);                
                segments.Insert(0, new ArraySegment<byte>(lenArray, 0, 4));

                // send message
                foreach (var item in segments)
                {
                    this.wsClient.Send(item.Array, item.Offset, item.Count);
                }
                
                //foreach (var item in segments)
                //{
                //    this.wsClient.Send(item.Array, item.Offset, item.Count);
                //}

                logger.DebugFormat("sent message[{0}].", encodedBytes.Count);
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
            get { return this.via; }
        }

        ArraySegment<byte> EncodeMessage(Message message)
        {
            logger.DebugFormat("Encoding message...");

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
            logger.DebugFormat("Decoding message...");

            //// take buffer from buffer manager
            //// the message will be closed later and the buffer will be retured to buffer manager
            byte[] buffer = this.bufferManager.TakeBuffer(data.Length);
            data.CopyTo(buffer, 0);

            // Note that we must set the ArraySegment count as data length. The buffer taken from buffer Manager
            // may have more space for the length required.
            ArraySegment<byte> encodedBytes = new ArraySegment<byte>(buffer, 0, data.Length);

            Message msg = this.encoder.ReadMessage(encodedBytes, this.bufferManager);

            logger.DebugFormat("Decoded message.");

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
            int maxLen = 32 * 1024; // 32K
            while (pos < data.Count)
            {
                splitedList.Add(new ArraySegment<byte>(data.Array, pos, maxLen < (data.Count - pos) ? maxLen : data.Count - pos));
                pos += maxLen;
            }

            return splitedList;
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketClientChannel));

    } // class WebSocketClientChannel : ChannelBase, IDuplexChannel

    class OpenAsyncResult : AsyncResult
    {
        public OpenAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public void Complete()
        {
            logger.Info("Complete()");

            this.Complete(false);
        }

        public void Complete(Exception e)
        {
            logger.InfoFormat("Complete(). [{0}]\r\n[{1}]", e.Message, e.StackTrace);

            this.Complete(false, e);
        }

        public static void End(IAsyncResult result)
        {
            AsyncResult.End<OpenAsyncResult>(result);
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(OpenAsyncResult));

    } // class OpenAsyncResult


    class ReadDataAsyncResultClient : AsyncResult
    {
        byte[] data = null;
        public ReadDataAsyncResultClient(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public void Complete(bool completedSynchronously, byte[] data)
        {
            logger.DebugFormat("Complete() data length[{0}]", data != null ? data.Length.ToString() : "null");

            this.data = data;

            this.Complete(completedSynchronously);
        }

        public void Complete(Exception e)
        {
            this.Complete(false, e);
        }

        public static byte[] End(IAsyncResult result)
        {
            ReadDataAsyncResultClient thisPtr = AsyncResult.End<ReadDataAsyncResultClient>(result);
            return thisPtr.data;
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(ReadDataAsyncResultClient));

    } // class ReadDataAsyncResult
}
