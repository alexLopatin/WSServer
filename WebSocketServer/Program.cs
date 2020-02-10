using System;
using System.Threading;

namespace WebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            new Thread( () =>
            {
                Thread.CurrentThread.IsBackground = true;
                Server serv = new ChatServer();
                serv.Run();
            }).Start();
            
            //System.Diagnostics.Process.Start(@"cmd.exe ", @"/c StartPage.html");
            Console.ReadKey();
        }
    }
}
