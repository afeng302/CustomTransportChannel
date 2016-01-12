using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace WcfTcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(CalculatorService)))
            {
                host.Open();

                Console.WriteLine("Server started");

                //using (DuplexChannelFactory<ICalculator> channelFactory = new DuplexChannelFactory<ICalculator>(instanceContext, "CalculatorService"))
                //{
                //    ICalculator proxy = channelFactory.CreateChannel();
                //    using (proxy as IDisposable)
                //    {
                //        proxy.Add(1, 2);
                //        //Console.Read();
                //    }
                //}

                Console.Read();
            }
        }
    }
}
