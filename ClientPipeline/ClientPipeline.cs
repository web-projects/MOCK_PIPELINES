using MockPipelines.NamedPipeline.Helpers;
using MockPipelines.NamedPipeline.Interfaces;
using System;
using System.Collections.Generic;
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

        private readonly NamedPipeClientStream _pipeClient;

        public event EventHandler<MessageEventArgs> ClientPipeMessage;

        #endregion

        /********************************************************************************************************/
        // INTERFACE SECTION
        /********************************************************************************************************/
        #region -- ICommunicationClient implementation --

        public void Start()
        {
            _pipeClient.Connect(TRY_CONNECT_TIMEOUT);
        }

        public void Stop()
        {
            try
            {
                _pipeClient.WaitForPipeDrain();
            }
            finally
            {
                _pipeClient.Close();
                _pipeClient.Dispose();
            }
        }

        public Task<TaskResult> SendMessage(string message)
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
        }

        #endregion

        /********************************************************************************************************/
        // PRIVATE METHODS SECTION
        /********************************************************************************************************/
        #region -- private methods --

        private TaskResult EndWriteCallBack(IAsyncResult asyncResult)
        {
            _pipeClient.EndWrite(asyncResult);
            _pipeClient.Flush();

            return new TaskResult { IsSuccess = true };
        }

        #endregion

        /********************************************************************************************************/
        // IMPLEMENTATION SECTION
        /********************************************************************************************************/
        #region -- implementation --

        public ClientPipeline(string serverId)
        {
            _pipeClient = new NamedPipeClientStream(".", serverId, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        /*public void StartPipe()
        {
            bool alive = true;
            string text = string.Empty;

            while (alive)
            {
                text = string.Empty;

                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "DeviceEmulatorPipe", PipeDirection.In, PipeOptions.Asynchronous))
                {
                    // Connect to the pipe or wait until the pipe is available.
                    mainForm.Invoke(new MethodInvoker(() =>
                    {
                        ClientPipeMessage?.Invoke(this, new MessageEventArgs("Attempting to connect to pipe..."));
                        Thread.Sleep(1000);
                    }));
                    pipeClient.Connect();

                    mainForm.Invoke(new MethodInvoker(() =>
                    {
                        ClientPipeMessage?.Invoke(this, new MessageEventArgs("Connected to pipe."));
                        Thread.Sleep(1000);
                        ClientPipeMessage?.Invoke(this, new MessageEventArgs($"There are currently {pipeClient.NumberOfServerInstances} pipe server instances open."));
                    }));

                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        string intext;
                        while ((intext = sr.ReadLine()) != null && alive)
                        {
                            text = intext;
                            Debug.WriteLine($"Received from server: {text}");
                            mainForm.Invoke(new MethodInvoker(() =>
                            {
                                ClientPipeMessage?.Invoke(this, new MessageEventArgs($"Received from server: {text}"));
                                Thread.Sleep(2000);
                            }));
                            if (text?.Equals("quit", StringComparison.CurrentCultureIgnoreCase) ?? false)
                                alive = false;
                        }
                    }

                    if (alive && text != null && text != string.Empty)
                    {
                        using (NamedPipeClientStream pipeClientWriter = new NamedPipeClientStream(".", "DeviceEmulatorPipe", PipeDirection.Out, PipeOptions.Asynchronous))
                        {
                            pipeClientWriter.Connect();

                            using (StreamWriter sw = new StreamWriter(pipeClientWriter))
                            {
                                sw.AutoFlush = true;
                                char[] charArray = text.ToCharArray();
                                Array.Reverse(charArray);
                                sw.WriteLine(new string(charArray));
                            }
                        }
                    }
                }
            }
        }*/

        #endregion
    }
}
