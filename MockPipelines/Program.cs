using System;
using System.Threading;

namespace MockPipelines.NamedPipeline
{
    class Program
    {
        static readonly string[] MESSAGES =
        {
            "Insert Card",
            "Remove Card",
            "Enter Zip Code",
            "Enter PIN"
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Starting server namedpipeline...");
            ServerPipeline serverpipe = new ServerPipeline();
            if (serverpipe != null)
            {
                serverpipe.Start();
                int msgindex = 0;
                for (int index = 0; index < 100000; index++)
                {
                    Thread.Sleep(5000);

                    if (serverpipe.ClientConnected())
                    {
                        Thread.Sleep(1000);

                        serverpipe.SendMessage($"{MESSAGES[msgindex++]}");
                        if(msgindex > MESSAGES.Length - 1)
                            msgindex = 0;
                    }
                }

                serverpipe.Stop();
            }
        }
    }
}
