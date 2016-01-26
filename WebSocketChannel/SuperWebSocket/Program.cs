using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;

namespace SuperWebSocket
{
    class Program
    {
        static List<WebSocketSession> SessionLst = new List<WebSocketSession>();

        static void Main(string[] args)
        {
            //Console.WriteLine("Press any key to start the server!");

            //Console.ReadKey();
            //Console.WriteLine();

            ServerConfig cfg = new ServerConfig()
            {
                Port = 2012,
                Ip = "Any",
                Mode = SuperSocket.SocketBase.SocketMode.Tcp,
                ReceiveBufferSize = 2048 * 1024,
                SendBufferSize = 2048 * 1024,
                MaxRequestLength = 2048 * 1024,
                MaxConnectionNumber = 20
            };

            WebSocketServer wsServer = new WebSocketServer();
            if (!wsServer.Setup(cfg))
            {
                Console.WriteLine("Failed to setup!");
                Console.ReadKey();
                return;
            }

            wsServer.NewSessionConnected += wsServer_NewSessionConnected;
            wsServer.SessionClosed += wsServer_SessionClosed;
            wsServer.NewDataReceived += wsServer_NewDataReceived;
            wsServer.NewMessageReceived += wsServer_NewMessageReceived;



            if (!wsServer.Start())
            {
                Console.WriteLine("Failed to start!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("The server started successfully, press key 'q' to stop it!");

            while (Console.ReadKey().KeyChar != 'q')
            {
                Console.WriteLine();
                continue;
            }

            //Stop the appServer
            wsServer.Stop();

            Console.WriteLine("The server was stopped!");
            Console.ReadKey();
        }



        static void wsServer_NewMessageReceived(WebSocketSession session, string value)
        {
            Console.WriteLine("wsServer_NewMessageReceived: [" + value + "]");

            Console.WriteLine("will echo back ...");
            Thread.Sleep(1000);

            session.Send(value);
        }

        static void wsServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            Console.WriteLine("wsServer_NewDataReceived. Length= [" + value.Length + "]");

            Console.WriteLine("will echo back ...");
            //Thread.Sleep(1000);

            // copy the data and send back
            byte[] data = new byte[value.Length];
            Array.Copy(value, data, value.Length);

            session.Send(data, 0, data.Length);
        }

        static void wsServer_NewSessionConnected(WebSocketSession session)
        {
            Console.WriteLine("wsServer_NewSessionConnected. SessionCount=["
                + session.AppServer.SessionCount.ToString() + "]");
        }

        static void wsServer_SessionClosed(WebSocketSession session, CloseReason value)
        {
            Console.WriteLine("wsServer_SessionClosed. SessionCount=["
                + session.AppServer.SessionCount.ToString() + "] CloseReason=[" + value.ToString() + "]");
        }
    }
}
