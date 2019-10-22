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
using Newtonsoft.Json;

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
            client = new InternalPipeClient(serverId);
            client.MessageReceivedEvent += MessageReceivedHandler;
            client.Start();
        }

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceived(eventArgs);
        }

        private void OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            Console.WriteLine($"client: server message received=[{eventArgs.Message}]");
            _synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), eventArgs);
            ClientPipeMessage?.Invoke(this, new MessageEventArgs(RetrieveMessage(eventArgs.Message)));
        }

        private string RetrieveMessage(string message)
        {
            string result = string.Empty;
            string value = System.Text.RegularExpressions.Regex.Replace(message.Trim('\"'), "[\\\\]+", string.Empty);
            DalActionRequestRoot request = JsonConvert.DeserializeObject<DalActionRequestRoot>(value);
            if (request != null)
            {
                result = request.DALActionRequest.DeviceUIRequest.DisplayText[0];
            }

            return result;
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
            if (client != null)
            { 
                client.Stop();
            }
        }

        public void SendMessage(string message)
        {
            client?.SendMessage(message);
        }

        #endregion
    }
}
