using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using tray_app_mvc.model;

namespace tray_app_mvc
{
    public partial class Form1 : Form
    {
        public void OnMonitorBrightnessChanged(ModelBrightnessChangedEventArgs e)
        {
            currentBrightnessLabel.Text = e.Brightness.ToString();
            brightnessTextbox.Text = e.Brightness.ToString();
        }

        private bool _manualMode;

        public Form1()
        {
            InitializeComponent();
        }

        private void manualModeButton_Click(object sender, EventArgs e)
        {
            _manualMode = !_manualMode;
            manualModeButton.Text = _manualMode ? "Switch to Automatic" : "Switch to Manual";
            ManualModeChanged?.Invoke(_manualMode);
            setBrightnessButton.Enabled = _manualMode;
            brightnessTextbox.Enabled = _manualMode;
            errorProvider1.SetError(brightnessTextbox, null);
        }

        private void setBrightnessButton_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren()) return;

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
                        () => { sensorValueLabel.Text = luxValue; }
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

        private void brightnessTextbox_Validating(object sender, CancelEventArgs e)
        {
            e.Cancel = ValidateBrightness();
        }

        private bool ValidateBrightness()
        {
            if (!int.TryParse(brightnessTextbox.Text, out var brightness) || brightness < 0 || brightness > 100)
            {
                errorProvider1.SetError(brightnessTextbox, "Please input valid brightness value [0-100]");
                return true;
            }

            errorProvider1.SetError(brightnessTextbox, null);
            return false;
        }
    }

    public class ViewBrightnessChangedEventArgs : EventArgs
    {
        public int Brightness { get; set; }
    }
}