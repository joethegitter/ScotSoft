namespace ScotSoft.PattySaver
{
    partial class ScrollingTextWindow
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
            this.theTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // theTextBox
            // 
            this.theTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.theTextBox.Location = new System.Drawing.Point(0, 0);
            this.theTextBox.Multiline = true;
            this.theTextBox.Name = "theTextBox";
            this.theTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.theTextBox.Size = new System.Drawing.Size(350, 588);
            this.theTextBox.TabIndex = 0;
            // 
            // ScrollingTextWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(350, 588);
            this.Controls.Add(this.theTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "ScrollingTextWindow";
            this.Text = "Debug Log";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScrollingTextWindow_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ScrollingTextWindow_FormClosed);
            this.Load += new System.EventHandler(this.ScrollingTextWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox theTextBox;
    }
}