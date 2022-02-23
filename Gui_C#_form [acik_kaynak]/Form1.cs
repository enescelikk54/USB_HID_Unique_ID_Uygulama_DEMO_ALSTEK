using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;

namespace AHidLib.Samples.Csharp
{
    public partial class Form1 : Form
    {
        void timerCallback(object sender, EventArgs e)
        {
            read();

            if (0 == (findInterval++ % 10))
            {
                find();
            }
        }

        public Form1()
        {
            InitializeComponent();

            Timer timer = new Timer();

            timer.Interval = TIMER_INTERVAL;
            timer.Tick += new EventHandler(timerCallback);
            timer.Start();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            connect();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            reset();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            write();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ManagementObjectCollection collection;
            using (var finddevice = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity"))
                collection = finddevice.Get();
            foreach (var device in collection)
            {
                //listBox1.Items.Add(device.GetPropertyValue("DeviceID"));
                //listBox1.Items.Add(device.GetPropertyValue("DeviceID"));
                String deger = "PNPDeviceID";
                String ids = device.GetPropertyValue(deger).ToString();
                String vid, pid;
                if (ids != null)
                {


                    //  dizi.Append(device.GetPropertyValue(deger).ToString());

                    //)
                    if (ids.IndexOf('H') == 0 && ids.IndexOf('I') == 1 && ids.IndexOf('D') == 2)
                    {
                        string[] parcalar = ids.Split('_');
                        if (parcalar.Length > 2)
                        {
                            vid = parcalar[1].ToString().Split('&')[0];
                            pid = parcalar[2].ToString();
                            textVID.Text = vid;
                            textPID.Text = pid.Substring(0, 4);
                    
                            listBox1.Items.Add("VID :" + vid + " PID :" + pid.Substring(0, 4));
                        }
                        

                        //parcalar[1].ToString().Split('&');                          
                    }





                }

            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string yeniVID = listBox1.SelectedItem.ToString().Split(':')[1].Substring(0, 4);
            string yeniPID = listBox1.SelectedItem.ToString().Split(':')[2].Substring(0,4);
            textVID.Text = yeniVID;
            textPID.Text = yeniPID;
        }

        private void inputListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
