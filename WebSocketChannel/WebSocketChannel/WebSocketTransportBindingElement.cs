using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;

namespace WebSocketChannel
{
    public class WebSocketTransportBindingElement : TransportBindingElement
    {
        public const string WebSocketScheme = "ws";

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
            this.ReceiveBufferSize = elementToBeCloned.ReceiveBufferSize;
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

        public int ReceiveBufferSize
        {
            get;
            set;
        }
    }
}
