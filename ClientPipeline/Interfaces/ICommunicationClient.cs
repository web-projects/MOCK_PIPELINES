using System.Threading.Tasks;
using MockPipelines.NamedPipeline.Helpers;

namespace MockPipelines.NamedPipeline.Interfaces
{
    public interface ICommunicationClient : ICommunication
    {
        Task<TaskResult> SendMessage(string message);
    }
}
