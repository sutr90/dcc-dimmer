using System;
using System.Collections.Generic;
using System.Windows.Forms;
using tray_app_mvc.controller;
using tray_app_mvc.model;

namespace tray_app_mvc
{
    public class DdcApplicationContext : ApplicationContext
    {
        private Form1 ConfigWindow { get; }
        
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();

        private readonly List<IMonitorController> _monitorControllers = new  List<IMonitorController>();

        public DdcApplicationContext()
        {
            ConfigWindow = new Form1();
            var monitorModel = new MonitorModel();
            monitorModel.BrightnessChanged += ConfigWindow.OnMonitorBrightnessChanged;
            monitorModel.DisplayListChanged += ConfigWindow.OnDisplayListChanged;
            var monitorDisplayController = new MonitorDisplayController(monitorModel);
            
            _monitorControllers.Add(monitorDisplayController);
            _monitorControllers.Add(new MonitorUserController(monitorModel));
            foreach (var controller in _monitorControllers)
            {
                ConfigWindow.BrightnessChanged += controller.OnUserChangedBrightness;
            }
            
            ConfigWindow.RefreshDisplayList += monitorDisplayController.OnRefreshDisplayList;

            var sensorModel = new SensorModel();
            var sensorController = new SensorController(sensorModel);

            sensorModel.SensorValueChanged += ConfigWindow.OnSensorValueChanged;
            sensorModel.DeviceListChanged += sensorController.OnDeviceListChanged;

            ToolStripItem button1 = new ToolStripMenuItem("Configuration", null, ShowConfig);
            ToolStripItem button2 = new ToolStripMenuItem("Exit", null, Exit);

            _notifyIcon.Icon = Resources.AppIcon;

            var contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(button1);
            contextMenuStrip.Items.Add(button2);

            _notifyIcon.ContextMenuStrip = contextMenuStrip;
            _notifyIcon.Visible = true;
            
            
            monitorDisplayController.OnRefreshDisplayList();
            sensorController.OnDeviceListChanged();
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
            // TODO: ConfigWindow.Shutdown();
            _notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}