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
                for (int index = 0; index < 100000; index++)
                {
                    Thread.Sleep(1000);

                    if (serverpipe.ClientConnected())
                    {
                        Thread.Sleep(1000);
                        serverpipe.SendMessage("message from server");
                    }
                }

                serverpipe.Stop();
            }
        }
    }
}
