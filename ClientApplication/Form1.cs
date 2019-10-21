using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MockPipelines.NamedPipeline
{
    public partial class Form1 : Form
    {
        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes --
        private readonly string _pipeName = "TC_DEVICE_EMULATOR_PIPELINE";
        private ClientPipeline _clientpipe;
        private bool connected;
        #endregion

        /********************************************************************************************************/
        // CONSTRUCTION SECTION
        /********************************************************************************************************/
        #region -- construction --

        public Form1()
        {
            InitializeComponent();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            StartClientPipeline();
        }
        #endregion

        /********************************************************************************************************/
        // METHODS SECTION
        /********************************************************************************************************/
        #region -- METHODS --

        private void StartClientPipeline()
        {
            //WIP
            //string _pipeName = Clipboard.GetText();
            Debug.WriteLine($"client: pipeline started with GUID=[{_pipeName}]");
            _clientpipe = new ClientPipeline(string.IsNullOrEmpty(_pipeName) ? Guid.NewGuid().ToString() : _pipeName);
            _clientpipe.ClientPipeMessage += (sender, e) => this.label1.Text = e.Message;
 
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Debug.WriteLine("client: pipe started! +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");

                _clientpipe.Start();
                _clientpipe.SendMessage("CLIENT CONNECTED");

                this.Invoke(new MethodInvoker(() =>
                {
                    this.button2.Enabled = true;
                    this.button3.Enabled = true;
                }));

                connected = true;

                while (connected)
                {
                    Thread.Sleep(100);
                    //_clientpipe.ReadMessage();
                }

                //Application.Exit();

            }).Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_clientpipe != null)
            {
                _clientpipe.SendMessage("CLIENT EXITING...");
                _clientpipe.Stop();
                connected = false;
            }

            Application.Exit();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (_clientpipe != null)
            {
                _clientpipe.SendMessage("GET PAYMENT");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_clientpipe != null)
            {
                _clientpipe.SendMessage("GET STATUS");
            }
        }

        #endregion
    }
}
