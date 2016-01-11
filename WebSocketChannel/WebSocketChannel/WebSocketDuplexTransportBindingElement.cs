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
            return typeof(TChannel) == typeof(IDuplexChannel);
        }
    }
}
