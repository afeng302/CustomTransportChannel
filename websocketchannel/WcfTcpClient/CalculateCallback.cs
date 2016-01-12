using System;
using WcfTcpServer;

namespace WcfTcpClient
{
    class CalculateCallback : ICallback
    {
        public void DisplayResult(double x, double y, double result)
        {
            Console.WriteLine("In DisplayResult: x + y = {2} when x = {0} and y = {1}", x, y, result);
        }
    }
}
