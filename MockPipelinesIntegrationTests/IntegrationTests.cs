using System.Threading;
using NUnit.Framework;

namespace MockPipelines.NamedPipeline.IntegrationTests
{
    public class IntegrationTests
    {
        private ServerPipeline _server;
        private ClientPipeline _client;

        [TearDown]
        public void TearDown()
        {
            if (_client != null)
            {
                _client.Stop();
                _client = null;
            }

            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }
        }

        [Test]
        public void Instantiate_PipeServer_ShouldHaveValidServerId()
        {
            // Act
            _server = new ServerPipeline();

            // Verify
            Assert.IsNotNull(_server.ServerId);
            Assert.IsNotEmpty(_server.ServerId);
        }

        [Test]
        public void Client_Connect_ServerShouldFireClientConnectedEvent()
        {
            // Prepare
            var isConnected = false;
            _server = new ServerPipeline();

            _server.ClientConnectedEvent += (sender, args) =>
            {
                isConnected = true;
            };

            _server.Start();
            Assert.IsFalse(isConnected);

            // Act
            _client = new ClientPipeline(_server.ServerId);
            _client.Start();
            Thread.Sleep(100);

            // Verify
            Assert.IsTrue(isConnected);
        }

        [Test]
        public void Client_Disconnect_ServerShouldFireClientDisconnectedEvent()
        {
            // Prepare
            var isDisconnected = false;
            _server = new ServerPipeline();
            
            _server.ClientDisconnectedEvent += (sender, args) =>
            {
                isDisconnected = true;
            };

            _server.Start();
            Assert.IsFalse(isDisconnected);

            _client = new ClientPipeline(_server.ServerId);
            _client.Start();

            // Act
            _client.Stop();
            _client = null;
            Thread.Sleep(100);

            // Verify
            Assert.IsTrue(isDisconnected);
        }

        [Test]
        public void Client_SendMessage_ServerShouldFireMessageReceivedEvent()
        {
            // Prepare
            _server = new ServerPipeline();
            _client = new ClientPipeline(_server.ServerId);

            string message = null;
            var autoEvent = new AutoResetEvent(false);

            _server.MessageReceivedEvent += (sender, args) =>
            {
                message = args.Message;
                autoEvent.Set();
            };

            _server.Start();
            _client.Start();

            // Act
            _client.SendMessage("Client's message");

            // Verify
            autoEvent.WaitOne();

            Assert.AreEqual("Client's message", message);
        }
    }
}
