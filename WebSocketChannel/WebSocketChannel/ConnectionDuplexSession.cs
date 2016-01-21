using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketChannel
{
    class ConnectionDuplexSession : IDuplexSession
    {
        public ConnectionDuplexSession()
        {
            this.Id = Guid.NewGuid().ToString();
        }
        
        public IAsyncResult BeginCloseOutputSession(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        public IAsyncResult BeginCloseOutputSession(AsyncCallback callback, object state)
        {
            return this.BeginCloseOutputSession(TimeSpan.MaxValue, callback, state);
        }

        public void CloseOutputSession(TimeSpan timeout)
        {
            //throw new NotImplementedException();
        }

        public void CloseOutputSession()
        {
            //throw new NotImplementedException();
        }

        public void EndCloseOutputSession(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        public string Id
        {
            get;
            private set;
        }
    }
}
