using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;

namespace WebSocketChannel
{
    class WebSocketDuplexChannelFactory : ChannelFactoryBase<IDuplexChannel>
    {
        public WebSocketDuplexChannelFactory(WebSocketDuplexTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
        }

        protected override IDuplexChannel OnCreateChannel(System.ServiceModel.EndpointAddress address, Uri via)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
