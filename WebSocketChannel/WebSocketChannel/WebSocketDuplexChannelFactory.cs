using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using WebSocket4Net;

namespace WebSocketChannel
{
    class WebSocketDuplexChannelFactory : ChannelFactoryBase<IDuplexChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        public WebSocketDuplexChannelFactory(WebSocketDuplexTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            // populate members from binding element
            int maxBufferSize = (int)bindingElement.MaxReceivedMessageSize;
            this.bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, maxBufferSize);

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
        }

        protected override IDuplexChannel OnCreateChannel(System.ServiceModel.EndpointAddress address, Uri via)
        {
            WebSocket wsSocket = new WebSocket(address.Uri.ToString());

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
        }
    }
}
