using System;
using System.Threading.Tasks;
using MockPipelines.NamedPipeline.Helpers;

namespace MockPipelines.NamedPipeline.Interfaces
{
    public interface ICommunicationClient : ICommunication
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        //Task<TaskResult> SendMessage(string message);
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
