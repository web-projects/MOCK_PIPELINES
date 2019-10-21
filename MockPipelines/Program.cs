using System;
using System.Threading;

namespace MockPipelines.NamedPipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server namedpipeline...");
            ServerPipeline serverpipe = new ServerPipeline();
            if (serverpipe != null)
            {
                serverpipe.Start();
                //serverpipe.SendMessage("message from server");
                Thread.Sleep(10000000);
                serverpipe.Stop();
            }
        }
    }
}
