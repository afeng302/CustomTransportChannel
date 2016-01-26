using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using log4net;

namespace WebSocketChannel
{
    public class WebSocketTransportBindingElement : TransportBindingElement
    {
        public const string WebSocketScheme = "ws";

        /// <summary>
        /// To simplify the logic, we use this buffer size will be used for receive and send buffer.
        /// It is a simple fixed value for "all" buffer used in transport.
        /// There is no configuration provided for it.
        /// </summary>
        public const int MaxBufferSize = 2048 * 1024;

        public WebSocketTransportBindingElement()
            : base()
        {
        }

        public WebSocketTransportBindingElement(WebSocketTransportBindingElement elementToBeCloned)
            : base(elementToBeCloned)
        {
            this.UseProxy = elementToBeCloned.UseProxy;
            this.ProxyUri = elementToBeCloned.ProxyUri;
            this.ProxyAuthUserName = elementToBeCloned.ProxyAuthUserName;
            this.ProxyAuthPassword = elementToBeCloned.ProxyAuthPassword;
        }
        public override string Scheme
        {
            get { return WebSocketScheme; }
        }

        public override BindingElement Clone()
        {
            return new WebSocketTransportBindingElement(this);
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IDuplexSessionChannel);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            return (IChannelFactory<TChannel>)(object)new WebSocketDuplexChannelFactory(this, context);
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IDuplexSessionChannel);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            return (IChannelListener<TChannel>)(object)new WebSocketDuplexChannelListener(this, context);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(MessageVersion))
            {
                return (T)(object)MessageVersion.Soap12WSAddressing10;
            }

            return base.GetProperty<T>(context);
        }

        public bool UseProxy
        {
            get;
            set;
        }

        public string ProxyUri
        {
            get;
            set;
        }

        public string ProxyAuthUserName
        {
            get;
            set;
        }

        public string ProxyAuthPassword
        {
            get;
            set;
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketTransportBindingElement));
    }
}
