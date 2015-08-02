namespace SingleInstanceScreenSaver
{
    partial class CP_SI_Controller
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
            this.SuspendLayout();
            // 
            // CP_SI_Controller
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(10, 10);
            this.ControlBox = false;
            this.Enabled = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Location = new System.Drawing.Point(-20, -20);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CP_SI_Controller";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "CP_SI_Controller";
            this.Activated += new System.EventHandler(this.CP_SI_Controller_Activated);
            this.Deactivate += new System.EventHandler(this.CP_SI_Controller_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CP_SI_Controller_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CP_SI_Controller_FormClosed);
            this.Load += new System.EventHandler(this.CP_SI_Controller_Load);
            this.Shown += new System.EventHandler(this.CP_SI_Controller_Shown);
            this.EnabledChanged += new System.EventHandler(this.CP_SI_Controller_EnabledChanged);
            this.LocationChanged += new System.EventHandler(this.CP_SI_Controller_LocationChanged);
            this.VisibleChanged += new System.EventHandler(this.CP_SI_Controller_VisibleChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.CP_SI_Controller_Paint);
            this.Enter += new System.EventHandler(this.CP_SI_Controller_Enter);
            this.Leave += new System.EventHandler(this.CP_SI_Controller_Leave);
            this.StyleChanged += new System.EventHandler(this.CP_SI_Controller_StyleChanged);
            this.ResumeLayout(false);

        }

        #endregion
    }
}