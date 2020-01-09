using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tray_app
{
    public partial class Form1 : Form
    {
        private bool disabled = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void disableButton_Click(object sender, EventArgs e)
        {
            disabled = !disabled;
            this.disableButton.Text = disabled ? "Enable" : "Disable";
        }

    }
}
