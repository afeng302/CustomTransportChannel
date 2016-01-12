using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using WebSocketChannel;

namespace WcfServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = WebSocketDuplexTransportBindingElement.WebSocketScheme + "://localhost:2012";
            ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddress));
            Binding binding = new CustomBinding(new WebSocketDuplexTransportBindingElement());

            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ICalculator), binding, "");

            host.Open();
            Console.WriteLine("Host opened");

            Console.ReadKey();
        }
    }
}
