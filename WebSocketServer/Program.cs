using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

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
            //Console.WriteLine("s");
            StringBuilder str = new StringBuilder();
            byte[] dataB = new byte[256];
            do
            {
                int bytes = stream.Read(dataB, 0, dataB.Length);
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
            //Console.WriteLine(s);
        }
        public override string ToString()
        {
            string res = "";
            if (type == headerType.SWITCH)
                res += "HTTP/1.1 101 Switching Protocols\r\n";
            foreach (var kvp in data)
                res += kvp.Key + ": " + kvp.Value + "\r\n";
            return res + "\r\n\r\n";
        }
        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
        public string this[string index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }
    }
    class WebSocket
    { 
        TcpListener listener;
        TcpClient client;
        public WebSocket()
        {
            IPAddress addr = IPAddress.Parse("127.0.0.2");
            listener = new TcpListener(addr, 80);
            listener.Start();
        }
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
            if (header["Sec-WebSocket-Version"] != "13" || header["Connection"] != "Upgrade" || header["Upgrade"] != "websocket")
                client.Close();
            Header answer = new Header();
            answer.type = Header.headerType.SWITCH;
            answer["Upgrade"] = "websocket";
            answer["Connection"] = "Upgrade";
            answer["Sec-WebSocket-Accept"] = AcceptString(header["Sec-WebSocket-Key"]);
            var ansBytes = answer.ToBytes();
            client.GetStream().Write(ansBytes, 0, ansBytes.Length);
        }

    }
    class Server
    {
        WebSocket ws;
        public void Run()
        {
            ws = new WebSocket();
            
            while (true)
            {
                ws.AcceptClient();
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
