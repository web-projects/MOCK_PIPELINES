namespace MockPipelines.NamedPipeline.Interfaces
{
    public interface ICommunication
    {
        void Start();
        void Stop();
        void SendMessage(string message);
    }
}
