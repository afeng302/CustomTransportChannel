using System.ServiceModel;

namespace WcfServer
{
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void DisplayResult(double x, double y, double result);
    }

    [ServiceContract(Name = "CalculatorService", //Namespace = "http://www.artechartechartech.com/", 
        CallbackContract = typeof(ICallback))]
    public interface ICalculator
    {
        [OperationContract]//(IsOneWay = true)]
        void Add(double x, double y);
    }
}
