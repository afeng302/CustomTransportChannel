using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using log4net;

namespace WebSocketChannel
{
    class WebSocketBinding : Binding
    {
        readonly MessageEncodingBindingElement messageElement;
        readonly WebSocketTransportBindingElement transportElement;

        public WebSocketBinding()
        {
            this.messageElement = new TextMessageEncodingBindingElement();
            this.transportElement = new WebSocketTransportBindingElement();
        }

        public WebSocketBinding(string configurationName)
            : this()
        {
            WebSocketBindingCollectionElement section = (WebSocketBindingCollectionElement)ConfigurationManager.GetSection(
                "system.serviceModel/bindings/webSocketTransportBinding");
            WebSocketBindingElement element = section.Bindings[configurationName];
            if (element == null)
            {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                    "There is no binding named {0} at {1}.", configurationName, section.BindingName));
            }
            else
            {
                element.ApplyConfiguration(this);
            }
        }

        public override BindingElementCollection CreateBindingElements()
        {
            return new BindingElementCollection(new BindingElement[] {
                this.messageElement,
                this.transportElement
            });
        }

        public override string Scheme
        {
            get { return this.transportElement.Scheme; }
        }

        public bool UseProxy
        {
            get { return this.transportElement.UseProxy; }
            set { this.transportElement.UseProxy = value; }
        }

        public string ProxyUri
        {
            get { return this.transportElement.ProxyUri; }
            set { this.transportElement.ProxyUri = value; }
        }

        public string ProxyAuthUserName
        {
            get { return this.transportElement.ProxyAuthUserName; }
            set { this.transportElement.ProxyAuthUserName = value; }
        }

        public string ProxyAuthPassword
        {
            get { return this.transportElement.ProxyAuthPassword; }
            set { this.transportElement.ProxyAuthPassword = value; }
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketBinding));
    }
}
