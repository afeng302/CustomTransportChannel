using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using SuperWebSocket;

namespace WebSocketChannel
{
    class WebSocketServerChannel : ChannelBase, IDuplexChannel
    {
        const int maxBufferSize = 64 * 1024;

        MessageEncoder encoder;
        BufferManager bufferManager;

        WebSocketSession wsServer = null;
        EndpointAddress localAddress = null;

        Queue<byte[]> receivedDataQueue = new Queue<byte[]>();
        Queue<ReadDataAsyncResultServer> readDataAsyncResultQueue = new Queue<ReadDataAsyncResultServer>();

        /// <summary>
        /// Use the same locker to operate receivedDataQueue and readDataAsyncResultQueue. Otherwise, the data may be lost
        /// </summary>
        object recvLocker = new object();
        public WebSocketServerChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager,
            WebSocketSession wsServer, EndpointAddress localAddress)
            : base(channelManager)
        {
            this.encoder = encoder;
            this.bufferManager = bufferManager;

            this.wsServer = wsServer;
            this.localAddress = localAddress;
            //this.localAddress = new EndpointAddress("ws://localhost");
        }
        protected override void OnAbort()
        {
            throw new NotImplementedException();
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
            this.wsServer.Close();
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
            get { return null; }
        }

        public void Send(Message message, TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();

            ArraySegment<byte> encodedBytes = default(ArraySegment<byte>);

            try
            {
                encodedBytes = this.EncodeMessage(message);

                this.wsServer.Send(encodedBytes.Array, encodedBytes.Offset, encodedBytes.Count);
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

        public void ReceiveData(byte[] data)
        {
            if (data == null)
            {
                // log
                return;
            }

            lock (this.recvLocker)
            {
                // complete the read async result
                if (this.readDataAsyncResultQueue.Count > 0)
                {
                    ReadDataAsyncResultServer result = this.readDataAsyncResultQueue.Dequeue();
                    result.Complete(false, data);

                    return;
                }

                // there is no read data async result, cache the received data
                this.receivedDataQueue.Enqueue(data);
            }
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
            if (msg != null)
            {
                msg.Headers.To = this.LocalAddress.Uri;
                //RemoteEndpointMessageProperty prop = new RemoteEndpointMessageProperty(this.LocalAddress.Uri.Host, this.LocalAddress.Uri.Port);
                //msg.Properties.Add(RemoteEndpointMessageProperty.Name, prop);
            }

            return msg;
        }

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
    } // class ReadDataAsyncResult : AsyncResult
}
