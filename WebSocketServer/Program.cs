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
            get 
            {
                try
                {
                    return data[index];
                }
                catch
                {
                    return "";
                }
            }
            set { data[index] = value; }
        }
    }
    
    public class Frame
    {
        public byte[] message;
        public byte opCode;
        public long length;
        public bool isFin = false;
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
            List<byte> byteList = new List<byte>();
            byteList.Add((byte)stream.ReadByte());
            byteList.Add((byte)stream.ReadByte());
            opCode = (byte)(byteList[0] & 15);
            isFin = (byteList[0] & 128) != 0;
            length = -128 + byteList[1];
            int offset = 0;
            if (length == 126)
            {
                offset = 2;
                byteList.Add((byte)stream.ReadByte());
                byteList.Add((byte)stream.ReadByte());
                length = byteList[2] * 256 + byteList[3];
            }
            else if (length == 127)
            {
                offset = 8;
                for(int i = 0; i < 8; i++)
                {
                    byteList.Add((byte)stream.ReadByte());
                    length += byteList[2 + i] * (1 << (7 - i));
                }
            }

            for(int i = 0; i < 4 + length; i++)
                byteList.Add((byte)stream.ReadByte());
            byte[] mask = new byte[4] { byteList[2 + offset], byteList[3 + offset], byteList[4 + offset], byteList[5 + offset] };
            message = new byte[length];
            for (int i = 0; i < length; i++)
                message[i] = (byte)(byteList[i + 6 + offset] ^ mask[i % 4]);
        }
        public Frame(string text, byte opCode)
        {
            this.opCode = opCode;
            
            message = Encoding.UTF8.GetBytes(text);
            if (opCode == 8)
                message = Append(new byte[2] { 3, 232 }, message, message.Length);
            length = message.Length;
        }
        public byte[] GetBytes()
        {
            byte[] result = new byte[length + 2];
            result[0] = (byte)(128 + opCode);
            result[1] = (byte)(length);

            message.CopyTo(result, 2);
            return result;
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
        
        public void Test(object sender, MessageArgs args)
        {
            //if(frame.opCode == 1)
            {
                var frame = args.frame;
                string text = Encoding.UTF8.GetString(args.frame.message);
                Send("testString");
            }
        }
        private bool ToClose = false;
        public void Close(string reason)
        {
            ToClose = true;
            closeReason = reason;
        }
        int time = 0;
        string closeReason;
        public async void ListenAsync()
        {
            
            NetworkStream stream = client.GetStream();
            while (true)
            {
                if (time > 1000)
                    Close("timeLimit (10s)");
                if (ToClose)
                {
                    SendClose(closeReason);
                    if(OnClose != null)
                    OnClose.Invoke(this, new CloseArgs(1000, closeReason));
                    await Task.Delay(40);
                    client.Close();
                    break;
                }
                if (!stream.DataAvailable)
                {
                    time += 1;
                    await Task.Delay(10);
                    continue;
                }
                Frame frame = new Frame(stream);
                //string s = Encoding.UTF8.GetString(frame.message);
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
