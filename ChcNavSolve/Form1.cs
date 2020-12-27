using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ChcNavSolve
{
    public partial class Form1 : Form
    {
        private TcpClient tcpClient;
        private BackgroundWorker backgroundWorker;
        private System.Timers.Timer hourTimer;
        private System.Timers.Timer minuteTimer;

        private double[] init = { 0, 0, 2912677.7541,2912760.4699,2912879.327,2912990.7365,2913089.3157,2912677.7541 };
        public Form1()
        {
            InitializeComponent();
            //tcpClient = new TcpClient("localhost", Int32.Parse(textBoxPort.Text));
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;

            hourTimer = new System.Timers.Timer(1000 * 20);
            hourTimer.Elapsed += new System.Timers.ElapsedEventHandler(HourTimer_TimesUp);
            hourTimer.AutoReset = true; //每到指定时间Elapsed事件是触发一次（false），还是一直触发（true）
            minuteTimer = new System.Timers.Timer(1000 * 3);
            minuteTimer.Elapsed += new System.Timers.ElapsedEventHandler(MinuteTimer_TimesUp);
            minuteTimer.AutoReset = false; //每到指定时间Elapsed事件是触发一次（false），还是一直触发（true）
        }

        private void HourTimer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            //isSaveRawData = true;
            minuteTimer.Start();

            AppendLog(DateTime.Now.ToString()+" HourTimer...up");
        }

        private void MinuteTimer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            //isSaveRawData = false;
            AppendLog(DateTime.Now.ToString() + " MinuteTimer...up");
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            //backgroundWorker.RunWorkerAsync();
            hourTimer.Start();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            //string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            byte[] data = new Byte[1024];
            NetworkStream stream = tcpClient.GetStream();

            while (true)
            {
                try
                {
                    if (tcpClient.Connected)
                    {
                        // Read the first batch of the TcpServer response bytes.
                        Int32 bytes = stream.Read(data, 0, data.Length);
                        int offset = 51;
                        int n = bytes / 51;

                        if (n < 1)
                        {
                            continue;
                        }

                        for(int i = 0; i < n; i++)
                        {
                            if(data[0+offset*i] == 0x24 && data[49+offset*i]==0x0d && data[50 + offset * i] == 0x0a)
                            {
                                int stationId = data[12 + offset * i];
                                double x = BitConverter.ToDouble(data, 24 + i * offset);
                                double y = BitConverter.ToDouble(data, 32 + i * offset);
                                double z = BitConverter.ToDouble(data, 40 + i * offset);

                                string content = "station id: " + stationId + " x: " + x.ToString() + " y: " + y.ToString() + " z: " + z.ToString();

                                AppendLog(content);
                                UpdateChart(stationId, (x-init[stationId])*1000);
                                //chart1.Series[0].Points.AddY(x);
                                //chart1.Series[0].Points.AddY(x);
                                //chart1.Series[0].Points.AddY(x);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (backgroundWorker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }
                    stream.Close();
                    using (StreamWriter sw = new StreamWriter(@"ErrLog.txt", true))
                    {
                        sw.WriteLine(ex.Message + " \r\n" + ex.StackTrace.ToString());
                        sw.WriteLine("---------------------------------------------------------");
                        sw.Close();
                    }
                }
            }
        }

        private void UpdateChart(int station,double x)
        {
            if (chart1.InvokeRequired)
            {
                chart1.BeginInvoke(new MethodInvoker(() =>
                {
                    chart1.Series[station].Points.AddY(x);

                    if (chart1.Series[station].Points.Count > 256)
                    {
                        chart1.Series[station].Points.RemoveAt(0);
                    }
                    chart1.ResetAutoValues();
                    chart1.Invalidate();
                }));
            }
            else
            {
                chart1.Series[station].Points.AddY(x);

                if (chart1.Series[station].Points.Count > 256)
                {
                    chart1.Series[station].Points.RemoveAt(0);
                }
                chart1.ResetAutoValues();
                chart1.Invalidate();
            }
        }

        private void AppendLog(string message)
        {
            if (textBoxLog.InvokeRequired)
            {
                textBoxLog.BeginInvoke(new MethodInvoker(() =>
                {
                    textBoxLog.AppendText(message + " \r\n");
                }));
            }
            else
            {
                textBoxLog.AppendText(message + " \r\n");
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            //tcpClient.Close();
            //backgroundWorker.CancelAsync();
            hourTimer.Stop();
        }
    }
}
