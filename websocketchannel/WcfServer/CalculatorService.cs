using System;
using System.ServiceModel;

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

            Console.WriteLine("OperationContext.Current.Channel.State = " + OperationContext.Current.Channel.State.ToString());

            callback.DisplayResult(x, y, result);

            Console.WriteLine("callback DisplayResult() returned.");
        }

        public void DisplayCounter()
        {
            Console.WriteLine("iCounter=" + iCounter);
        }
    }
}
