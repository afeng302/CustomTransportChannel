using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;

namespace WebSocketChannel
{
    public class WebSocketDuplexTransportBindingElement : TransportBindingElement
    {
        public const string WebSocketScheme = "ws";

        public WebSocketDuplexTransportBindingElement()
            : base()
        {
        }

        public WebSocketDuplexTransportBindingElement(WebSocketDuplexTransportBindingElement other)
            : base(other)
        {
        }
        public override string Scheme
        {
            get { return WebSocketScheme; }
        }

        public override BindingElement Clone()
        {
            return new WebSocketDuplexTransportBindingElement(this);
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
    }
}
