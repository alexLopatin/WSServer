using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace WebSocketServer
{
    public class Header
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
                res.Append(kvp.Key + ": " + kvp.Value + "\r\n");
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
}
