using System;

namespace MockPipelines.NamedPipeline.Interfaces
{
    public interface ICommunicationServer : ICommunication
    {
        string ServerId { get; }

        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;

        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
