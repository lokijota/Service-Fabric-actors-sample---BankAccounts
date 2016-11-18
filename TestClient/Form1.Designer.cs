namespace TestClient
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.create100button = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.createSObutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // create100button
            // 
            this.create100button.Location = new System.Drawing.Point(23, 23);
            this.create100button.Name = "create100button";
            this.create100button.Size = new System.Drawing.Size(150, 68);
            this.create100button.TabIndex = 0;
            this.create100button.Text = "Create 100 Accounts";
            this.create100button.UseVisualStyleBackColor = true;
            this.create100button.Click += new System.EventHandler(this.create100button_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(203, 23);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(989, 557);
            this.textBox1.TabIndex = 1;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(203, 600);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(989, 48);
            this.progressBar1.Step = 1;
            this.progressBar1.TabIndex = 2;
            // 
            // createSObutton
            // 
            this.createSObutton.Location = new System.Drawing.Point(23, 115);
            this.createSObutton.Name = "createSObutton";
            this.createSObutton.Size = new System.Drawing.Size(150, 97);
            this.createSObutton.TabIndex = 3;
            this.createSObutton.Text = "Create 100 Standing Orders";
            this.createSObutton.UseVisualStyleBackColor = true;
            this.createSObutton.Click += new System.EventHandler(this.createSObutton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1218, 675);
            this.Controls.Add(this.createSObutton);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.create100button);
            this.Name = "Form1";
            this.Text = "SFActors.BankAccounts Sophisticated TestClient";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button create100button;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button createSObutton;
    }
}

