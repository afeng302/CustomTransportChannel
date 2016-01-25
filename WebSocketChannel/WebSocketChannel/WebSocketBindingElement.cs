using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using log4net;

namespace WebSocketChannel
{
    class WebSocketBindingElement : StandardBindingElement
    {
        const string UseProxyPropertyName = "useProxy";
        const string ProxyUriPropertyName = "proxyUri";
        const string ProxyAuthUserNamePropertyName = "proxyAuthUserName";
        const string ProxyAuthPasswordPropertyName = "proxyAuthPassword";
        const string ReceiveBufferSizePropertyName = "receiveBufferSize";
        protected override Type BindingElementType
        {
            get { return typeof(WebSocketBinding); }
        }

        protected override void OnApplyConfiguration(System.ServiceModel.Channels.Binding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            if (binding.GetType() != typeof(WebSocketBinding))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    "Invalid type for binding. Expected type: {0}. Type passed in: {1}.",
                    typeof(WebSocketBinding).AssemblyQualifiedName,
                    binding.GetType().AssemblyQualifiedName));
            }

            ((WebSocketBinding)binding).UseProxy = this.UseProxy;
            ((WebSocketBinding)binding).ProxyUri = this.ProxyUri;
            ((WebSocketBinding)binding).ProxyAuthUserName = this.ProxyAuthUserName;
            ((WebSocketBinding)binding).ProxyAuthPassword = this.ProxyAuthPassword;
            ((WebSocketBinding)binding).ReceiveBufferSize = this.ReceiveBufferSize;
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                var result = base.Properties;
                result.Add(new ConfigurationProperty(UseProxyPropertyName, typeof(bool), false));
                result.Add(new ConfigurationProperty(ProxyUriPropertyName, typeof(string), string.Empty));
                result.Add(new ConfigurationProperty(ProxyAuthUserNamePropertyName, typeof(string), string.Empty));
                result.Add(new ConfigurationProperty(ProxyAuthPasswordPropertyName, typeof(string), string.Empty));
                result.Add(new ConfigurationProperty(ReceiveBufferSizePropertyName, typeof(int), 65536));

                return base.Properties;
            }
        }

        [ConfigurationProperty(UseProxyPropertyName, DefaultValue = false)]
        public bool UseProxy
        {
            get { return (bool)base[UseProxyPropertyName]; }
            set { base[UseProxyPropertyName] = value; }
        }

        [ConfigurationProperty(ProxyUriPropertyName, DefaultValue = "")]
        public string ProxyUri
        {
            get { return (string)base[ProxyUriPropertyName]; }
            set { base[ProxyUriPropertyName] = value; }
        }

        [ConfigurationProperty(ProxyAuthUserNamePropertyName, DefaultValue = "")]
        public string ProxyAuthUserName
        {
            get { return (string)base[ProxyAuthUserNamePropertyName]; }
            set { base[ProxyAuthUserNamePropertyName] = value; }
        }

        [ConfigurationProperty(ProxyAuthPasswordPropertyName, DefaultValue = "")]
        public string ProxyAuthPassword
        {
            get { return (string)base[ProxyAuthPasswordPropertyName]; }
            set { base[ProxyAuthPasswordPropertyName] = value; }
        }

        [ConfigurationProperty(ReceiveBufferSizePropertyName, DefaultValue = 65536)]
        public int ReceiveBufferSize
        {
            get { return (int)base[ReceiveBufferSizePropertyName]; }
            set { base[ReceiveBufferSizePropertyName] = value; }
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketBindingElement));
    }
}
