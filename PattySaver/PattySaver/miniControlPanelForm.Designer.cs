namespace ScotSoft.PattySaver
{
    partial class miniControlPanelForm
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
            // miniControlPanelForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Bisque;
            this.ClientSize = new System.Drawing.Size(148, 123);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "miniControlPanelForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Activated += new System.EventHandler(this.miniControlPanelForm_Activated);
            this.Deactivate += new System.EventHandler(this.miniControlPanelForm_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.miniControlPanelForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.miniControlPanelForm_FormClosed);
            this.Load += new System.EventHandler(this.miniControlPanelForm_Load);
            this.Shown += new System.EventHandler(this.miniControlPanelForm_Shown);
            this.ResumeLayout(false);

        }

        #endregion
    }
}