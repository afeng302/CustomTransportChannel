using System.ServiceModel;

namespace WcfServer
{
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void DisplayResult(double x, double y, double result);

        [OperationContract(IsOneWay = true)]
        void SendBulkDataBack(byte[] data);
    }

    [ServiceContract(Name = "CalculatorService", SessionMode = SessionMode .Required, CallbackContract = typeof(ICallback))]
    public interface ICalculator
    {
        [OperationContract]//(IsOneWay = true)]
        void Add(double x, double y);

        [OperationContract]
        void DisplayCounter();

        [OperationContract]
        void SendBulkData(byte[] data);
    }
}
