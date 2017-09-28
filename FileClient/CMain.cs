using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace FileClient
{
    public partial class CMain : Form
    {
        public static readonly ConcurrentQueue<string> MessageLog = new ConcurrentQueue<string>();

        public CMain()
        {
            InitializeComponent();
        }

        #region 基础
        private void timer1_Tick(object sender, EventArgs e)
        {
            while (!MessageLog.IsEmpty)
            {
                string message;

                if (!MessageLog.TryDequeue(out message)) continue;
                if (textBoxMessages.TextLength > 1024 * 10)
                {
                    textBoxMessages.Clear();
                    textBoxMessages.Text = "";
                }
                textBoxMessages.AppendText(message);
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

        public void StartThread()
        {
            if (!File.Exists(textBoxFilePath.Text))
            {
                Enqueue("文件 " + textBoxFilePath.Text + " 不存在");
                return;
            }
            FileInfo fi = new FileInfo(textBoxFilePath.Text);
            ListViewItem lvi = new ListViewItem();
            lvi.Text = textBoxFilePath.Text;
            lvi.SubItems.Add((fi.Length/1024).ToString());
            lvi.SubItems.Add("0");
            listViewFiles.Items.Add(lvi);
            FileConnection.ClientConnection c = new FileConnection.ClientConnection();
            c.Output = (string msg) =>
            {
                Enqueue(msg);
                return true;
            };
            c.UploadNotify = (string msg) =>
            {
                lvi.SubItems[2].Text = msg;
                return true;
            };
            c.Start(textBoxFilePath.Text, "llllllllll", "116.62.13.180", 8087);
            textBoxFilePath.Text = "";
        }
        private void button1_Click(object sender, System.EventArgs e)
        {
            new Thread(StartThread).Start();
        }
        
    }
}
