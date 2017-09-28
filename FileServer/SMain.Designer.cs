namespace FileServer
{
    partial class SMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonStartup = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxListenPort = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.textBoxMessageLog = new System.Windows.Forms.TextBox();
            this.textBoxRecvFilePath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(217, 6);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 30);
            this.buttonStop.TabIndex = 10;
            this.buttonStop.Text = "停止";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonStartup
            // 
            this.buttonStartup.Location = new System.Drawing.Point(136, 6);
            this.buttonStartup.Name = "buttonStartup";
            this.buttonStartup.Size = new System.Drawing.Size(75, 30);
            this.buttonStartup.TabIndex = 9;
            this.buttonStartup.Text = "启动";
            this.buttonStartup.UseVisualStyleBackColor = true;
            this.buttonStartup.Click += new System.EventHandler(this.buttonStartup_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "端口";
            // 
            // textBoxListenPort
            // 
            this.textBoxListenPort.Location = new System.Drawing.Point(50, 12);
            this.textBoxListenPort.Name = "textBoxListenPort";
            this.textBoxListenPort.Size = new System.Drawing.Size(80, 21);
            this.textBoxListenPort.TabIndex = 7;
            this.textBoxListenPort.Text = "8087";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // textBoxMessageLog
            // 
            this.textBoxMessageLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxMessageLog.Location = new System.Drawing.Point(0, 61);
            this.textBoxMessageLog.Multiline = true;
            this.textBoxMessageLog.Name = "textBoxMessageLog";
            this.textBoxMessageLog.Size = new System.Drawing.Size(665, 352);
            this.textBoxMessageLog.TabIndex = 13;
            // 
            // textBoxRecvFilePath
            // 
            this.textBoxRecvFilePath.Location = new System.Drawing.Point(429, 12);
            this.textBoxRecvFilePath.Name = "textBoxRecvFilePath";
            this.textBoxRecvFilePath.Size = new System.Drawing.Size(224, 21);
            this.textBoxRecvFilePath.TabIndex = 14;
            this.textBoxRecvFilePath.Text = "C:\\RecvFiles";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(322, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 12);
            this.label2.TabIndex = 15;
            this.label2.Text = "接收文件保存路径";
            // 
            // SMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(665, 413);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxRecvFilePath);
            this.Controls.Add(this.textBoxMessageLog);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStartup);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxListenPort);
            this.Name = "SMain";
            this.Text = "Server";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonStartup;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxListenPort;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TextBox textBoxMessageLog;
        private System.Windows.Forms.TextBox textBoxRecvFilePath;
        private System.Windows.Forms.Label label2;
    }
}

