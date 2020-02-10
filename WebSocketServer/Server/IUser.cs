using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketServer
{
    interface IUser
    {
        WebSocket webSocket { get; set; }
    }
    public class User : IUser
    {
        public WebSocket webSocket { get; set; }
        public string Username { get; set; }
        public User(WebSocket ws, string name)
        {
            webSocket = ws;
            Username = name;
        }
    }
}
