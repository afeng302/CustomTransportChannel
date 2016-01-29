using System;
using System.ServiceModel;
using log4net;

namespace WcfServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    class CalculatorService : ICalculator
    {
        int iCounter = 0;
        public void Add(double x, double y)
        {
            iCounter += 1;

            Console.WriteLine("Enter Add(), x=" + x + ", y=" + y);

            double result = x + y;
            ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();

            callback.DisplayResult(x, y, result);

            Console.WriteLine("callback DisplayResult() returned.");
        }

        public void DisplayCounter()
        {
            Console.WriteLine("iCounter=" + iCounter);
        }

        public  void SendBulkData(byte[] data)
        {
            logger.DebugFormat("enter SendBulkData. data[{0}]", data != null ? data.Length.ToString() : "null");

            Console.WriteLine("received data: " + data.Length);

            //Console.WriteLine("sending data back ...");
            //data = new byte[data.Length];

            //ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();
            //DateTime t0 = DateTime.Now;
            //callback.SendBulkDataBack(data);
            //DateTime t1 = DateTime.Now;
            //Console.WriteLine("timespan: " + (t1 - t0).TotalMilliseconds);
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(CalculatorService));
    }
}
