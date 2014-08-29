namespace ScotSoft.PattySaver
{
    partial class Settings
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
            this.Save = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.Add = new System.Windows.Forms.Button();
            this.Shuffle = new System.Windows.Forms.CheckBox();
            this.Remove = new System.Windows.Forms.Button();
            this.Folders = new System.Windows.Forms.CheckedListBox();
            this.SpeedLabel = new System.Windows.Forms.Label();
            this.btnChooseMetadata = new System.Windows.Forms.Button();
            this.ShowMetaData = new System.Windows.Forms.CheckBox();
            this.SlideshowInterval = new System.Windows.Forms.ComboBox();
            this.UseOnlyChecked = new System.Windows.Forms.CheckBox();
            this.PictureFolderWarning = new System.Windows.Forms.Label();
            this.Recursion = new System.Windows.Forms.CheckBox();
            this.Instructions = new System.Windows.Forms.Label();
            this.DividerLine = new System.Windows.Forms.Label();
            this.DividerLineBottom = new System.Windows.Forms.Label();
            this.btnChooseFont = new System.Windows.Forms.Button();
            this.btnChooseFontColor = new System.Windows.Forms.Button();
            this.lblFontDescription = new System.Windows.Forms.Label();
            this.btnChooseEffects = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Save
            // 
            this.Save.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Save.Location = new System.Drawing.Point(440, 374);
            this.Save.Name = "Save";
            this.Save.Size = new System.Drawing.Size(86, 25);
            this.Save.TabIndex = 1;
            this.Save.Text = "Save";
            this.Save.UseVisualStyleBackColor = true;
            this.Save.Click += new System.EventHandler(this.Save_Click);
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Cancel.Location = new System.Drawing.Point(533, 374);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(86, 25);
            this.Cancel.TabIndex = 2;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // Add
            // 
            this.Add.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Add.Location = new System.Drawing.Point(440, 12);
            this.Add.Name = "Add";
            this.Add.Size = new System.Drawing.Size(86, 25);
            this.Add.TabIndex = 3;
            this.Add.Text = "Add...";
            this.Add.UseVisualStyleBackColor = true;
            this.Add.Click += new System.EventHandler(this.Add_Click);
            // 
            // Shuffle
            // 
            this.Shuffle.AutoSize = true;
            this.Shuffle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Shuffle.Location = new System.Drawing.Point(471, 253);
            this.Shuffle.Name = "Shuffle";
            this.Shuffle.Size = new System.Drawing.Size(89, 19);
            this.Shuffle.TabIndex = 10;
            this.Shuffle.Text = "Shuffle Files";
            this.Shuffle.UseVisualStyleBackColor = true;
            // 
            // Remove
            // 
            this.Remove.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Remove.Location = new System.Drawing.Point(533, 12);
            this.Remove.Name = "Remove";
            this.Remove.Size = new System.Drawing.Size(86, 25);
            this.Remove.TabIndex = 4;
            this.Remove.Text = "Remove...";
            this.Remove.UseVisualStyleBackColor = true;
            this.Remove.Click += new System.EventHandler(this.Remove_Click);
            // 
            // Folders
            // 
            this.Folders.CheckOnClick = true;
            this.Folders.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Folders.FormattingEnabled = true;
            this.Folders.Location = new System.Drawing.Point(12, 46);
            this.Folders.Name = "Folders";
            this.Folders.Size = new System.Drawing.Size(607, 166);
            this.Folders.TabIndex = 5;
            this.Folders.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.Folders_ItemCheck);
            // 
            // SpeedLabel
            // 
            this.SpeedLabel.AutoSize = true;
            this.SpeedLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SpeedLabel.Location = new System.Drawing.Point(432, 224);
            this.SpeedLabel.Name = "SpeedLabel";
            this.SpeedLabel.Size = new System.Drawing.Size(138, 15);
            this.SpeedLabel.TabIndex = 8;
            this.SpeedLabel.Text = "Slideshow Interval (secs):";
            // 
            // btnChooseMetadata
            // 
            this.btnChooseMetadata.AutoSize = true;
            this.btnChooseMetadata.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnChooseMetadata.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnChooseMetadata.Location = new System.Drawing.Point(134, 302);
            this.btnChooseMetadata.Name = "btnChooseMetadata";
            this.btnChooseMetadata.Size = new System.Drawing.Size(119, 25);
            this.btnChooseMetadata.TabIndex = 12;
            this.btnChooseMetadata.Text = "Choose Metadata...";
            this.btnChooseMetadata.UseVisualStyleBackColor = true;
            this.btnChooseMetadata.Click += new System.EventHandler(this.ChooseMetadata_Click);
            // 
            // ShowMetaData
            // 
            this.ShowMetaData.AutoSize = true;
            this.ShowMetaData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowMetaData.Location = new System.Drawing.Point(15, 306);
            this.ShowMetaData.Name = "ShowMetaData";
            this.ShowMetaData.Size = new System.Drawing.Size(108, 19);
            this.ShowMetaData.TabIndex = 11;
            this.ShowMetaData.Text = "Show Metadata";
            this.ShowMetaData.UseVisualStyleBackColor = true;
            // 
            // SlideshowInterval
            // 
            this.SlideshowInterval.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SlideshowInterval.FormattingEnabled = true;
            this.SlideshowInterval.Location = new System.Drawing.Point(571, 219);
            this.SlideshowInterval.Name = "SlideshowInterval";
            this.SlideshowInterval.Size = new System.Drawing.Size(39, 23);
            this.SlideshowInterval.TabIndex = 9;
            // 
            // UseOnlyChecked
            // 
            this.UseOnlyChecked.AutoSize = true;
            this.UseOnlyChecked.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UseOnlyChecked.Location = new System.Drawing.Point(15, 223);
            this.UseOnlyChecked.Name = "UseOnlyChecked";
            this.UseOnlyChecked.Size = new System.Drawing.Size(175, 19);
            this.UseOnlyChecked.TabIndex = 6;
            this.UseOnlyChecked.Text = "Search only checked Folders";
            this.UseOnlyChecked.UseVisualStyleBackColor = true;
            this.UseOnlyChecked.CheckedChanged += new System.EventHandler(this.UseOnlyChecked_CheckedChanged);
            // 
            // PictureFolderWarning
            // 
            this.PictureFolderWarning.AutoSize = true;
            this.PictureFolderWarning.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PictureFolderWarning.ForeColor = System.Drawing.SystemColors.ControlText;
            this.PictureFolderWarning.Location = new System.Drawing.Point(56, 254);
            this.PictureFolderWarning.Name = "PictureFolderWarning";
            this.PictureFolderWarning.Size = new System.Drawing.Size(295, 15);
            this.PictureFolderWarning.TabIndex = 14;
            this.PictureFolderWarning.Text = "If no folders are listed, your Pictures Folder will be used.";
            // 
            // Recursion
            // 
            this.Recursion.AutoSize = true;
            this.Recursion.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Recursion.Location = new System.Drawing.Point(201, 223);
            this.Recursion.Name = "Recursion";
            this.Recursion.Size = new System.Drawing.Size(216, 19);
            this.Recursion.TabIndex = 7;
            this.Recursion.Text = "Search all Folders below each Folder";
            this.Recursion.UseVisualStyleBackColor = true;
            // 
            // Instructions
            // 
            this.Instructions.AutoSize = true;
            this.Instructions.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Instructions.Location = new System.Drawing.Point(12, 17);
            this.Instructions.Name = "Instructions";
            this.Instructions.Size = new System.Drawing.Size(234, 15);
            this.Instructions.TabIndex = 15;
            this.Instructions.Text = "Search these Folders for pictures to display:";
            // 
            // DividerLine
            // 
            this.DividerLine.BackColor = System.Drawing.SystemColors.ControlDark;
            this.DividerLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.DividerLine.Enabled = false;
            this.DividerLine.Location = new System.Drawing.Point(25, 288);
            this.DividerLine.Name = "DividerLine";
            this.DividerLine.Size = new System.Drawing.Size(580, 2);
            this.DividerLine.TabIndex = 16;
            // 
            // DividerLineBottom
            // 
            this.DividerLineBottom.BackColor = System.Drawing.SystemColors.ControlDark;
            this.DividerLineBottom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.DividerLineBottom.Enabled = false;
            this.DividerLineBottom.Location = new System.Drawing.Point(0, 363);
            this.DividerLineBottom.Name = "DividerLineBottom";
            this.DividerLineBottom.Size = new System.Drawing.Size(642, 2);
            this.DividerLineBottom.TabIndex = 17;
            // 
            // btnChooseFont
            // 
            this.btnChooseFont.AutoSize = true;
            this.btnChooseFont.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnChooseFont.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnChooseFont.Location = new System.Drawing.Point(259, 302);
            this.btnChooseFont.Name = "btnChooseFont";
            this.btnChooseFont.Size = new System.Drawing.Size(93, 25);
            this.btnChooseFont.TabIndex = 13;
            this.btnChooseFont.Text = "Choose Font...";
            this.btnChooseFont.UseVisualStyleBackColor = true;
            this.btnChooseFont.Click += new System.EventHandler(this.btnChooseFont_Click);
            // 
            // btnChooseFontColor
            // 
            this.btnChooseFontColor.AutoSize = true;
            this.btnChooseFontColor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnChooseFontColor.Enabled = false;
            this.btnChooseFontColor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnChooseFontColor.Location = new System.Drawing.Point(358, 302);
            this.btnChooseFontColor.Name = "btnChooseFontColor";
            this.btnChooseFontColor.Size = new System.Drawing.Size(91, 25);
            this.btnChooseFontColor.TabIndex = 14;
            this.btnChooseFontColor.Text = "More Colors...";
            this.btnChooseFontColor.UseVisualStyleBackColor = true;
            this.btnChooseFontColor.Click += new System.EventHandler(this.btnChooseFontColor_Click);
            // 
            // lblFontDescription
            // 
            this.lblFontDescription.AutoSize = true;
            this.lblFontDescription.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFontDescription.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblFontDescription.Location = new System.Drawing.Point(256, 337);
            this.lblFontDescription.Name = "lblFontDescription";
            this.lblFontDescription.Size = new System.Drawing.Size(70, 15);
            this.lblFontDescription.TabIndex = 14;
            this.lblFontDescription.Text = "Initialize Me";
            // 
            // btnChooseEffects
            // 
            this.btnChooseEffects.AutoSize = true;
            this.btnChooseEffects.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnChooseEffects.Enabled = false;
            this.btnChooseEffects.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnChooseEffects.Location = new System.Drawing.Point(456, 302);
            this.btnChooseEffects.Name = "btnChooseEffects";
            this.btnChooseEffects.Size = new System.Drawing.Size(95, 25);
            this.btnChooseEffects.TabIndex = 15;
            this.btnChooseEffects.Text = "Better Effects...";
            this.btnChooseEffects.UseVisualStyleBackColor = true;
            // 
            // Settings
            // 
            this.AcceptButton = this.Save;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(626, 414);
            this.ControlBox = false;
            this.Controls.Add(this.DividerLineBottom);
            this.Controls.Add(this.DividerLine);
            this.Controls.Add(this.Instructions);
            this.Controls.Add(this.lblFontDescription);
            this.Controls.Add(this.PictureFolderWarning);
            this.Controls.Add(this.Recursion);
            this.Controls.Add(this.UseOnlyChecked);
            this.Controls.Add(this.SlideshowInterval);
            this.Controls.Add(this.SpeedLabel);
            this.Controls.Add(this.Folders);
            this.Controls.Add(this.Remove);
            this.Controls.Add(this.Shuffle);
            this.Controls.Add(this.Add);
            this.Controls.Add(this.btnChooseEffects);
            this.Controls.Add(this.btnChooseFontColor);
            this.Controls.Add(this.btnChooseFont);
            this.Controls.Add(this.btnChooseMetadata);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.Save);
            this.Controls.Add(this.ShowMetaData);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "-- Text Not Set From Code --";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Settings_FormClosed);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Save;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button Add;
        private System.Windows.Forms.CheckBox Shuffle;
        private System.Windows.Forms.Button Remove;
        private System.Windows.Forms.CheckedListBox Folders;
        private System.Windows.Forms.Label SpeedLabel;
        private System.Windows.Forms.Button btnChooseMetadata;
        private System.Windows.Forms.CheckBox ShowMetaData;
        private System.Windows.Forms.ComboBox SlideshowInterval;
        private System.Windows.Forms.CheckBox UseOnlyChecked;
        private System.Windows.Forms.Label PictureFolderWarning;
        private System.Windows.Forms.CheckBox Recursion;
        private System.Windows.Forms.Label Instructions;
        private System.Windows.Forms.Label DividerLine;
        private System.Windows.Forms.Label DividerLineBottom;
        private System.Windows.Forms.Button btnChooseFont;
        private System.Windows.Forms.Button btnChooseFontColor;
        private System.Windows.Forms.Label lblFontDescription;
        private System.Windows.Forms.Button btnChooseEffects;
    }
}