using MockPipelines.NamedPipeline.Helpers;
using MockPipelines.NamedPipeline.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MockPipelines.NamedPipeline
{
    public class ClientPipeline : ICommunication
    {
        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes --

        const int TRY_CONNECT_TIMEOUT = 5 * 60 * 1000; // 5 minutes

        //private NamedPipeClientStream _pipeClient;
        private InternalPipeClient client;
        private string serverId;
        private readonly SynchronizationContext _synchronizationContext;
        public event EventHandler<MessageEventArgs> ClientPipeMessage;

        #endregion

        /********************************************************************************************************/
        // EVENTS SECTION
        /********************************************************************************************************/
        #region -- events --

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        #endregion
 
        /********************************************************************************************************/
        // PRIVATE METHODS SECTION
        /********************************************************************************************************/
        #region -- private methods --

        private void StartNamedPipeClient()
        {
            //_pipeClient = new NamedPipeClientStream(".", serverId, PipeDirection.InOut, PipeOptions.Asynchronous);
            client = new InternalPipeClient(serverId);
            client.MessageReceivedEvent += MessageReceivedHandler;
            client.Start();
            //_pipeClient.Connect(TRY_CONNECT_TIMEOUT);
        }

        /*private TaskResult EndWriteCallBack(IAsyncResult asyncResult)
        {
            //_pipeClient.EndWrite(asyncResult);
            //_pipeClient.Flush();

            return new TaskResult { IsSuccess = true };
        }*/

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceived(eventArgs);
        }

        private void OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            Console.WriteLine($"client: message received=[{eventArgs.Message}]");
            _synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), eventArgs);
        }

        #endregion

        /********************************************************************************************************/
        // CONSTRUCTION SECTION
        /********************************************************************************************************/
        #region -- construction --

        public ClientPipeline(string serverId)
        {
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;
            this.serverId = serverId;
        }

        #endregion

        /********************************************************************************************************/
        // IMPLEMENTATION SECTION
        /********************************************************************************************************/
        #region -- implementation --

        public void Start()
        {
            StartNamedPipeClient();
        }

        public void Stop()
        {
            try
            {
                //_pipeClient.WaitForPipeDrain();
            }
            finally
            {
                //_pipeClient.Close();
                //_pipeClient.Dispose();
            }
        }

        /*public Task<TaskResult> SendMessage(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskResult>();

            if (_pipeClient.IsConnected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                _pipeClient.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(EndWriteCallBack(asyncResult));
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }

                }, null);
            }
            else
            {
                Logger.Error("Cannot send message, pipe is not connected");
                throw new IOException("pipe is not connected");
            }

            return taskCompletionSource.Task;
        }*/

        public void SendMessage(string message)
        {
            client?.SendMessage(message);
        }

        #endregion
    }
}
