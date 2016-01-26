using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using CustomTcpDuplex.Channels;
using log4net.Config;
using WcfServer;
using WebSocketChannel;

namespace WcfClient
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            //string baseAddress = WebSocketDuplexTransportBindingElement.WebSocketScheme + "://112.74.207.57:12012";
            //string baseAddress = WebSocketDuplexTransportBindingElement.WebSocketScheme + "://a23126-04:12012";
            //string baseAddress = WebSocketTransportBindingElement.WebSocketScheme + "://localhost:12012";
            //Binding binding = new CustomBinding(new WebSocketTransportBindingElement());

            //string baseAddress = "net.tcp://localhost:9999";
            //Binding binding = new CustomBinding(new NetTcpBinding());

            //string baseAddress = "http://localhost:9999";
            //Binding binding = new CustomBinding(new WSDualHttpBinding());

            //string baseAddress = SizedTcpDuplexTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            //Binding binding = new CustomBinding(new SizedTcpDuplexTransportBindingElement());


            //InstanceContext instanceContext = new InstanceContext(new CalculateCallback());
            //EndpointAddress endpointAddress = new EndpointAddress(baseAddress);
            //DuplexChannelFactory<ICalculator> factory = new DuplexChannelFactory<ICalculator>(instanceContext, binding, endpointAddress);

            //factory.Endpoint.EndpointBehaviors.Add(new ClientViaBehavior(new Uri("net.tcp://localhost:19999")));

            InstanceContext instanceContext = new InstanceContext(new CalculateCallback());
            DuplexChannelFactory<ICalculator> factory = new DuplexChannelFactory<ICalculator>(instanceContext, "CalculatorClient");

            ICalculator proxy = factory.CreateChannel();
            //proxy.Add(2, 3);

            //proxy.DisplayCounter();

            //Console.WriteLine("Add() returned.");

            byte[] data = new byte[1024000];           
            while (true)
            {
                Console.WriteLine("sending data");

                DateTime t0 = DateTime.Now;
                proxy.SendBulkData(data);
                DateTime t1 = DateTime.Now;

                Console.WriteLine("timespan: " + (t1 - t0).TotalMilliseconds);

                System.Threading.Thread.Sleep(1000);
            }

        }
    }
}
