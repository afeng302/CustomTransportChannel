using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using WcfServer;
using WebSocketChannel;

namespace WcfClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = WebSocketDuplexTransportBindingElement.WebSocketScheme + "://localhost:2012";
            Binding binding = new CustomBinding(new WebSocketDuplexTransportBindingElement());

            InstanceContext instanceContext = new InstanceContext(new CalculateCallback());
            EndpointAddress endpointAddress = new EndpointAddress(baseAddress);
            DuplexChannelFactory<ICalculator> factory = new DuplexChannelFactory<ICalculator>(instanceContext, binding, endpointAddress);
            ICalculator proxy = factory.CreateChannel();
            proxy.Add(2, 3);


            Console.ReadKey();
        }
    }
}
