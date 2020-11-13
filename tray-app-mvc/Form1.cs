using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace tray_app_mvc
{
    public partial class Form1 : Form
    {
        private List<DdcMonitorItem> monitors = new List<DdcMonitorItem>();

        private bool disabled = false;

        private CancellationTokenSource tokenSource;

        public Form1()
        {
            InitializeComponent();
            startTasks();
        }

        public void Shutdown()
        {
            tokenSource.Cancel();
        }

        private async void startTasks()
        {
            tokenSource = new CancellationTokenSource();
            var progress = new Progress<string>(s => currentBrightnessLabel.Text = s);
            var token = tokenSource.Token;
            _ = Task.Factory.StartNew(() => LongWork(token, this, progress), TaskCreationOptions.LongRunning);

            await loadMonitors();
        }

        private async Task loadMonitors()
        {
            Task<List<DdcMonitorItem>> monitorsTask = Task.Run(() => MonitorManager.EnumerateMonitors().ToList());
            var monitorList = await monitorsTask;

            this.monitors.Clear();
            foreach (var m in monitorList)
            {
                this.monitors.Add(m);
            }

            if (this.monitors.Count > 0)
            {
                listBox1.BeginUpdate();
                listBox1.Items.Clear();
                foreach (var item in this.monitors)
                {
                    listBox1.Items.Add(item.Description);
                }
                listBox1.EndUpdate();
            }
        }

        private void disableButton_Click(object sender, EventArgs e)
        {
            disabled = !disabled;
            this.disableButton.Text = disabled ? "Enable" : "Disable";
        }

        private void setBrightnessButton_Click(object sender, EventArgs e)
        {
            try
            {
                var b = getBrigtnessValue();
                foreach(var m in this.monitors){
                    m.SetBrightness(b);
                }
            }
            catch
            {
                Debug.WriteLine("Failed to set brigthness");
            }
        }

        private int getBrigtnessValue()
        {
            var brightString = this.brightnessTextbox.Text;
            var brightness = int.Parse(brightString);

            if (brightness >= 0 && brightness <= 100)
            {
                return brightness;
            }
            else
            {
                Debug.WriteLine("brightness value out of range [0, 100]");
                throw new Exception("brightness value out of range [0, 100]");
            }
        }


        private void brightnessTextbox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                getBrigtnessValue();

                this.brightnessTextbox.ForeColor = SystemColors.ControlText;
                this.setBrightnessButton.Enabled = true;
            }
            catch
            {
                this.brightnessTextbox.BackColor = Color.Red;
                this.setBrightnessButton.Enabled = false;
            }
        }

        public void LongWork(CancellationToken token, Form1 form, IProgress<string> progress)
        {
            while (true)
            {
                if (form.disabled) continue;

                if (token.IsCancellationRequested)
                    break;

                if (form.monitors.Count > 0)
                {
                    foreach (var monitor in form.monitors)
                    {
                        monitor.UpdateBrightness();
                    }

                    progress.Report(form.monitors[0].Brightness.ToString());
                }

                Task.Delay(5000).Wait();
            }
        }
    }
}
