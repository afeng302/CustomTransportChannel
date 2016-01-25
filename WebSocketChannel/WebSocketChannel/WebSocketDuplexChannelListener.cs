using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using log4net;
using SuperWebSocket;

namespace WebSocketChannel
{
    class WebSocketDuplexChannelListener : ChannelListenerBase<IDuplexSessionChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        WebSocketServer wsServer = new WebSocketServer();
        Uri uri = null;
        Queue<AcceptChannelAsyncResult> asyncResultQueue = new Queue<AcceptChannelAsyncResult>();
        Dictionary<WebSocketSession, WebSocketServerChannel> channelMap = new Dictionary<WebSocketSession, WebSocketServerChannel>();
        int receiveBufferSize;
        public WebSocketDuplexChannelListener(WebSocketTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            // populate members from binding element
            receiveBufferSize = (int)bindingElement.ReceiveBufferSize;
            this.bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, receiveBufferSize);

            Collection<MessageEncodingBindingElement> messageEncoderBindingElements
                = context.BindingParameters.FindAll<MessageEncodingBindingElement>();

            if (messageEncoderBindingElements.Count > 1)
            {
                throw new InvalidOperationException("More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext");
            }
            else if (messageEncoderBindingElements.Count == 1)
            {
                this.encoderFactory = messageEncoderBindingElements[0].CreateMessageEncoderFactory();
            }
            else
            {
                this.encoderFactory = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8).CreateMessageEncoderFactory();
            }

            this.uri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
        }
        protected override IDuplexSessionChannel OnAcceptChannel(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            AcceptChannelAsyncResult asyncResult;

            asyncResult = new AcceptChannelAsyncResult(callback, state);
            lock (this.asyncResultQueue)
            {
                this.asyncResultQueue.Enqueue(asyncResult);
            }
            return asyncResult;
        }

        protected override IDuplexSessionChannel OnEndAcceptChannel(IAsyncResult result)
        {
            return AcceptChannelAsyncResult.End(result);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Uri Uri
        {
            get { return this.uri; }
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.Start();
        }

        private void Start()
        {
            this.wsServer.Setup(this.uri.Port);
            this.wsServer.NewSessionConnected += wsServer_NewSessionConnected;
            this.wsServer.SessionClosed += wsServer_SessionClosed;
            this.wsServer.NewDataReceived += wsServer_NewDataReceived;
            this.wsServer.NewMessageReceived += wsServer_NewMessageReceived;

            this.wsServer.Start();
        }

        void wsServer_NewMessageReceived(WebSocketSession session, string value)
        {
            throw new NotImplementedException();
        }

        void wsServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            Console.WriteLine("enter wsServer_NewDataReceived()");

            WebSocketServerChannel channel = null;

            lock (this.channelMap)
            {
                if (!this.channelMap.TryGetValue(session, out channel))
                {
                    // log
                    Console.WriteLine("session not found!!!");
                    return;
                }
            }

            // log
            logger.DebugFormat("data recieved [{0}] at channel [{1}].",
                value != null ? value.Length.ToString() : "null", channel.GetHashCode());

            // receve data
            channel.ReceiveData(value);
        }

        void wsServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            WebSocketServerChannel channel = null;

            lock (this.channelMap)
            {
                if (!this.channelMap.TryGetValue(session, out channel))
                {
                    // log
                    Console.WriteLine("session not found!!!");
                    return;
                }

                this.channelMap.Remove(session);
            }

            //channel.ReceiveData(null);
            channel.Close();

            // log
            logger.InfoFormat("channel[{0}] colsed. total channel number [{1}]", channel.GetHashCode(), this.channelMap.Count);
        }

        void wsServer_NewSessionConnected(WebSocketSession session)
        {
            Console.WriteLine("enter wsServer_NewSessionConnected()");

            AcceptChannelAsyncResult aysncResult = null;

            lock (this.asyncResultQueue)
            {
                if (this.asyncResultQueue.Count == 0)
                {
                    return;
                }

                aysncResult = this.asyncResultQueue.Dequeue();
            }

            WebSocketServerChannel channel = new WebSocketServerChannel(this.encoderFactory.Encoder, this.bufferManager,
                this, session, new EndpointAddress(this.uri));
            lock (this.channelMap)
            {
                this.channelMap[session] = channel;
            }

            aysncResult.Complete(channel);

            // log
            logger.InfoFormat("new server channel[{0}] created. total channel number: [{1}]", channel.GetHashCode(), this.channelMap.Count);
        }

        private void Stop()
        {
            logger.Info("Stop()");

            this.wsServer.Stop();
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketDuplexChannelListener));

        class AcceptChannelAsyncResult : AsyncResult
        {
            IDuplexSessionChannel channel;
            public AcceptChannelAsyncResult(AsyncCallback callback, object state)
                : base(callback, state)
            {
            }

            public void Complete(IDuplexSessionChannel channel)
            {
                logger.Info("Complete()");

                // set the channel before complete
                this.channel = channel;

                // the websocket server accpet only support async operation
                this.Complete(false);
            }

            public static IDuplexSessionChannel End(IAsyncResult result)
            {
                AcceptChannelAsyncResult thisPtr = AsyncResult.End<AcceptChannelAsyncResult>(result);
                return thisPtr.channel;
            }

            private static readonly ILog logger = LogManager.GetLogger(typeof(AcceptChannelAsyncResult));
        }
    }
}
