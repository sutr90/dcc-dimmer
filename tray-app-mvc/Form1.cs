﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tray_app_mvc.controller;
using tray_app_mvc.model;

namespace tray_app_mvc
{
    public partial class Form1 :  Form
    {
        private readonly MonitorUserController controller;
        
        public void OnMonitorBrightnessChanged(object? sender, BrightnessChangedEventArgs e)
        {
            currentBrightnessLabel.Text = e.Brightness.ToString();
        }

        private readonly List<DdcMonitorItem> _monitors = new List<DdcMonitorItem>();

        private bool _disabled;

        private CancellationTokenSource _tokenSource;

        public Form1(MonitorUserController controller)
        {
            this.controller = controller;
            InitializeComponent();
            StartTasks();
        }

        public void Shutdown()
        {
            _tokenSource.Cancel();
        }

        private async void StartTasks()
        {
            _tokenSource = new CancellationTokenSource();
            var progress = new Progress<string>(s => currentBrightnessLabel.Text = s);
            var token = _tokenSource.Token;
            _ = Task.Factory.StartNew(() => LongWork(token, this, progress), TaskCreationOptions.LongRunning);

            await LoadMonitors();
        }

        private async Task LoadMonitors()
        {
            Task<List<DdcMonitorItem>> monitorsTask = Task.Run(() => MonitorManager.EnumerateMonitors().ToList());
            var monitorList = await monitorsTask;

            _monitors.Clear();
            foreach (var m in monitorList)
            {
                _monitors.Add(m);
            }

            if (_monitors.Count > 0)
            {
                listBox1.BeginUpdate();
                listBox1.Items.Clear();
                foreach (var item in _monitors)
                {
                    listBox1.Items.Add(item.Description);
                }
                listBox1.EndUpdate();
            }
        }

        private void disableButton_Click(object sender, EventArgs e)
        {
            _disabled = !_disabled;
            disableButton.Text = _disabled ? "Enable" : "Disable";
        }

        private void setBrightnessButton_Click(object sender, EventArgs e)
        {
            try
            {
                var b = GetBrigtnessValue();
                foreach(var m in _monitors){
                    m.SetBrightness(b);
                }

                controller.SetBrightness(b);
            }
            catch
            {
                Debug.WriteLine("Failed to set brigthness");
            }
        }

        private int GetBrigtnessValue()
        {
            var brightString = brightnessTextbox.Text;
            var brightness = int.Parse(brightString);

            if (brightness >= 0 && brightness <= 100)
            {
                return brightness;
            }

            Debug.WriteLine("brightness value out of range [0, 100]");
            throw new Exception("brightness value out of range [0, 100]");
        }


        private void brightnessTextbox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                GetBrigtnessValue();

                brightnessTextbox.ForeColor = SystemColors.ControlText;
                setBrightnessButton.Enabled = true;
            }
            catch
            {
                brightnessTextbox.BackColor = Color.Red;
                setBrightnessButton.Enabled = false;
            }
        }

        private static void LongWork(CancellationToken token, Form1 form, IProgress<string> progress)
        {
            while (true)
            {
                if (form._disabled) continue;

                if (token.IsCancellationRequested)
                    break;

                if (form._monitors.Count > 0)
                {
                    foreach (var monitor in form._monitors)
                    {
                        monitor.UpdateBrightness();
                    }

                    progress.Report(form._monitors[0].Brightness.ToString());
                }

                Task.Delay(5000, token).Wait(token);
            }
        }
    }
}
