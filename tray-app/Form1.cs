using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace tray_app
{
    public partial class Form1 : Form
    {

        public class Flag
        {
            public bool disabled = false;
        }

        private CancellationTokenSource tokenSource;

        private Flag  flag = new Flag();

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
           _ = Task.Factory.StartNew(() => LongWork(token, flag, progress), TaskCreationOptions.LongRunning);

            await loadMonitors();
        }

        private async Task loadMonitors()
        {
            Task<string> s = Task.Run(()=>MonitorLoader());
            label1.Text = await s; 
        }

        private async Task<string> MonitorLoader() {
            await Task.Delay(15000);
            return "data";
        }

        private void disableButton_Click(object sender, EventArgs e)
        {
            flag.disabled = !flag.disabled;
            this.disableButton.Text = flag.disabled ? "Enable" : "Disable";
        }

        public void LongWork(CancellationToken token, Flag flag, IProgress<string> progress)
        {
            // Perform a long running work...
            while (true)
            {
                if(flag.disabled) continue;

                if (token.IsCancellationRequested)
                    break;
                Task.Delay(500).Wait();
                progress.Report(new Random().Next().ToString());
            }
        }
    }
}
