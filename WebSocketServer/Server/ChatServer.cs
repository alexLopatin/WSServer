using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketServer
{
    class ChatServer : Server
    {

        public void MessageHandler(object sender, MessageArgs args)
        {
            var user = (User)ConnectedUsers.Find(p => p.webSocket == sender);
            if(args.frame.opCode == 1)
                SendAll(user.Username + ": " + Encoding.UTF8.GetString(args.frame.message));
        }
        public string GenerateName()
        {
            int i = 1;
            while(true)
            {
                string name = "User #" + i.ToString();
                if (ConnectedUsers.Exists(p => ((User)p).Username == name))
                    i++;
                else
                    return name;
            }
        }
        public override void OnWebSocketOpen(WebSocket webSocket)
        {
            User user = new User(webSocket, GenerateName());
            webSocket.OnClose += (object sender, CloseArgs args) => { 
                ConnectedUsers.RemoveAll(p => p.webSocket == webSocket);
                SendAll(user.Username + " disconnected");
            };
            webSocket.OnMessageReceived += MessageHandler;
            ConnectedUsers.Add(user);
            SendAll(user.Username + " connected!");
        }
    }
}
