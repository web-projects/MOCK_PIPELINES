using MockPipelines.NamedPipeline.Helpers;
using MockPipelines.NamedPipeline.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MockPipelines.NamedPipeline
{
    public class ServerPipeline
    {
        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes --
        private readonly string _pipeName = "TC_DEVICE_EMULATOR_PIPELINE";
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IDictionary<string, ICommunicationServer> _servers; // ConcurrentDictionary is thread safe
        private const int MaxNumberOfServerInstances = 10;
        private bool clientConnected;
        #endregion

        /********************************************************************************************************/
        // EVENTS SECTION
        /********************************************************************************************************/
        #region -- events --

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        #endregion

        /********************************************************************************************************/
        // CONSTRUCTION SECTION
        /********************************************************************************************************/
        #region -- constructor --

        public ServerPipeline()
        {
            //_pipeName = Guid.NewGuid().ToString();
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;
            _servers = new ConcurrentDictionary<string, ICommunicationServer>();
            Copy_GUID_Clipboard();
        }

        #endregion

        /********************************************************************************************************/
        // PRIVATE METHODS SECTION
        /********************************************************************************************************/
        #region -- private methods --

        public void ClipboardCopyToRunInThread()
        {
            //Clipboard.SetText(_pipeName);
        }

        protected void Copy_GUID_Clipboard()
        {
            Console.WriteLine($"server: pipeline started with GUID=[{_pipeName}]");
            Thread clipboardThread = new Thread(ClipboardCopyToRunInThread);
            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.IsBackground = false;
            clipboardThread.Start();
        }

        private void StartNamedPipeServer()
        {
            var server = new InternalPipeServer(_pipeName, MaxNumberOfServerInstances);
            _servers[server.Id] = server;

            server.ClientConnectedEvent += ClientConnectedHandler;
            server.ClientDisconnectedEvent += ClientDisconnectedHandler;
            server.MessageReceivedEvent += MessageReceivedHandler;

            server.Start();
        }

        private void StopNamedPipeServer(string id)
        {
            UnregisterFromServerEvents(_servers[id]);
            _servers[id].Stop();
            _servers.Remove(id);
        }

        private void UnregisterFromServerEvents(ICommunicationServer server)
        {
            server.ClientConnectedEvent -= ClientConnectedHandler;
            server.ClientDisconnectedEvent -= ClientDisconnectedHandler;
            server.MessageReceivedEvent -= MessageReceivedHandler;
        }

        private string RetrieveMessage(string message)
        {
            string result = string.Empty;
            string value = System.Text.RegularExpressions.Regex.Replace(message.Trim('\"'), "[\\\\]+", string.Empty);
            DalActionResponseRoot request = JsonConvert.DeserializeObject<DalActionResponseRoot>(value);
            if (request != null)
            {
                result = request.DALActionResponse.DeviceUIResponse.DisplayText[0];
            }

            return result;
        }

        private void OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            Console.WriteLine($"server: client message received=[{RetrieveMessage(eventArgs.Message)}]");
            _synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), eventArgs);
        }

        private void OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            clientConnected = true;
            Console.WriteLine($"server: client connected with ID=[{eventArgs.ClientId}]");
            _synchronizationContext.Post(e => ClientConnectedEvent.SafeInvoke(this, (ClientConnectedEventArgs)e), eventArgs);
        }

        private void OnClientDisconnected(ClientDisconnectedEventArgs eventArgs)
        {
            Console.WriteLine($"server: client disconnected with ID=[{eventArgs.ClientId}]");
            _synchronizationContext.Post(e => ClientDisconnectedEvent.SafeInvoke(this, (ClientDisconnectedEventArgs)e), eventArgs);
        }

        private void ClientConnectedHandler(object sender, ClientConnectedEventArgs eventArgs)
        {
            OnClientConnected(eventArgs);

            StartNamedPipeServer(); // Create a additional server as a preparation for new connection
        }

        private void ClientDisconnectedHandler(object sender, ClientDisconnectedEventArgs eventArgs)
        {
            clientConnected = false;
            OnClientDisconnected(eventArgs);

            StopNamedPipeServer(eventArgs.ClientId);
        }

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceived(eventArgs);
        }

        #endregion
        
        /********************************************************************************************************/
        // IMPLEMENTATION SECTION
        /********************************************************************************************************/
        #region -- implementation --

        public string ServerId
        {
            get { return _pipeName; }
        }

        public void Start()
        {
            StartNamedPipeServer();
        }

        public void Stop()
        {
            foreach (var server in _servers.Values)
            {
                try
                {
                    UnregisterFromServerEvents(server);
                    server.Stop();
                }
                catch (Exception)
                {
                    Logger.Error("server: failed to stop server");
                }
            }

            _servers.Clear();
        }

        public bool ClientConnected()
        {
            return clientConnected;
        }

        public void SendMessage(string message)
        {
            foreach (var server in _servers.Values)
            {
                server.SendMessage(message);
            }
        }

        #endregion
    }
}
