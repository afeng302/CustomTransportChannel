using System;
using WcfServer;

namespace WcfClient
{
    class CalculateCallback : ICallback
    {
        public void DisplayResult(double x, double y, double result)
        {
            Console.WriteLine("In DisplayResult: x + y = {2} when x = {0} and y = {1}", x, y, result);
        }


        public void SendBulkDataBack(byte[] data)
        {
            Console.WriteLine("received data: " + data.Length);
        }
    }
}
