using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using WebSocket4Net;

namespace WebSocketChannel
{
    class WebSocketClientChannel : ChannelBase, IDuplexChannel
    {
        MessageEncoder encoder;
        BufferManager bufferManager;

        WebSocket wsClient = null;
        EndpointAddress remoteAddress = null;
        Uri via = null;

        OpenAsyncResult openAsyncResult;

        Queue<byte[]> receivedDataQueue = new Queue<byte[]>();
        Queue<ReadDataAsyncResult> readDataAsyncResultQueue = new Queue<ReadDataAsyncResult>();
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
                    ReadDataAsyncResult result = this.readDataAsyncResultQueue.Dequeue();
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
            ReadDataAsyncResult result = new ReadDataAsyncResult(this, callback, state);

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
            byte[] data = ReadDataAsyncResult.End(result);
            if (data == null)
            {
                return null;
            }

            ArraySegment<byte> encodedBytes = new ArraySegment<byte>(data, 0, data.Length);
            return this.encoder.ReadMessage(encodedBytes, this.bufferManager);
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
            throw new NotImplementedException();
        }

        public void Send(Message message)
        {
            throw new NotImplementedException();
        }

        public Uri Via
        {
            get { return this.via; }
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
    }



    class ReadDataAsyncResult : AsyncResult
    {
        byte[] data = null;
        public ReadDataAsyncResult(WebSocketClientChannel channel, AsyncCallback callback, object state)
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
            ReadDataAsyncResult thisPtr = AsyncResult.End<ReadDataAsyncResult>(result);
            return thisPtr.data;
        }
    }
}
