using System;
using System.Collections.ObjectModel;
using System.Net;
using System.ServiceModel.Channels;
using System.Text;
using log4net;
using WebSocket4Net;

namespace WebSocketChannel
{
    class WebSocketDuplexChannelFactory : ChannelFactoryBase<IDuplexSessionChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;

        bool useProxy;
        string proxyAuthUserName;
        string proxyAuthPassword;
        Uri proxyUri;
        public WebSocketDuplexChannelFactory(WebSocketTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            // populate members from binding element
            this.bufferManager = BufferManager.CreateBufferManager(WebSocketTransportBindingElement.MaxBufferSize * 10,
                WebSocketTransportBindingElement.MaxBufferSize);

            //Collection<MessageEncodingBindingElement> messageEncoderBindingElements
            //    = context.BindingParameters.FindAll<MessageEncodingBindingElement>();

            //if (messageEncoderBindingElements.Count > 1)
            //{
            //    throw new InvalidOperationException("More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext");
            //}
            //else if (messageEncoderBindingElements.Count == 1)
            //{
            //    this.encoderFactory = messageEncoderBindingElements[0].CreateMessageEncoderFactory();
            //}
            //else
            //{
            this.encoderFactory = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8).CreateMessageEncoderFactory();
            //BinaryMessageEncodingBindingElement encodingBindingElement = new BinaryMessageEncodingBindingElement();
            //encodingBindingElement.MessageVersion = MessageVersion.Soap12WSAddressing10;
            //this.encoderFactory = encodingBindingElement.CreateMessageEncoderFactory();
            //}

            this.useProxy = bindingElement.UseProxy;
            this.proxyUri = string.IsNullOrEmpty(bindingElement.ProxyUri) ? null : new Uri(bindingElement.ProxyUri);
            this.proxyAuthUserName = bindingElement.ProxyAuthUserName;
            this.proxyAuthPassword = bindingElement.ProxyAuthPassword;
        }

        protected override IDuplexSessionChannel OnCreateChannel(System.ServiceModel.EndpointAddress address, Uri via)
        {
            logger.InfoFormat("OnCreateChannel(). address[{0}], via[{1}]", address, via);

            WebSocket wsSocket = new WebSocket(address.Uri.ToString());
            //wsSocket.ReceiveBufferSize = 1024000; //WebSocketTransportBindingElement.MaxBufferSize;

            //wsSocket.NoDelay = true;

            // check if need web proxy to access server
            if (this.useProxy)
            {
                Uri serviceUrl = new Uri(string.Format("http://{0}", address.Uri.Host));

                // if there is no proxy uri specified, use the system default proxy.
                if (this.proxyUri == null)
                {
                    Uri proxyUri = WebRequest.DefaultWebProxy.GetProxy(serviceUrl);
                    if (serviceUrl != proxyUri)
                    {
                        this.proxyUri = proxyUri;
                    }
                }

                // if there is proxy setted/pretected, use the proxy for the connection.
                if (this.proxyUri != null)
                {
                    Console.WriteLine("use proxy: [" + this.proxyUri.ToString() + "]");
                    logger.InfoFormat("http proxy[{0}] will be used in the connection.", this.proxyUri);

                    IPAddress ip;
                    EndPoint proxyEndPoint;
                    if (IPAddress.TryParse(this.proxyUri.Host, out ip))
                    {
                        proxyEndPoint = new IPEndPoint(ip, this.proxyUri.Port);
                    }
                    else
                    {
                        proxyEndPoint = new DnsEndPoint(this.proxyUri.Host, this.proxyUri.Port);
                    }

                    HttpConnectProxy proxy = new HttpConnectProxy(proxyEndPoint);
                    proxy.UserName = this.proxyAuthUserName;
                    proxy.Password = this.proxyAuthPassword;

                    wsSocket.Proxy = proxy;
                }
            }

            return new WebSocketClientChannel(this.encoderFactory.Encoder, this.bufferManager, this, wsSocket, address, via);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            logger.Info("OnOpen");
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketDuplexChannelFactory));
    }
}
