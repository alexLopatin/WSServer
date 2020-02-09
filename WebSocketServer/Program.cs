using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections;
using System.Net.Security;

namespace WebSocketServer
{
    class Header
    {
        private Dictionary<string, string> data = new Dictionary<string, string>();
        public enum headerType { GET, POST, SWITCH }
        public headerType type { get; set; }
        public string address { get; set; }
        public Header()
        {

        }
        public Header(NetworkStream stream)
        {
            StringBuilder str = new StringBuilder();
            int bytes = 0;
            byte[] dataB = new byte[256];
            do
            {
                bytes = stream.Read(dataB, 0, dataB.Length);
                str.Append(Encoding.UTF8.GetString(dataB, 0, bytes));
            }

            while (stream.DataAvailable);
            string s = str.ToString();
            s = s.Replace("\r", "");
            var lines = s.Split('\n');
            string first = lines[0];
            var firstSplit = first.Split(' ');
            if (firstSplit[0] == "POST")
                type = headerType.POST;
            else
                type = headerType.GET;
            
            
            address = firstSplit[1];
            foreach (string line in lines)
            {
                var brk = line.Split(": ");
                if (brk.Length == 2)
                    data[brk[0]] = brk[1];
            }
        }
        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            if (type == headerType.SWITCH)
                res.Append("HTTP/1.1 101 Switching Protocols\r\n");
            foreach (var kvp in data)
                res.Append( kvp.Key + ": " + kvp.Value + "\r\n");
            return res.ToString() + "\r\n";
        }
        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
        public string this[string index]
        {
            get { 
                return data[index]; }
            set { data[index] = value; }
        }
    }
    
    public class Frame
    {
        public byte[] message;
        public byte opCode;
        public int length;
        private byte[] Append(byte[] arr, byte[] data, int bytes)
        {
            byte[] newArr = new byte[arr.Length + bytes];
            arr.CopyTo(newArr, 0);
            for (int i = 0; i < bytes; i++)
                newArr[i + arr.Length] = data[i];
            return newArr;
        }
        public Frame(NetworkStream stream)
        {
            data = new byte[0];
            byte[] dataB = new byte[256];
            do
            {
                int bytes = stream.Read(dataB, 0, dataB.Length);
                data = Append(data, dataB, bytes);
            }
            while (stream.DataAvailable);
            Read(data);
        }
        public byte[] data;
        public Frame(string text)
        {
            opCode = 1;
            message = Encoding.UTF8.GetBytes(text);
            length = message.Length;
        }
        public byte[] GetBytes()
        {
            byte[] result = new byte[length + 2];
            result[0] = 129;
            result[1] = (byte)(length);
            message.CopyTo(result, 2);
            return result;
        }
        private void Read(byte[] bytes)
        {
            opCode = (byte)(bytes[0] & 15);
            length = -128 + bytes[1];
            byte[] mask = new byte[4] { bytes[2], bytes[3], bytes[4], bytes[5] };
            message = new byte[length];
            for (int i = 0; i < length; i++)
                message[i] = (byte)(bytes[i + 6] ^ mask[i % 4]);
        }
    }
    public class MessageArgs : EventArgs
    {
        public Frame frame { get; private set; }
        public MessageArgs(Frame frame) : base()
        {
            this.frame = frame;
        }
    }
    class WebSocket
    { 
        TcpListener listener;
        TcpClient client;
        public WebSocket(TcpListener listener)
        {
            this.listener = listener;
            OnMessageReceived += Test;
        }
        public void Send(string data)
        {
            Frame frame = new Frame(data);
            byte[] bytes = frame.GetBytes();
            client.GetStream().Write(bytes, 0, bytes.Length);
            
        }
        public EventHandler<MessageArgs> OnMessageReceived;
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
        
        public void Test(object sender, MessageArgs args)
        {
            //if(frame.opCode == 1)
            {
                var frame = args.frame;
                string text = Encoding.UTF8.GetString(args.frame.message);
                Send("testString");
            }
        }
        
        public async void ListenAsync()
        {
            
            NetworkStream stream = client.GetStream();
            while (true)
            {
                if(!stream.DataAvailable)
                {
                    await Task.Delay(10);
                    continue;
                }
                Frame frame = new Frame(stream);
                OnMessageReceived.Invoke(this, new MessageArgs(frame));
            }
        }

    }
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
                await Task.Run(() => ws.ListenAsync());
                wsockets.Add(ws);
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            new Thread( () =>
            {
                Thread.CurrentThread.IsBackground = true;
                Server serv = new Server();
                serv.Run();
            }).Start();
            

            //System.Diagnostics.Process.Start(@"cmd.exe ", @"/c StartPage.html");
            Console.ReadKey();
        }

    }
}
