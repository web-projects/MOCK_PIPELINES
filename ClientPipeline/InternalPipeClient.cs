using System;
using System.IO.Pipes;
using System.Text;
using MockPipelines.NamedPipeline.Interfaces;
using MockPipelines.NamedPipeline.Helpers;

namespace MockPipelines.NamedPipeline
{
    internal class InternalPipeClient : ICommunicationClient
    {
        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes --

        const int TRY_CONNECT_TIMEOUT = 5 * 60 * 1000; // 5 minutes

        private readonly NamedPipeClientStream _pipeClient;
        private bool _isStopping;
        private readonly object _lockingObject = new object();
        private const int BufferSize = 2048;

        // Client Messages - Insert Card, Remove Card
        private string displayText = "{{ \"DALActionResponse\": {{ \"DeviceUIResponse\": {{ \"UIAction\": \"Display\", \"DisplayText\": [\"{0}\"] }} }} }}";

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

        public InternalPipeClient(string serverId)
        {
            _pipeClient = new NamedPipeClientStream(".", serverId, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        #endregion

        /********************************************************************************************************/
        // EVENTS SECTION
        /********************************************************************************************************/
        #region events

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        #endregion

        /********************************************************************************************************/
        // PRIVATE METHODS SECTION
        /********************************************************************************************************/
        #region private methods

        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = _pipeClient.EndRead(result);
            if (readBytes > 0)
            {
                var info = (Info)result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!_pipeClient.IsMessageComplete) // Message is not complete, continue reading
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
                            Stop();
                        }
                    }
                }
            }
        }

        private void BeginRead(Info info)
        {
            try
            {
                _pipeClient?.BeginRead(info.Buffer, 0, BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private void EndWriteCallBack(IAsyncResult result)
        {
            if (_pipeClient.IsConnected)
            { 
                _pipeClient.EndWrite(result);
                _pipeClient.WaitForPipeDrain();
                _pipeClient.Flush();
            }
        }

        private void BeginWrite(Info info)
        {
            try
            {
                _pipeClient?.BeginWrite(info.Buffer, 0, BufferSize, EndWriteCallBack, info);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
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

        #endregion

        /********************************************************************************************************/
        // PUBLIC METHODS SECTION
        /********************************************************************************************************/
        #region public methods

        public void Start()
        {
            try
            {
                Console.WriteLine("client started. Waiting for server connection...");
                _pipeClient.Connect(TRY_CONNECT_TIMEOUT);
                _pipeClient.ReadMode = PipeTransmissionMode.Message;
                BeginRead(new Info());
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
                if (_pipeClient.IsConnected)
                {
                    _pipeClient.WaitForPipeDrain();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
            finally
            {
                _pipeClient.Close();
                _pipeClient.Dispose();
            }
        }

        public void SendMessage(string message)
        {
            if (_pipeClient?.IsConnected ?? false)
            {
                var info = new Info();

                // Get the write bytes and append them
                byte [] writeBytes = Encoding.ASCII.GetBytes(string.Format(displayText, message));
                Array.Copy(writeBytes, writeBytes.GetLowerBound(0), info.Buffer, info.Buffer.GetLowerBound(0), writeBytes.Length);
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, info.Buffer.Length));
                BeginWrite(info);
            }
        }

        #endregion
    }
}
