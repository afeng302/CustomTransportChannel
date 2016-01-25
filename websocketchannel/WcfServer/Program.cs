using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using CustomTcpDuplex.Channels;
using log4net.Config;
using WebSocketChannel;

namespace WcfServer
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            //string baseAddress = WebSocketTransportBindingElement.WebSocketScheme + "://localhost:12012";
            //ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddress));
            //Binding binding = new CustomBinding(new WebSocketTransportBindingElement());

            //string baseAddress = "net.tcp://localhost:9999";
            //ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddress));
            //Binding binding = new CustomBinding(new NetTcpBinding());

            //string baseAddress = "http://localhost:9999";
            //ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddress));
            //Binding binding = new CustomBinding(new WSDualHttpBinding());

            //string baseAddress = SizedTcpDuplexTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            //ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddress));
            //Binding binding = new CustomBinding(new SizedTcpDuplexTransportBindingElement());

            //ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ICalculator), binding, "");


            ServiceHost host = new ServiceHost(typeof(CalculatorService));
            host.Open();
            Console.WriteLine("Host opened");

            Console.ReadKey();
        }
    }
}
