using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;

namespace AHidLib.Samples.Csharp
{
    partial class Form1
    {
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_init(byte[] parm);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_register(ref int handle, int vid, int pid, int mi,
                                               int reportId, int reportSize, int reportType);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_deregister(ref int handle);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_write(int handle, byte[] buffer, int bytesToWrite, ref int bytesWritten);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_read(int handle, byte[] buffer, int bytesToRead, ref int bytesRead);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_find(int handle);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_identify(int handle, byte[] buffer, int bufferSize, ref int bytesProcessed);
        [DllImport("AHid.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AHid_info(byte[] buffer1, int bufferSize1, byte[] buffer2, int bufferSize2);

        const int TIMER_INTERVAL = 100;
        const int TMP_BUFFER_SIZE = 1000;
        const int TMP_BUFFER_SMALL_SIZE = 24;
        const int TMP_BUFFER_VERY_SMALL_SIZE = 3;
        
        const int MAX_REPORT_SIZE = 64;
        const int AHID_REPORT_TYPE_INPUT = 0;
        const int AHID_REPORT_TYPE_OUTPUT = 1;
        const int AHID_OK = 0;
        const int AHID_ERROR = -1;

        static int findInterval;

        bool isConnected = false;
        int vid;
        int pid;
        int interfaceID;
        int outputHandle;
        int outputReportID;
        int outputReportSize;
        int outputCounter;
        byte[] outputBuffer;
        int inputHandle;
        int inputReportID;
        int inputReportSize;
        int inputCounter;

        private void readValuesFromControls()
        {
            vid = Convert.ToInt32(textVID.Text, 16);
            pid = Convert.ToInt32(textPID.Text, 16);
            interfaceID = Convert.ToInt32(textInterfaceID.Text, 10);
            outputReportID = (byte)Convert.ToUInt32(textOutputID.Text, 10);
            outputReportSize = (byte)Convert.ToUInt32(textOutputSize.Text, 10);
            inputReportID = (byte)Convert.ToUInt32(textInputID.Text, 10);
            inputReportSize = (byte)Convert.ToUInt32(textInputSize.Text, 10);

            string[] splitBuffer = textOutputData.Text.Split(' ');
            int count = 0;

            outputBuffer = new byte[outputReportSize];

            foreach (String hex in splitBuffer)
            {
                if (count < outputReportSize)
                    outputBuffer[count++] = (byte)Convert.ToInt32(hex, 16);
            }
        }

        protected void init() 
        {
            byte[] parm = System.Text.Encoding.ASCII.GetBytes("\0");

            readValuesFromControls();

            if (AHID_ERROR == AHid_init(parm)) 
            {
                byte[] buffer1 = new byte[TMP_BUFFER_SIZE];
                byte[] buffer2 = new byte[TMP_BUFFER_SIZE];

                AHid_info(buffer1, TMP_BUFFER_SIZE, buffer2, TMP_BUFFER_SIZE);
            }
        }

        protected void connect() 
        {
            try 
            {
                if (false == isConnected)
                {
                    init();

                    AHid_register(ref outputHandle, vid, pid, interfaceID, outputReportID, outputReportSize, AHID_REPORT_TYPE_OUTPUT);
                    AHid_register(ref inputHandle, vid, pid, interfaceID, inputReportID, inputReportSize, AHID_REPORT_TYPE_INPUT);

                    find();

                    buttonConnect.Text = "Baglantiyi Kes";
                    isConnected = true;
                }
                else
                {
                    AHid_deregister(ref outputHandle);
                    AHid_deregister(ref inputHandle);

                    textStatus.Text = "DISCON";
                    buttonConnect.Text = "Baglan";
                    isConnected = false;
                }

            }
            catch (Exception) 
            {
                MessageBox.Show("Please put the AHid.dll in your \\Debug or \\Release folder!", "AHid.dll not found!", MessageBoxButtons.OK);

                Application.Exit();
            }
        }

        private void setGUID()
        {
            int bytesProcessed = 0;
            byte[] path = new byte[TMP_BUFFER_SIZE];

            if (AHID_OK == AHid_identify(outputHandle, path, TMP_BUFFER_SIZE, ref bytesProcessed))
            {
                textGUID.Text = Encoding.Default.GetString(path);
            }
        }

        protected bool find()
        {
            bool result = false;

            if (AHID_OK == AHid_find(outputHandle) &&
                AHID_OK == AHid_find(inputHandle))
            {
                setGUID();

                textStatus.Text = "ATTACHED";

                result = true;
            }
            else
            {
                textStatus.Text = "REMOVED";
            }

            return result;
        }

        protected void reset()
        {
            outputCounter = 0;
            inputCounter = 0;

            textOutputCounter.Text = outputCounter.ToString();
            textInputCounter.Text = inputCounter.ToString();
            inputListBox.Items.Clear();
        }

        private void updateReadCounterAndListBox(byte[] data)
        {
            string result = "";
            string value;

            for (int i = 0; i < inputReportSize; i++)
            {
                value = data[i].ToString("X02");
                result = result + " " + value;
            }

            if (0 == (inputCounter % TIMER_INTERVAL))
            {
                inputListBox.Items.Clear();
            }

            inputCounter++;

            inputListBox.Items.Insert(0, result);
            textInputCounter.Text = inputCounter.ToString();
        }

        private void read()
        {
            int bytesRead = 0;
            byte[] buffer = new byte[MAX_REPORT_SIZE];

            for (int i = 0; i < TIMER_INTERVAL; i++) 
            {
                AHid_read(inputHandle, buffer, inputReportSize, ref bytesRead);

                if (0 != bytesRead)
                {
                    updateReadCounterAndListBox(buffer);
                }
            }
        }

        private void write()
        {
            int bytesWritten = 0;
            uint loops;

            loops = Convert.ToUInt32(textOutputLoops.Text, 10);

            for (int i = 0; i < loops; i++)
            {
                if (AHID_OK == AHid_write(outputHandle, outputBuffer, outputReportSize, ref bytesWritten))
                {
                    outputCounter++;

                    textOutputCounter.Text = outputCounter.ToString();
                }
            }
        }

        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonSend = new System.Windows.Forms.Button();
            this.textOutputLoops = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textOutputCounter = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textOutputSize = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textOutputID = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textOutputData = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textInterfaceID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonReset = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.inputListBox = new System.Windows.Forms.ListBox();
            this.textInputCounter = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.textInputSize = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.textInputID = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textStatus = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.textGUID = new System.Windows.Forms.TextBox();
            this.textPID = new System.Windows.Forms.TextBox();
            this.textVID = new System.Windows.Forms.TextBox();
            this.label31 = new System.Windows.Forms.Label();
            this.label32 = new System.Windows.Forms.Label();
            this.label33 = new System.Windows.Forms.Label();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonSend);
            this.groupBox1.Controls.Add(this.textOutputLoops);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.textOutputCounter);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.textOutputSize);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.textOutputID);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.textOutputData);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Location = new System.Drawing.Point(12, 193);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(563, 81);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Output";
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new System.Drawing.Point(458, 19);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(90, 46);
            this.buttonSend.TabIndex = 28;
            this.buttonSend.Text = "Data Gonder";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // textOutputLoops
            // 
            this.textOutputLoops.Location = new System.Drawing.Point(362, 19);
            this.textOutputLoops.Name = "textOutputLoops";
            this.textOutputLoops.Size = new System.Drawing.Size(66, 20);
            this.textOutputLoops.TabIndex = 18;
            this.textOutputLoops.Text = "1";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(314, 22);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(41, 15);
            this.label10.TabIndex = 17;
            this.label10.Text = "Loops";
            // 
            // textOutputCounter
            // 
            this.textOutputCounter.Location = new System.Drawing.Point(232, 19);
            this.textOutputCounter.Name = "textOutputCounter";
            this.textOutputCounter.ReadOnly = true;
            this.textOutputCounter.Size = new System.Drawing.Size(66, 20);
            this.textOutputCounter.TabIndex = 16;
            this.textOutputCounter.Text = "0";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(182, 22);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(50, 15);
            this.label9.TabIndex = 15;
            this.label9.Text = "Counter";
            // 
            // textOutputSize
            // 
            this.textOutputSize.Location = new System.Drawing.Point(127, 18);
            this.textOutputSize.Name = "textOutputSize";
            this.textOutputSize.Size = new System.Drawing.Size(40, 20);
            this.textOutputSize.TabIndex = 12;
            this.textOutputSize.Text = "32";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(94, 21);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(31, 15);
            this.label7.TabIndex = 11;
            this.label7.Text = "Size";
            // 
            // textOutputID
            // 
            this.textOutputID.Location = new System.Drawing.Point(44, 18);
            this.textOutputID.Name = "textOutputID";
            this.textOutputID.Size = new System.Drawing.Size(40, 20);
            this.textOutputID.TabIndex = 10;
            this.textOutputID.Text = "0";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(20, 21);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(19, 15);
            this.label6.TabIndex = 9;
            this.label6.Text = "ID";
            // 
            // textOutputData
            // 
            this.textOutputData.Location = new System.Drawing.Point(44, 45);
            this.textOutputData.Name = "textOutputData";
            this.textOutputData.Size = new System.Drawing.Size(384, 20);
            this.textOutputData.TabIndex = 8;
            this.textOutputData.Text = "00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A " +
    "1B 1C 1D 1E 1F 20 21 22 23 24 25 26 27 28 29 2A 2B 2C 2D 2E 2F 30 31 32 33 34 35" +
    " 36 37 38 39 3A 3B 3C 3D 3E 3F ";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 48);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 15);
            this.label5.TabIndex = 7;
            this.label5.Text = "Data";
            // 
            // textInterfaceID
            // 
            this.textInterfaceID.Location = new System.Drawing.Point(232, 78);
            this.textInterfaceID.Name = "textInterfaceID";
            this.textInterfaceID.Size = new System.Drawing.Size(40, 20);
            this.textInterfaceID.TabIndex = 3;
            this.textInterfaceID.Text = "-1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(160, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "Interface ID";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonReset);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.inputListBox);
            this.groupBox2.Controls.Add(this.textInputCounter);
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.textInputSize);
            this.groupBox2.Controls.Add(this.label14);
            this.groupBox2.Controls.Add(this.textInputID);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Location = new System.Drawing.Point(12, 280);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(563, 203);
            this.groupBox2.TabIndex = 19;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Input";
            // 
            // buttonReset
            // 
            this.buttonReset.Location = new System.Drawing.Point(458, 18);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(90, 44);
            this.buttonReset.TabIndex = 28;
            this.buttonReset.Text = "Reset";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 15);
            this.label1.TabIndex = 27;
            this.label1.Text = "Data";
            // 
            // inputListBox
            // 
            this.inputListBox.FormattingEnabled = true;
            this.inputListBox.Location = new System.Drawing.Point(6, 68);
            this.inputListBox.Name = "inputListBox";
            this.inputListBox.ScrollAlwaysVisible = true;
            this.inputListBox.Size = new System.Drawing.Size(504, 108);
            this.inputListBox.TabIndex = 26;
            this.inputListBox.SelectedIndexChanged += new System.EventHandler(this.inputListBox_SelectedIndexChanged);
            // 
            // textInputCounter
            // 
            this.textInputCounter.Location = new System.Drawing.Point(232, 18);
            this.textInputCounter.Name = "textInputCounter";
            this.textInputCounter.ReadOnly = true;
            this.textInputCounter.Size = new System.Drawing.Size(66, 20);
            this.textInputCounter.TabIndex = 16;
            this.textInputCounter.Text = "0";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(182, 21);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(50, 15);
            this.label12.TabIndex = 15;
            this.label12.Text = "Counter";
            // 
            // textInputSize
            // 
            this.textInputSize.Location = new System.Drawing.Point(127, 18);
            this.textInputSize.Name = "textInputSize";
            this.textInputSize.Size = new System.Drawing.Size(40, 20);
            this.textInputSize.TabIndex = 12;
            this.textInputSize.Text = "64";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(94, 21);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(31, 15);
            this.label14.TabIndex = 11;
            this.label14.Text = "Size";
            // 
            // textInputID
            // 
            this.textInputID.Location = new System.Drawing.Point(44, 18);
            this.textInputID.Name = "textInputID";
            this.textInputID.Size = new System.Drawing.Size(40, 20);
            this.textInputID.TabIndex = 10;
            this.textInputID.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(20, 21);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(19, 15);
            this.label15.TabIndex = 9;
            this.label15.Text = "ID";
            // 
            // textStatus
            // 
            this.textStatus.Location = new System.Drawing.Point(329, 80);
            this.textStatus.Name = "textStatus";
            this.textStatus.ReadOnly = true;
            this.textStatus.Size = new System.Drawing.Size(66, 20);
            this.textStatus.TabIndex = 14;
            this.textStatus.Text = "DISCON";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(278, 83);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(45, 15);
            this.label26.TabIndex = 13;
            this.label26.Text = "Durum";
            // 
            // textGUID
            // 
            this.textGUID.Location = new System.Drawing.Point(44, 106);
            this.textGUID.Name = "textGUID";
            this.textGUID.ReadOnly = true;
            this.textGUID.Size = new System.Drawing.Size(384, 20);
            this.textGUID.TabIndex = 4;
            // 
            // textPID
            // 
            this.textPID.Location = new System.Drawing.Point(114, 78);
            this.textPID.Name = "textPID";
            this.textPID.ReadOnly = true;
            this.textPID.Size = new System.Drawing.Size(40, 20);
            this.textPID.TabIndex = 3;
            this.textPID.Text = "0003";
            // 
            // textVID
            // 
            this.textVID.Location = new System.Drawing.Point(41, 78);
            this.textVID.Name = "textVID";
            this.textVID.ReadOnly = true;
            this.textVID.Size = new System.Drawing.Size(40, 20);
            this.textVID.TabIndex = 1;
            this.textVID.Text = "FFFF";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(3, 106);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(37, 15);
            this.label31.TabIndex = 2;
            this.label31.Text = "GUID";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(81, 78);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(27, 15);
            this.label32.TabIndex = 1;
            this.label32.Text = "PID";
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(9, 78);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(26, 15);
            this.label33.TabIndex = 0;
            this.label33.Text = "VID";
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(458, 19);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(90, 46);
            this.buttonConnect.TabIndex = 28;
            this.buttonConnect.Text = "Baglan";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(6, 19);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(422, 56);
            this.listBox1.TabIndex = 20;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.6F);
            this.label3.Location = new System.Drawing.Point(209, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(179, 15);
            this.label3.TabIndex = 21;
            this.label3.Text = "USB HID STM32 UYGULAMASI";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label31);
            this.groupBox3.Controls.Add(this.textGUID);
            this.groupBox3.Controls.Add(this.textStatus);
            this.groupBox3.Controls.Add(this.buttonConnect);
            this.groupBox3.Controls.Add(this.label26);
            this.groupBox3.Controls.Add(this.listBox1);
            this.groupBox3.Controls.Add(this.textVID);
            this.groupBox3.Controls.Add(this.textPID);
            this.groupBox3.Controls.Add(this.textInterfaceID);
            this.groupBox3.Controls.Add(this.label32);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label33);
            this.groupBox3.Location = new System.Drawing.Point(12, 52);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(563, 135);
            this.groupBox3.TabIndex = 22;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "USB Aygıt Listesi";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(589, 465);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.3F);
            this.Name = "Form1";
            this.Text = " ";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textOutputSize;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textOutputID;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textOutputData;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textInterfaceID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textOutputLoops;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textOutputCounter;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textInputCounter;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textInputSize;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textInputID;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ListBox inputListBox;
        private Button buttonConnect;
        private TextBox textStatus;
        private Label label26;
        private TextBox textGUID;
        private TextBox textPID;
        private TextBox textVID;
        private Label label31;
        private Label label32;
        private Label label33;
        private Button buttonReset;
        private Label label1;
        private ListBox listBox1;
        private Label label3;
        private Button buttonSend;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private GroupBox groupBox3;
    }
}

