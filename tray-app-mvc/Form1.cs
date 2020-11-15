#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using tray_app_mvc.model;
using tray_app_mvc.view;

namespace tray_app_mvc
{
    public partial class Form1 : Form, IView
    {
        public void OnMonitorBrightnessChanged(ModelBrightnessChangedEventArgs e)
        {
            Debug.Print("view recv ModelBrightnessChangedEventArgs");
            currentBrightnessLabel.Text = e.Brightness.ToString();
        }

        private bool _disabled;

        public Form1()
        {
            InitializeComponent();
        }

        private void disableButton_Click(object sender, EventArgs e)
        {
            _disabled = !_disabled;
            disableButton.Text = _disabled ? "Enable" : "Disable";
        }

        private void setBrightnessButton_Click(object sender, EventArgs e)
        {
            var b = GetBrightnessValue();
            var args = new IView.ViewBrightnessChangedEventArgs {Brightness = b};
            DispatchBrightnessChanged(args);
        }

        private void DispatchBrightnessChanged(IView.ViewBrightnessChangedEventArgs e)
        {
            Debug.Print("view raise ViewBrightnessChangedEventArgs");
            var handler = BrightnessChanged;
            handler.Invoke(e);
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

        public event Action<IView.ViewBrightnessChangedEventArgs> BrightnessChanged;
        public event Action RefreshDisplayList;

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

        private void refreshButton_Click(object? sender, EventArgs e)
        {
            RefreshDisplayList.Invoke();
        }
    }
}