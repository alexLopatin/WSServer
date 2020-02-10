using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace WebSocketServer
{
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
                for (int i = 0; i < 8; i++)
                {
                    byteList.Add((byte)stream.ReadByte());
                    length += byteList[2 + i] * (1 << (7 - i));
                }
            }

            for (int i = 0; i < 4 + length; i++)
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
            int offset = 0;
            byte[] result = new byte[length + 2];
            if (length < 126)
                result[1] = (byte)(length);
            else if (length <= ushort.MaxValue)
            {
                offset = 2;
                result = new byte[length + 2 + offset];
                result[1] = 126;
                result[2] = (byte)(length / 256);
                result[3] = (byte)(length % 256);
            }
            else
            {
                offset = 8;
                result = new byte[length + 2 + offset];
                result[1] = 127;
                var bytes = BitConverter.GetBytes(length);
                for (int i = 0; i < 8; i++)
                    result[9 - i] = bytes[i];
            }
            result[0] = (byte)(128 + opCode);
            message.CopyTo(result, 2 + offset);
            return result;
        }
    }
}
