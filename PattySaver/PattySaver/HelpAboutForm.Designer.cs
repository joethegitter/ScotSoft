namespace ScotSoft.PattySaver
{
    partial class HelpAbout
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
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlInner = new System.Windows.Forms.Panel();
            this.lblKeyExplanation = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.lblCopyright = new System.Windows.Forms.Label();
            this.pbTwoGuys = new System.Windows.Forms.PictureBox();
            this.pnlMain.SuspendLayout();
            this.pnlInner.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbTwoGuys)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            this.pnlMain.BackColor = System.Drawing.SystemColors.Control;
            this.pnlMain.Controls.Add(this.pnlInner);
            this.pnlMain.Controls.Add(this.btnOK);
            this.pnlMain.Controls.Add(this.lblCopyright);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(260, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(470, 440);
            this.pnlMain.TabIndex = 2;
            // 
            // pnlInner
            // 
            this.pnlInner.AutoScroll = true;
            this.pnlInner.Controls.Add(this.lblKeyExplanation);
            this.pnlInner.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlInner.Location = new System.Drawing.Point(0, 0);
            this.pnlInner.Name = "pnlInner";
            this.pnlInner.Size = new System.Drawing.Size(470, 381);
            this.pnlInner.TabIndex = 4;
            // 
            // lblKeyExplanation
            // 
            this.lblKeyExplanation.AutoSize = true;
            this.lblKeyExplanation.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblKeyExplanation.Location = new System.Drawing.Point(13, 9);
            this.lblKeyExplanation.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblKeyExplanation.Name = "lblKeyExplanation";
            this.lblKeyExplanation.Size = new System.Drawing.Size(232, 18);
            this.lblKeyExplanation.TabIndex = 5;
            this.lblKeyExplanation.Text = "We put key explanations here";
            this.lblKeyExplanation.UseMnemonic = false;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(371, 405);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = true;
            this.lblCopyright.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCopyright.Location = new System.Drawing.Point(13, 418);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(215, 13);
            this.lblCopyright.TabIndex = 2;
            this.lblCopyright.Text = "Code and Images © Two Guys In White, LLC";
            // 
            // pbTwoGuys
            // 
            this.pbTwoGuys.BackColor = System.Drawing.Color.Transparent;
            this.pbTwoGuys.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pbTwoGuys.Dock = System.Windows.Forms.DockStyle.Left;
            this.pbTwoGuys.Image = global::ScotSoft.PattySaver.PattySaverResources.twoguys;
            this.pbTwoGuys.Location = new System.Drawing.Point(0, 0);
            this.pbTwoGuys.Name = "pbTwoGuys";
            this.pbTwoGuys.Size = new System.Drawing.Size(260, 440);
            this.pbTwoGuys.TabIndex = 0;
            this.pbTwoGuys.TabStop = false;
            // 
            // HelpAbout
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkRed;
            this.ClientSize = new System.Drawing.Size(730, 440);
            this.ControlBox = false;
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pbTwoGuys);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HelpAbout";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "-- Title Not Set From Code --";
            this.Load += new System.EventHandler(this.HelpAbout_Load);
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.pnlInner.ResumeLayout(false);
            this.pnlInner.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbTwoGuys)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbTwoGuys;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.Panel pnlInner;
        private System.Windows.Forms.Label lblKeyExplanation;
    }
}