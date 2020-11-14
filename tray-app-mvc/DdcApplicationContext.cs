using System;
using System.Windows.Forms;
using tray_app_mvc.controller;
using tray_app_mvc.model;
using tray_app_mvc.view;

namespace tray_app_mvc
{
    public class DdcApplicationContext : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();
        Form1 configWindow;
        MonitorModel model;
        MonitorUserController controller;

        public DdcApplicationContext()
        {
            model = new MonitorModel();
            controller = new MonitorUserController(model);
            configWindow = new Form1();

            model.BrightnessChanged += configWindow.OnMonitorBrightnessChanged;
            ((IView)configWindow).BrightnessChanged += controller.OnUserChangedBrightness;


            ToolStripItem button1 = new ToolStripMenuItem("Configuration", null, ShowConfig);
            ToolStripItem button2 = new ToolStripMenuItem("Exit", null, Exit);

            notifyIcon.Icon = Resources.AppIcon;

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(button1);
            contextMenuStrip.Items.Add(button2);

            notifyIcon.ContextMenuStrip = contextMenuStrip;
            notifyIcon.Visible = true;
        }

        void ShowConfig(object sender, EventArgs e)
        {
            // If we are already showing the window merely focus it.
            if (configWindow.Visible)
                configWindow.Focus();
            else
                configWindow.ShowDialog();
        }

        void Exit(object sender, EventArgs e)
        {
            configWindow.Shutdown();
            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}