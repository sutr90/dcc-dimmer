using System.Windows.Forms;

namespace tray_app_mvc
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.currentBrightnessLabel = new System.Windows.Forms.Label();
            this.brightnessTextbox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.sensorValueLabel = new System.Windows.Forms.Label();
            this.setBrightnessButton = new System.Windows.Forms.Button();
            this.manualModeButton = new System.Windows.Forms.Button();
            this.refreshDisplayListButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Current brightness";
            // 
            // label2
            // 
            this.currentBrightnessLabel.AutoSize = true;
            this.currentBrightnessLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.currentBrightnessLabel.Location = new System.Drawing.Point(134, 9);
            this.currentBrightnessLabel.Name = "currentBrightnessLabel";
            this.currentBrightnessLabel.Size = new System.Drawing.Size(38, 15);
            this.currentBrightnessLabel.TabIndex = 1;
            this.currentBrightnessLabel.Text = "-1";
            // 
            // textBox1
            // 
            this.brightnessTextbox.Location = new System.Drawing.Point(12, 47);
            this.brightnessTextbox.Name = "textBox1";
            this.brightnessTextbox.Size = new System.Drawing.Size(105, 23);
            this.brightnessTextbox.TabIndex = 3;
            this.brightnessTextbox.Enabled = false;
            this.brightnessTextbox.TextAlign = HorizontalAlignment.Right;
            this.brightnessTextbox.Validating += new System.ComponentModel.CancelEventHandler(this.brightnessTextbox_Validating);  
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label3.Location = new System.Drawing.Point(44, 24);
            this.label3.Name = "label3";
            this.label3.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.label3.Size = new System.Drawing.Size(73, 20);
            this.label3.TabIndex = 0;
            this.label3.Text = "Sensor value";
            // 
            // label5
            // 
            this.sensorValueLabel.AutoSize = true;
            this.sensorValueLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.sensorValueLabel.Location = new System.Drawing.Point(134, 29);
            this.sensorValueLabel.Name = "sensorValueLabel";
            this.sensorValueLabel.Size = new System.Drawing.Size(38, 15);
            this.sensorValueLabel.TabIndex = 1;
            this.sensorValueLabel.Text = "-1";
            // 
            // button2
            // 
            this.setBrightnessButton.Location = new System.Drawing.Point(134, 46);
            this.setBrightnessButton.Name = "button2";
            this.setBrightnessButton.Size = new System.Drawing.Size(95, 25);
            this.setBrightnessButton.TabIndex = 4;
            this.setBrightnessButton.Text = "Set brightness";
            this.setBrightnessButton.Enabled = false;
            this.setBrightnessButton.UseVisualStyleBackColor = true;
            this.setBrightnessButton.Click += new System.EventHandler(this.setBrightnessButton_Click);
            // 
            // button3
            // 
            this.manualModeButton.Location = new System.Drawing.Point(12, 86);
            this.manualModeButton.Name = "manualModeButton";
            this.manualModeButton.Size = new System.Drawing.Size(217, 23);
            this.manualModeButton.TabIndex = 4;
            this.manualModeButton.Text = "Switch to Manual mode";
            this.manualModeButton.UseVisualStyleBackColor = true;
            this.manualModeButton.Click += new System.EventHandler(this.manualModeButton_Click);
            // 
            // button4
            // 
            this.refreshDisplayListButton.Location = new System.Drawing.Point(12, 206);
            this.refreshDisplayListButton.Name = "refreshDisplayListButton";
            this.refreshDisplayListButton.Size = new System.Drawing.Size(217, 23);
            this.refreshDisplayListButton.TabIndex = 5;
            this.refreshDisplayListButton.Text = "Refresh display list";
            this.refreshDisplayListButton.UseVisualStyleBackColor = true;
            this.refreshDisplayListButton.Click += new System.EventHandler(this.refreshButton_Click);

            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label6.Location = new System.Drawing.Point(12, 120);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(38, 15);
            this.label6.TabIndex = 1;
            this.label6.Text = "Detected Monitors";
            //
            // ListBox
            //
            // Create a new ListBox control.
            this.listBox1 = new System.Windows.Forms.ListBox();
            listBox1.Bounds = new System.Drawing.Rectangle(new System.Drawing.Point(13, 136), new System.Drawing.Size(215, 64));
            listBox1.BeginUpdate();
            listBox1.Items.Add("No monitors found.");
            listBox1.EndUpdate();

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(240, 242);
            this.Controls.Add(this.manualModeButton);
            this.Controls.Add(this.refreshDisplayListButton);
            this.Controls.Add(this.setBrightnessButton);
            this.Controls.Add(this.sensorValueLabel);
            this.Controls.Add(this.brightnessTextbox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.currentBrightnessLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.label6);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "HID ALS";
            
            this.errorProvider1 = new ErrorProvider();
            this.errorProvider1.DataMember = null;  
            
            this.ResumeLayout(false);
            this.PerformLayout();
            
            errorProvider1.ContainerControl = this;  
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label currentBrightnessLabel;
        private System.Windows.Forms.TextBox brightnessTextbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label sensorValueLabel;
        private System.Windows.Forms.Button setBrightnessButton;
        private System.Windows.Forms.Button manualModeButton;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button refreshDisplayListButton;
        private System.Windows.Forms.ErrorProvider errorProvider1;  

    }
}

