using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileServer
{
    public partial class SMain : Form
    {
        private TcpListener _listener;
        public static readonly ConcurrentQueue<string> MessageLog = new ConcurrentQueue<string>();
        public bool ServerStarted = false;
        #region 基础
        public SMain()
        {
            InitializeComponent();
            buttonStop.Enabled = false;
            buttonStartup.Enabled = true;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            while (!MessageLog.IsEmpty)
            {
                string message;

                if (!MessageLog.TryDequeue(out message)) continue;
                if (textBoxMessageLog.TextLength > 1024 * 10)
                {
                    textBoxMessageLog.Clear();
                    textBoxMessageLog.Text = "";
                }
                textBoxMessageLog.AppendText(message);
            }
        }
        public static void Enqueue(Exception ex)
        {
            if (MessageLog.Count < 100)
                MessageLog.Enqueue(String.Format("[{0}]: {1} - {2}" + Environment.NewLine, DateTime.Now, ex.TargetSite, ex));
        }
        public static void Enqueue(string msg)
        {
            if (MessageLog.Count < 100)
                MessageLog.Enqueue(String.Format("[{0}]: {1}" + Environment.NewLine, DateTime.Now, msg));
        }
        #endregion

        private void Connection(IAsyncResult result)
        {
            if (!_listener.Server.IsBound) return;
            try
            {
                TcpClient tempTcpClient = _listener.EndAcceptTcpClient(result);
                var c = new FileConnection.ServerConnection(tempTcpClient, textBoxRecvFilePath.Text);
                c.Output = (string msg) =>
                {
                    Enqueue(msg);
                    return true;
                };
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            finally
            {
                if (_listener.Server.IsBound)
                    _listener.BeginAcceptTcpClient(Connection, null);
            }
        }
        private void buttonStartup_Click(object sender, EventArgs e)
        {
            string RecvFilePath = textBoxRecvFilePath.Text;
            if (string.IsNullOrEmpty(RecvFilePath))
            {
                Enqueue("请选择要保存的路径");
                return;
            }
            if (!Directory.Exists(RecvFilePath))
            {
                Directory.CreateDirectory(RecvFilePath);
            }
            _listener = new TcpListener(IPAddress.Any, Convert.ToInt32(textBoxListenPort.Text));
            _listener.Start();
            _listener.BeginAcceptTcpClient(Connection, null);
            ServerStarted = true;
            buttonStop.Enabled = true;
            buttonStartup.Enabled = false;
            Enqueue("启动文件传输服务成功...");
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            ServerStarted = false;
            buttonStop.Enabled = false;
            buttonStartup.Enabled = true;
            _listener.Stop();
            Enqueue("停止文件传输服务成功...");
        }
    }



}
