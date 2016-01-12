using System;
using System.ServiceModel;

namespace WcfServer
{
    class CalculatorService : ICalculator
    {
        public void Add(double x, double y)
        {
            Console.WriteLine("Enter Add(), x=" + x + ", y=" + y);

            double result = x + y;
            ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();
            callback.DisplayResult(x, y, result);

            Console.WriteLine("callback DisplayResult() returned.");
        }
    }
}
