using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;

namespace WebSocketServer
{
    class Server
    {
        List<WebSocket> wsockets = new List<WebSocket>();
        TcpListener listener;
        public Server()
        {
            IPAddress addr = IPAddress.Parse("127.0.0.1");
            listener = new TcpListener(addr, 80);
        }
        public async void Run()
        {
            listener.Start();

            while (true)
            {
                WebSocket ws = new WebSocket(listener);
                ws.AcceptClient();
                ws.OnClose += (object sender, CloseArgs args) => { wsockets.Remove(ws); };
                await Task.Run(() => ws.ListenAsync());
                wsockets.Add(ws);
            }
        }
    }
}
