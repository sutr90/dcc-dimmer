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
        
        private static readonly Icon _icon0 = new Icon(Resources.Icon0, SystemInformation.SmallIconSize);
        private static readonly Icon _icon25 = new Icon(Resources.Icon25, SystemInformation.SmallIconSize);
        private static readonly Icon _icon50 = new Icon(Resources.Icon50, SystemInformation.SmallIconSize);
        private static readonly Icon _icon75 = new Icon(Resources.Icon75, SystemInformation.SmallIconSize);
        private static readonly Icon _icon100 = new Icon(Resources.Icon100, SystemInformation.SmallIconSize);


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

            var icon = _icon100;
            if (brightness < 12)
            {
                icon = _icon0;
            }
            else if (brightness < 38)
            {
                icon = _icon25;
            }
            else if (brightness < 62)
            {
                icon = _icon50;
            }
            else if (brightness < 88)
            {
                icon = _icon75;
            }

            _notifyIcon.Icon = icon;
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