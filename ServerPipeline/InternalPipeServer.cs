using System;
using System.IO.Pipes;
using System.Text;
using MockPipelines.NamedPipeline.Interfaces;
using MockPipelines.NamedPipeline.Helpers;

namespace MockPipelines.NamedPipeline
{
    internal class InternalPipeServer : ICommunicationServer
    {
        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes --

        private readonly NamedPipeServerStream _pipeServer;
        private bool _isStopping;
        private readonly object _lockingObject = new object();
        private const int BufferSize = 2048;
        public readonly string Id;

        #endregion

        /********************************************************************************************************/
        // PRIVATE FIELDS SECTION
        /********************************************************************************************************/
        #region -- private fields --

        private class Info
        {
            public readonly byte[] Buffer;
            public readonly StringBuilder StringBuilder;

            public Info()
            {
                Buffer = new byte[BufferSize];
                StringBuilder = new StringBuilder();
            }
        }

        #endregion

        /********************************************************************************************************/
        // CONSTRUCTOR SECTION
        /********************************************************************************************************/
        #region -- constructor --

        public InternalPipeServer(string pipeName, int maxNumberOfServerInstances)
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Id = Guid.NewGuid().ToString();
        }

        #endregion

        /********************************************************************************************************/
        // EVENTS SECTION
        /********************************************************************************************************/
        #region events

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        #endregion

        /********************************************************************************************************/
        // PUBLIC METHODS SECTION
        /********************************************************************************************************/
        #region public methods

        public string ServerId
        {
            get { return Id; }
        }

        public void Start()
        {
            try
            {
                Console.WriteLine("server started. Waiting for client connection...");
                _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public void Stop()
        {
            _isStopping = true;

            try
            {
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
            finally
            {
                _pipeServer.Close();
                _pipeServer.Dispose();
            }
        }

        public void SendMessage(string message)
        {
            if (_pipeServer.IsConnected)
            {
                var info = new Info();

                // Get the write bytes and append them
                byte [] writeBytes = Encoding.ASCII.GetBytes(message);
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, writeBytes.Length));
                BeginWrite(info);
            }
        }

        #endregion

        /********************************************************************************************************/
        // PRIVATE METHODS SECTION
        /********************************************************************************************************/
        #region private methods

        private void BeginRead(Info info)
        {
            try
            {
                _pipeServer.BeginRead(info.Buffer, 0, BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private void BeginWrite(Info info)
        {
            try
            {
                _pipeServer.BeginWrite(info.Buffer, 0, BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            if (!_isStopping)
            {
                lock (_lockingObject)
                {
                    if (!_isStopping)
                    {
                        // Call EndWaitForConnection to complete the connection operation
                        _pipeServer.EndWaitForConnection(result);

                        OnConnected();

                        BeginRead(new Info());
                    }
                }
            }
        }

        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = _pipeServer.EndRead(result);
            if (readBytes > 0)
            {
                var info = (Info) result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!_pipeServer.IsMessageComplete) // Message is not complete, continue reading
                {
                    BeginRead(info);
                }
                else // Message is completed
                {
                    // Finalize the received string and fire MessageReceivedEvent
                    var message = info.StringBuilder.ToString().TrimEnd('\0');

                    OnMessageReceived(message);

                    // Begin a new reading operation
                    BeginRead(new Info());
                }
            }
            else // When no bytes were read, it can mean that the client have been disconnected
            {
                if (!_isStopping)
                {
                    lock (_lockingObject)
                    {
                        if (!_isStopping)
                        {
                            OnDisconnected();
                            Stop();
                        }
                    }
                }
            }
        }

        private void OnMessageReceived(string message)
        {
            if (MessageReceivedEvent != null)
            {
                MessageReceivedEvent(this,
                    new MessageReceivedEventArgs
                    {
                        Message = message
                    });
            }
        }

        private void OnConnected()
        {
            if (ClientConnectedEvent != null)
            {
                ClientConnectedEvent(this, new ClientConnectedEventArgs {ClientId = Id});
            }
        }

        private void OnDisconnected()
        {
            if (ClientDisconnectedEvent != null)
            {
                ClientDisconnectedEvent(this, new ClientDisconnectedEventArgs {ClientId = Id});
            }
        }

        #endregion
    }
}
