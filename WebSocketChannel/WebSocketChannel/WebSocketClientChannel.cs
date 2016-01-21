using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using WebSocket4Net;

namespace WebSocketChannel
{
    class WebSocketClientChannel : ChannelBase, IDuplexSessionChannel
    {
        const int maxBufferSize = 64 * 1024;

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
            if (this.openAsyncResult != null)
            {
                this.openAsyncResult.Complete(e.Exception);
            }
        }

        void wsClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                // log
                return;
            }

            // complete the read async result
            lock (this.readDataAsyncResultQueue)
            {
                if (this.readDataAsyncResultQueue.Count > 0)
                {
                    ReadDataAsyncResultClient result = this.readDataAsyncResultQueue.Dequeue();
                    result.Complete(false, e.Data);

                    return;
                }
            }

            // there is no read data async result, cache the received data
            lock (this.receivedDataQueue)
            {
                this.receivedDataQueue.Enqueue(e.Data);
            }
        }

        void wsClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void wsClient_Closed(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void OnAbort()
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.openAsyncResult = new OpenAsyncResult(callback, state);

            this.wsClient.Open();

            return this.openAsyncResult;
        }

        protected override void OnClose(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            throw new NotImplementedException();
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

        public void Send(Message message, TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();

            ArraySegment<byte> encodedBytes = default(ArraySegment<byte>);

            try
            {
                encodedBytes = this.EncodeMessage(message);

                this.wsClient.Send(encodedBytes.Array, encodedBytes.Offset, encodedBytes.Count);
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
            try
            {
                return encoder.WriteMessage(message, maxBufferSize, this.bufferManager);
            }
            finally
            {
                // we've consumed the message by serializing it, so clean up
                message.Close();
            }
        }

        Message DecodeMessage(byte[] data)
        {
            // take buffer from buffer manager
            // the message will be closed later and the buffer will be retured to buffer manager
            byte[] buffer = this.bufferManager.TakeBuffer(data.Length);
            data.CopyTo(buffer, 0);

            // Note that we must set the ArraySegment count as data length. The buffer taken from buffer Manager
            // may have more space for the length required.
            ArraySegment<byte> encodedBytes = new ArraySegment<byte>(buffer, 0, data.Length);

            Message msg = this.encoder.ReadMessage(encodedBytes, this.bufferManager);

            return msg;
        }



        public IDuplexSession Session
        {
            get;
            private set;
        }
    } // class WebSocketClientChannel : ChannelBase, IDuplexChannel

    class OpenAsyncResult : AsyncResult
    {
        public OpenAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public void Complete()
        {
            this.Complete(false);
        }

        public void Complete(Exception e)
        {
            this.Complete(false, e);
        }

        public static void End(IAsyncResult result)
        {
            AsyncResult.End<OpenAsyncResult>(result);
        }
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
    } // class ReadDataAsyncResult
}
