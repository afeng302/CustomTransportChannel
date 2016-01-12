using System;
using System.ServiceModel;
using WcfTcpServer;

namespace WcfTcpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ICalculator proxy = null;
            InstanceContext instanceContext = new InstanceContext(new CalculateCallback());

            using (DuplexChannelFactory<ICalculator> channelFactory = new DuplexChannelFactory<ICalculator>(instanceContext, "CalculatorService"))
            {
                proxy = channelFactory.CreateChannel();
                using (proxy as IDisposable)
                {
                    Console.WriteLine("proxy.Add(1, 2)");
                    proxy.Add(1, 2);
                    Console.WriteLine("proxy.Add(1, 2) returned.");

                    Console.Read();
                }
            }
        }
    }
}
