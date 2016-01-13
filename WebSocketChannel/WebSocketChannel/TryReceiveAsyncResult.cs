using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace WebSocketChannel
{
    class TryReceiveAsyncResult : AsyncResult
    {
        IDuplexChannel channel;
        bool receiveSuccess;
        Message message;
        public TryReceiveAsyncResult(TimeSpan timeout, IDuplexChannel channel, AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.channel = channel;

            bool completeSelf = true;

            try
            {
                IAsyncResult beginReceiveRequestResult = this.channel.BeginReceive(timeout, OnReceive, this);
                if (beginReceiveRequestResult.CompletedSynchronously)
                {
                    CompleteReceive(beginReceiveRequestResult);
                }
                else
                {
                    completeSelf = false;
                }
            }
            catch (TimeoutException)
            {
                // ignore the timeout exception
            }

            if (completeSelf)
            {
                base.Complete(true);
            }
        }

        void CompleteReceive(IAsyncResult result)
        {
            this.message = this.channel.EndReceive(result);
            this.receiveSuccess = true;
        }
        static void OnReceive(IAsyncResult result)
        {
            TryReceiveAsyncResult thisPtr = (TryReceiveAsyncResult)result.AsyncState;
            Exception completionException = null;
            try
            {
                thisPtr.CompleteReceive(result);
            }
            catch (TimeoutException)
            {
                // ignore the timeout exception
            }
            catch (Exception e)
            {
                completionException = e;
            }

            thisPtr.Complete(false, completionException);
        }

        public static bool End(IAsyncResult result, out Message message)
        {
            try
            {
                TryReceiveAsyncResult thisPtr = AsyncResult.End<TryReceiveAsyncResult>(result);
                message = thisPtr.message;
                return thisPtr.receiveSuccess;
            }
            catch (CommunicationException)
            {
                message = null;
                return false;
            }
        }
    }
}
