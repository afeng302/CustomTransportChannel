using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;

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

            //var bootstrap = BootstrapFactory.CreateBootstrap();

            //if (!bootstrap.Initialize())
            //{
            //    Console.WriteLine("Failed to initialize!");
            //    Console.ReadKey();
            //    return;
            //}

            //WebSocketServer server = Enumerable.First<IWorkItem>(bootstrap.AppServers) as WebSocketServer;
            //server.NewSessionConnected += wsServer_NewSessionConnected;
            //server.SessionClosed += wsServer_SessionClosed;
            //server.NewDataReceived += wsServer_NewDataReceived;
            //server.NewMessageReceived += wsServer_NewMessageReceived;

            //var result = bootstrap.Start();

            //Console.WriteLine("Start result: {0}!", result);

            //if (result == StartResult.Failed)
            //{
            //    Console.WriteLine("Failed to start!");
            //    Console.ReadKey();
            //    return;
            //}

            //Console.WriteLine("Press key 'q' to stop it!");

            //while (Console.ReadKey().KeyChar != 'q')
            //{
            //    Console.WriteLine();
            //    continue;
            //}

            //Console.WriteLine();

            ////Stop the appServer
            //bootstrap.Stop();


            ServerConfig cfg = new ServerConfig()
            {
                Port = 12012,
                Ip = "Any",
                Mode = SuperSocket.SocketBase.SocketMode.Tcp,
                //ReceiveBufferSize = 100* 1024,
                //SendBufferSize = 2048 * 1024,
                MaxRequestLength = 2048 * 1024,
                //MaxConnectionNumber = 20
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
            Console.WriteLine("wsServer_NewDataReceived. Length = [{0}]", value.Length);

            Console.WriteLine("echo back ...");

            //Thread.Sleep(200);

            // copy the data and send back
            byte[] data = new byte[100];
            //Array.Copy(value, data, value.Length);

            DateTime t0 = DateTime.Now;
            session.Send(data, 0, data.Length);
            DateTime t1 = DateTime.Now;

            Console.WriteLine("timespan: [{0}]", (t1 - t0).TotalMilliseconds);
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
