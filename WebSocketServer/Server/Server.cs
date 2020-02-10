using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;

namespace WebSocketServer
{
    abstract class Server
    {
        protected List<IUser> ConnectedUsers = new List<IUser>();
        TcpListener listener;
        public Server()
        {
            IPAddress addr = IPAddress.Parse("127.0.0.1");
            listener = new TcpListener(addr, 80);
        }
        public void SendAll(object message)
        {
            foreach (IUser user in ConnectedUsers)
                user.webSocket.Send(message.ToString());
        }
        public abstract void OnWebSocketOpen(WebSocket webSocket);
        public async void Run()
        {
            listener.Start();

            while (true)
            {
                WebSocket ws = new WebSocket(listener);
                ws.AcceptClient();
                await Task.Run(() => ws.ListenAsync());
                OnWebSocketOpen(ws);
            }
        }
    }
}
