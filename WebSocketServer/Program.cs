using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace WebSocketServer
{
    class Header
    {
        private Dictionary<string, string> data = new Dictionary<string, string>();
        public enum headerType { GET, POST }
        public headerType type { get; private set; }
        public string address { get; private set; }
        public Header(NetworkStream stream)
        {
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
            Console.WriteLine(str);
        }
        public object this[string index]
        {
            get { return data[index]; }
        }
    }
    class Server
    {
        public void Run()
        {
            IPAddress addr = IPAddress.Parse("127.0.0.2");
            TcpListener listener = new TcpListener(addr, 80);
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Header header = new Header( client.GetStream());
                client.Close();
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
