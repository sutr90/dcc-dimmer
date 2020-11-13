using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using tray_app_mcv.View;
using tray_app_mcv.Model;
using tray_app_mcv.Controller;

namespace tray_app_mvc
{
    public class DdcApplicationContext : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();
        Form1 configWindow;
        MonitorModel mdl;
        IController cnt;

        public DdcApplicationContext()
        {
            configWindow = new Form1();
            mdl = new MonitorModel();
            cnt = new MonitorController(configWindow, mdl);


            ToolStripItem button1 = new ToolStripMenuItem("Configuration", null, ShowConfig);
            ToolStripItem button2 = new ToolStripMenuItem("Exit", null, Exit);

            notifyIcon.Icon = tray_app_mvc.Resources.AppIcon;

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(button1);
            contextMenuStrip.Items.Add(button2);

            notifyIcon.ContextMenuStrip = contextMenuStrip;
            notifyIcon.Visible = true;
        }

        void ShowConfig(object sender, EventArgs e)
        {
            // If we are already showing the window meerly focus it.
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