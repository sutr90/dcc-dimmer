using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using tray_app_mvc.controller;
using tray_app_mvc.model;

namespace tray_app_mvc
{
    public class DdcApplicationContext : ApplicationContext
    {
        private Form1 ConfigWindow { get; }

        private readonly NotifyIcon _notifyIcon = new NotifyIcon();


        public DdcApplicationContext()
        {
            var monitorControllers = new List<IMonitorController>();
            ConfigWindow = new Form1();
            var monitorModel = new MonitorModel();
            monitorModel.BrightnessChanged += ConfigWindow.OnMonitorBrightnessChanged;
            monitorModel.BrightnessChanged += BrightnessChanged;
            monitorModel.DisplayListChanged += ConfigWindow.OnDisplayListChanged;
            var monitorDisplayController = new MonitorDisplayController(monitorModel);

            monitorControllers.Add(monitorDisplayController);
            monitorControllers.Add(new MonitorUserController(monitorModel));
            foreach (var controller in monitorControllers)
            {
                ConfigWindow.BrightnessChanged += controller.OnUserChangedBrightness;
                ConfigWindow.Shutdown += controller.OnShutdown;
            }

            ConfigWindow.RefreshDisplayList += monitorDisplayController.OnRefreshDisplayList;

            var sensorModel = new SensorModel();
            var sensorController = new SensorController(sensorModel, monitorDisplayController);

            sensorModel.SensorValueChanged += ConfigWindow.OnSensorValueChanged;
            sensorModel.DeviceListChanged += sensorController.OnDeviceListChanged;
            ConfigWindow.Shutdown += sensorController.OnShutdown;
            ConfigWindow.ManualModeChanged += sensorController.OnManualModeChanged;

            ToolStripItem button1 = new ToolStripMenuItem("Configuration", null, ShowConfig);
            ToolStripItem button2 = new ToolStripMenuItem("Exit", null, Exit);

            _notifyIcon.Icon = new Icon(Resources.Icon50, SystemInformation.SmallIconSize);

            var contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(button1);
            contextMenuStrip.Items.Add(button2);

            _notifyIcon.ContextMenuStrip = contextMenuStrip;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += ShowConfig;


            monitorDisplayController.OnRefreshDisplayList();
            sensorController.OnDeviceListChanged();
        }

        private void BrightnessChanged(ModelBrightnessChangedEventArgs obj)
        {
            var brightness = obj.Brightness;

            var icon = Resources.Icon100;
            if (brightness < 12)
            {
                icon = Resources.Icon0;
            }
            else if (brightness < 38)
            {
                icon = Resources.Icon25;
            }
            else if (brightness < 62)
            {
                icon = Resources.Icon50;
            }
            else if (brightness < 88)
            {
                icon = Resources.Icon75;
            }

            _notifyIcon.Icon = new Icon(icon, SystemInformation.SmallIconSize);
        }


        private void ShowConfig(object sender, EventArgs e)
        {
            // If we are already showing the window merely focus it.
            if (ConfigWindow.Visible)
                ConfigWindow.Focus();
            else
                ConfigWindow.ShowDialog();
        }

        private void Exit(object sender, EventArgs e)
        {
            ConfigWindow.ShutdownApplication();
            _notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}