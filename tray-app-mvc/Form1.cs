using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using tray_app_mvc.model;

namespace tray_app_mvc
{
    public partial class Form1 : Form
    {
        public void OnMonitorBrightnessChanged(ModelBrightnessChangedEventArgs e)
        {
            currentBrightnessLabel.Text = e.Brightness.ToString();
        }

        private bool _manualMode;

        public Form1()
        {
            InitializeComponent();
        }

        private void manualModeButton_Click(object sender, EventArgs e)
        {
            _manualMode = !_manualMode;
            manualModeButton.Text = _manualMode ? "Manual" : "Automatic";
            ManualModeChanged?.Invoke(_manualMode);
        }

        private void setBrightnessButton_Click(object sender, EventArgs e)
        {
            var b = GetBrightnessValue();
            var args = new ViewBrightnessChangedEventArgs {Brightness = b};
            BrightnessChanged?.Invoke(args);
        }

        private int GetBrightnessValue()
        {
            if (int.TryParse(brightnessTextbox.Text, out var brightness))
            {
                if (brightness >= 0 && brightness <= 100)
                {
                    return brightness;
                }
            }

            Debug.WriteLine("Incorrect brightness value {0}", brightnessTextbox.Text);
            throw new Exception("Incorrect brightness value");
        }


        private void brightnessTextbox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                GetBrightnessValue();

                brightnessTextbox.ForeColor = SystemColors.ControlText;
                setBrightnessButton.Enabled = true;
            }
            catch
            {
                brightnessTextbox.BackColor = Color.Red;
                setBrightnessButton.Enabled = false;
            }
        }

        public event Action<ViewBrightnessChangedEventArgs> BrightnessChanged;
        public event Action<bool> ManualModeChanged;
        public event Action RefreshDisplayList;
        public event Action Shutdown;

        public void OnDisplayListChanged(DisplayListChangedEventArgs evt)
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (var item in evt.DisplayList)
            {
                listBox1.Items.Add(item.Description);
            }

            listBox1.EndUpdate();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            RefreshDisplayList?.Invoke();
        }

        public void OnSensorValueChanged(SensorValueChangedEventArgs e)
        {
            var luxValue = ((int) e.Value).ToString();
            if (sensorValueLabel.InvokeRequired)
            {
                sensorValueLabel.Invoke(new Action(
                        () =>
                        {
                            sensorValueLabel.Text = luxValue;
                        }
                    )
                );
            }
            else
            {
                sensorValueLabel.Text = luxValue;
            }
        }

        public void ShutdownApplication()
        {
            Shutdown?.Invoke();
        }
    }
    
    public class ViewBrightnessChangedEventArgs : EventArgs
    {
        public int Brightness { get; set; }
    }

}