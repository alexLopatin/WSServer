using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer
{
    public class MessageArgs : EventArgs
    {
        public Frame frame { get; private set; }
        public MessageArgs(Frame frame) : base()
        {
            this.frame = frame;
        }
    }
    public class CloseArgs : EventArgs
    {
        public int Code { get; private set; }
        public string Reason { get; private set; }
        public CloseArgs(int code, string reason) : base()
        {
            Code = code;
            Reason = reason;
        }
    }
    public class WebSocket
    {
        TcpListener listener;
        TcpClient client;
        public WebSocket(TcpListener listener)
        {
            this.listener = listener;
        }
        public void Send(string data)
        {
            Frame frame = new Frame(data, 1);
            byte[] bytes = frame.GetBytes();
            client.GetStream().Write(bytes, 0, bytes.Length);

        }
        private void SendClose(string reason)
        {
            Frame frame = new Frame(reason, 8);
            byte[] bytes = frame.GetBytes();
            client.GetStream().Write(bytes, 0, bytes.Length);
        }
        public EventHandler<MessageArgs> OnMessageReceived;
        public EventHandler<CloseArgs> OnClose;
        private string AcceptString(string key)
        {
            SHA1Managed sha1 = new SHA1Managed();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            return Convert.ToBase64String(hash);
        }
        public void AcceptClient()
        {
            client = listener.AcceptTcpClient();
            Header header = new Header(client.GetStream());
            //if (header["Sec-WebSocket-Version"] != "13" || header["Connection"] != "Upgrade" || header["Upgrade"] != "websocket")
            //    client.Close();
            Header answer = new Header();
            answer.type = Header.headerType.SWITCH;
            answer["Upgrade"] = "websocket";
            answer["Connection"] = "Upgrade";
            answer["Sec-WebSocket-Accept"] = AcceptString(header["Sec-WebSocket-Key"]);
            var ansBytes = answer.ToBytes();
            client.GetStream().Write(ansBytes, 0, ansBytes.Length);
        }
        private bool ToClose = false;
        public void Close(string reason)
        {
            ToClose = true;
            closeReason = reason;
        }
        string closeReason;
        public async void ListenAsync()
        {

            NetworkStream stream = client.GetStream();
            while (true)
            {
                if (ToClose)
                {
                    SendClose(closeReason);
                    if (OnClose != null)
                        OnClose.Invoke(this, new CloseArgs(1000, closeReason));
                    await Task.Delay(40);
                    client.Close();
                    break;
                }
                if (!stream.DataAvailable)
                {
                    await Task.Delay(10);
                    continue;
                }
                Frame frame = new Frame(stream);
                if (frame.opCode != 8)
                {
                    if (OnMessageReceived != null)
                        OnMessageReceived.Invoke(this, new MessageArgs(frame));
                }
                else
                {
                    Close(Encoding.UTF8.GetString(frame.message));
                }
            }
        }

    }
}
