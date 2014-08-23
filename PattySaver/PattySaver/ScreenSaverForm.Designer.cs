namespace ScotSoft.PattySaver
{
    partial class ScreenSaverForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScreenSaverForm));
            this.pbMain = new System.Windows.Forms.PictureBox();
            this.contextMenuMain = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmPauseResume = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmNextPhoto = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmPreviousPhoto = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmExploreFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmNextInFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmPreviousInFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExitExploreFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmFullscreen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmShowMetadata = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmMetadataShowHide = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmMetaDataChangeFont = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmMetaDataChangeColor = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmMetadataMoveOnScreen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmFileDropDown = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmBlacklist = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmRating = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmOneStar = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsTwoStar = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmThreeStar = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmFourStar = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmFiveStar = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmRemoveRating = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmEditComments = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmEditTags = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmOpenFileExplorer = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmExit = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pbMain)).BeginInit();
            this.contextMenuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbMain
            // 
            this.pbMain.BackColor = System.Drawing.Color.Transparent;
            this.pbMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbMain.ErrorImage = global::ScotSoft.PattySaver.PattySaverResources.noimage;
            this.pbMain.InitialImage = global::ScotSoft.PattySaver.PattySaverResources.noimage;
            this.pbMain.Location = new System.Drawing.Point(0, 0);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(1432, 664);
            this.pbMain.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMain.TabIndex = 1;
            this.pbMain.TabStop = false;
            this.pbMain.WaitOnLoad = true;
            this.pbMain.LoadCompleted += new System.ComponentModel.AsyncCompletedEventHandler(this.pbMain_LoadCompleted);
            this.pbMain.Click += new System.EventHandler(this.pbMainPhoto_Click);
            this.pbMain.Paint += new System.Windows.Forms.PaintEventHandler(this.pbMainPhoto_Paint);
            // 
            // contextMenuMain
            // 
            this.contextMenuMain.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.contextMenuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmHelpAbout,
            this.tsmSeparator1,
            this.tsmPauseResume,
            this.tsmNextPhoto,
            this.tsmPreviousPhoto,
            this.tsmSeparator2,
            this.tsmExploreFolder,
            this.tsmNextInFolder,
            this.tsmPreviousInFolder,
            this.tsmExitExploreFolder,
            this.tsmSeparator3,
            this.tsmSettings,
            this.tsmSeparator4,
            this.tsmFullscreen,
            this.tsmShowMetadata,
            this.tsmSeparator5,
            this.tsmFileDropDown,
            this.tsmSeparator6,
            this.tsmExit});
            this.contextMenuMain.Name = "contextMenuMain";
            this.contextMenuMain.Size = new System.Drawing.Size(286, 326);
            this.contextMenuMain.Closed += new System.Windows.Forms.ToolStripDropDownClosedEventHandler(this.contextMenuMain_Closed);
            this.contextMenuMain.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuMain_Opening);
            // 
            // tsmHelpAbout
            // 
            this.tsmHelpAbout.Name = "tsmHelpAbout";
            this.tsmHelpAbout.ShortcutKeyDisplayString = "F1";
            this.tsmHelpAbout.Size = new System.Drawing.Size(285, 22);
            this.tsmHelpAbout.Text = "Help / About...";
            this.tsmHelpAbout.Click += new System.EventHandler(this.tsmHelpAbout_Click);
            // 
            // tsmSeparator1
            // 
            this.tsmSeparator1.Name = "tsmSeparator1";
            this.tsmSeparator1.Size = new System.Drawing.Size(282, 6);
            // 
            // tsmPauseResume
            // 
            this.tsmPauseResume.Name = "tsmPauseResume";
            this.tsmPauseResume.ShortcutKeyDisplayString = "Spacebar";
            this.tsmPauseResume.Size = new System.Drawing.Size(285, 22);
            this.tsmPauseResume.Text = "Pause / Resume Slideshow";
            this.tsmPauseResume.Click += new System.EventHandler(this.tsmPauseResume_Click);
            // 
            // tsmNextPhoto
            // 
            this.tsmNextPhoto.Name = "tsmNextPhoto";
            this.tsmNextPhoto.ShortcutKeyDisplayString = "Right";
            this.tsmNextPhoto.Size = new System.Drawing.Size(285, 22);
            this.tsmNextPhoto.Text = "Next Photo";
            this.tsmNextPhoto.Click += new System.EventHandler(this.tsmNextPhoto_Click);
            // 
            // tsmPreviousPhoto
            // 
            this.tsmPreviousPhoto.Name = "tsmPreviousPhoto";
            this.tsmPreviousPhoto.ShortcutKeyDisplayString = "Left";
            this.tsmPreviousPhoto.Size = new System.Drawing.Size(285, 22);
            this.tsmPreviousPhoto.Text = "Previous Photo";
            this.tsmPreviousPhoto.Click += new System.EventHandler(this.tsmPreviuosPhoto_Click);
            // 
            // tsmSeparator2
            // 
            this.tsmSeparator2.Name = "tsmSeparator2";
            this.tsmSeparator2.Size = new System.Drawing.Size(282, 6);
            // 
            // tsmExploreFolder
            // 
            this.tsmExploreFolder.Name = "tsmExploreFolder";
            this.tsmExploreFolder.ShortcutKeyDisplayString = "Up, Down";
            this.tsmExploreFolder.Size = new System.Drawing.Size(285, 22);
            this.tsmExploreFolder.Text = "Explore Photos In This Folder";
            this.tsmExploreFolder.Click += new System.EventHandler(this.tsmExploreFolder_Click);
            // 
            // tsmNextInFolder
            // 
            this.tsmNextInFolder.Enabled = false;
            this.tsmNextInFolder.Name = "tsmNextInFolder";
            this.tsmNextInFolder.ShortcutKeyDisplayString = "Up";
            this.tsmNextInFolder.Size = new System.Drawing.Size(285, 22);
            this.tsmNextInFolder.Text = "Next in This Folder";
            this.tsmNextInFolder.Click += new System.EventHandler(this.tsmNextInFolder_Click);
            // 
            // tsmPreviousInFolder
            // 
            this.tsmPreviousInFolder.Enabled = false;
            this.tsmPreviousInFolder.Name = "tsmPreviousInFolder";
            this.tsmPreviousInFolder.ShortcutKeyDisplayString = "Down";
            this.tsmPreviousInFolder.Size = new System.Drawing.Size(285, 22);
            this.tsmPreviousInFolder.Text = "Previous In This Folder";
            this.tsmPreviousInFolder.Click += new System.EventHandler(this.tsmPreviousInFolder_Click);
            // 
            // tsmExitExploreFolder
            // 
            this.tsmExitExploreFolder.Enabled = false;
            this.tsmExitExploreFolder.Name = "tsmExitExploreFolder";
            this.tsmExitExploreFolder.ShortcutKeyDisplayString = "Right, Left";
            this.tsmExitExploreFolder.Size = new System.Drawing.Size(285, 22);
            this.tsmExitExploreFolder.Text = "Back to Photos";
            this.tsmExitExploreFolder.Click += new System.EventHandler(this.tsmExitExploreFolder_Click);
            // 
            // tsmSeparator3
            // 
            this.tsmSeparator3.Name = "tsmSeparator3";
            this.tsmSeparator3.Size = new System.Drawing.Size(282, 6);
            // 
            // tsmSettings
            // 
            this.tsmSettings.Name = "tsmSettings";
            this.tsmSettings.ShortcutKeyDisplayString = "F2";
            this.tsmSettings.Size = new System.Drawing.Size(285, 22);
            this.tsmSettings.Text = "Settings...";
            this.tsmSettings.Click += new System.EventHandler(this.tsmSettings_Click);
            // 
            // tsmSeparator4
            // 
            this.tsmSeparator4.Name = "tsmSeparator4";
            this.tsmSeparator4.Size = new System.Drawing.Size(282, 6);
            // 
            // tsmFullscreen
            // 
            this.tsmFullscreen.Name = "tsmFullscreen";
            this.tsmFullscreen.ShortcutKeyDisplayString = "F11";
            this.tsmFullscreen.Size = new System.Drawing.Size(285, 22);
            this.tsmFullscreen.Text = "Toggle Fullscreen";
            this.tsmFullscreen.Click += new System.EventHandler(this.tsmFullscreen_Click);
            // 
            // tsmShowMetadata
            // 
            this.tsmShowMetadata.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmMetadataShowHide,
            this.tsmMetaDataChangeFont,
            this.tsmMetaDataChangeColor,
            this.tsmMetadataMoveOnScreen});
            this.tsmShowMetadata.Name = "tsmShowMetadata";
            this.tsmShowMetadata.ShortcutKeyDisplayString = "";
            this.tsmShowMetadata.Size = new System.Drawing.Size(285, 22);
            this.tsmShowMetadata.Text = "Metadata";
            // 
            // tsmMetadataShowHide
            // 
            this.tsmMetadataShowHide.Name = "tsmMetadataShowHide";
            this.tsmMetadataShowHide.ShortcutKeyDisplayString = "Backspace";
            this.tsmMetadataShowHide.Size = new System.Drawing.Size(279, 22);
            this.tsmMetadataShowHide.Text = "Show / Hide";
            this.tsmMetadataShowHide.Click += new System.EventHandler(this.tsmMetadataShowHide_Click);
            // 
            // tsmMetaDataChangeFont
            // 
            this.tsmMetaDataChangeFont.Name = "tsmMetaDataChangeFont";
            this.tsmMetaDataChangeFont.ShortcutKeyDisplayString = "F3";
            this.tsmMetaDataChangeFont.Size = new System.Drawing.Size(279, 22);
            this.tsmMetaDataChangeFont.Text = "Change Font...";
            this.tsmMetaDataChangeFont.Click += new System.EventHandler(this.tsmMetaDataChangeFont_Click);
            // 
            // tsmMetaDataChangeColor
            // 
            this.tsmMetaDataChangeColor.Enabled = false;
            this.tsmMetaDataChangeColor.Name = "tsmMetaDataChangeColor";
            this.tsmMetaDataChangeColor.ShortcutKeyDisplayString = "F4";
            this.tsmMetaDataChangeColor.Size = new System.Drawing.Size(279, 22);
            this.tsmMetaDataChangeColor.Text = "Change Color...";
            this.tsmMetaDataChangeColor.Click += new System.EventHandler(this.tsmMetaDataChangeColor_Click);
            // 
            // tsmMetadataMoveOnScreen
            // 
            this.tsmMetadataMoveOnScreen.Enabled = false;
            this.tsmMetadataMoveOnScreen.Name = "tsmMetadataMoveOnScreen";
            this.tsmMetadataMoveOnScreen.ShortcutKeyDisplayString = "Control+ArrowKeys";
            this.tsmMetadataMoveOnScreen.Size = new System.Drawing.Size(279, 22);
            this.tsmMetadataMoveOnScreen.Text = "Move on Screen...";
            this.tsmMetadataMoveOnScreen.Click += new System.EventHandler(this.tsmMetadataMoveOnScreen_Click);
            // 
            // tsmSeparator5
            // 
            this.tsmSeparator5.Name = "tsmSeparator5";
            this.tsmSeparator5.Size = new System.Drawing.Size(282, 6);
            // 
            // tsmFileDropDown
            // 
            this.tsmFileDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmBlacklist,
            this.toolStripSeparator1,
            this.tsmRating,
            this.tsmEditComments,
            this.tsmEditTags,
            this.toolStripSeparator2,
            this.tsmOpenFileExplorer});
            this.tsmFileDropDown.Name = "tsmFileDropDown";
            this.tsmFileDropDown.Size = new System.Drawing.Size(285, 22);
            this.tsmFileDropDown.Text = "For This File";
            // 
            // tsmBlacklist
            // 
            this.tsmBlacklist.Name = "tsmBlacklist";
            this.tsmBlacklist.ShortcutKeyDisplayString = "Del";
            this.tsmBlacklist.Size = new System.Drawing.Size(272, 22);
            this.tsmBlacklist.Text = "Never Display This Photo...";
            this.tsmBlacklist.Click += new System.EventHandler(this.tsmBlacklist_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(269, 6);
            // 
            // tsmRating
            // 
            this.tsmRating.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmOneStar,
            this.tmsTwoStar,
            this.tsmThreeStar,
            this.tsmFourStar,
            this.tsmFiveStar,
            this.tsmSeparator10,
            this.tsmRemoveRating});
            this.tsmRating.Enabled = false;
            this.tsmRating.Name = "tsmRating";
            this.tsmRating.Size = new System.Drawing.Size(272, 22);
            this.tsmRating.Text = "Rating";
            // 
            // tsmOneStar
            // 
            this.tsmOneStar.Name = "tsmOneStar";
            this.tsmOneStar.Size = new System.Drawing.Size(154, 22);
            this.tsmOneStar.Text = "One Star";
            // 
            // tmsTwoStar
            // 
            this.tmsTwoStar.Name = "tmsTwoStar";
            this.tmsTwoStar.Size = new System.Drawing.Size(154, 22);
            this.tmsTwoStar.Text = "Two Star";
            // 
            // tsmThreeStar
            // 
            this.tsmThreeStar.Name = "tsmThreeStar";
            this.tsmThreeStar.Size = new System.Drawing.Size(154, 22);
            this.tsmThreeStar.Text = "Three Star";
            // 
            // tsmFourStar
            // 
            this.tsmFourStar.Name = "tsmFourStar";
            this.tsmFourStar.Size = new System.Drawing.Size(154, 22);
            this.tsmFourStar.Text = "Four Star";
            // 
            // tsmFiveStar
            // 
            this.tsmFiveStar.Name = "tsmFiveStar";
            this.tsmFiveStar.Size = new System.Drawing.Size(154, 22);
            this.tsmFiveStar.Text = "Five Star";
            // 
            // tsmSeparator10
            // 
            this.tsmSeparator10.Name = "tsmSeparator10";
            this.tsmSeparator10.Size = new System.Drawing.Size(151, 6);
            // 
            // tsmRemoveRating
            // 
            this.tsmRemoveRating.Name = "tsmRemoveRating";
            this.tsmRemoveRating.Size = new System.Drawing.Size(154, 22);
            this.tsmRemoveRating.Text = "Remove Rating";
            // 
            // tsmEditComments
            // 
            this.tsmEditComments.Enabled = false;
            this.tsmEditComments.Name = "tsmEditComments";
            this.tsmEditComments.Size = new System.Drawing.Size(272, 22);
            this.tsmEditComments.Text = "Edit Comments...";
            // 
            // tsmEditTags
            // 
            this.tsmEditTags.Enabled = false;
            this.tsmEditTags.Name = "tsmEditTags";
            this.tsmEditTags.ShowShortcutKeys = false;
            this.tsmEditTags.Size = new System.Drawing.Size(272, 22);
            this.tsmEditTags.Text = "Edit Tags...";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(269, 6);
            // 
            // tsmOpenFileExplorer
            // 
            this.tsmOpenFileExplorer.Name = "tsmOpenFileExplorer";
            this.tsmOpenFileExplorer.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.tsmOpenFileExplorer.Size = new System.Drawing.Size(272, 22);
            this.tsmOpenFileExplorer.Text = "Open File Explorer To This File";
            this.tsmOpenFileExplorer.Click += new System.EventHandler(this.tsmOpenFileExplorer_Click);
            // 
            // tsmSeparator6
            // 
            this.tsmSeparator6.Name = "tsmSeparator6";
            this.tsmSeparator6.Size = new System.Drawing.Size(282, 6);
            // 
            // tsmExit
            // 
            this.tsmExit.Name = "tsmExit";
            this.tsmExit.ShortcutKeyDisplayString = "Esc";
            this.tsmExit.Size = new System.Drawing.Size(285, 22);
            this.tsmExit.Text = "Exit Screen Saver";
            this.tsmExit.Click += new System.EventHandler(this.tsmExit_Click);
            // 
            // FullScreenForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(1432, 664);
            this.Controls.Add(this.pbMain);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FullScreenForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "-- Title Not Set From Code --";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.FullScreenForm_Activated);
            this.Deactivate += new System.EventHandler(this.FullScreenForm_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FullScreenForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FullScreenForm_FormClosed);
            this.Load += new System.EventHandler(this.FullScreenForm_Load);
            this.SizeChanged += new System.EventHandler(this.FullScreenForm_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FullScreenForm_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FullScreenForm_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FullScreenForm_KeyUp);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.FullScreenForm_PreviewKeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.pbMain)).EndInit();
            this.contextMenuMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbMain;
        private System.Windows.Forms.ContextMenuStrip contextMenuMain;
        private System.Windows.Forms.ToolStripMenuItem tsmHelpAbout;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsmPauseResume;
        private System.Windows.Forms.ToolStripMenuItem tsmNextPhoto;
        private System.Windows.Forms.ToolStripMenuItem tsmPreviousPhoto;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator2;
        private System.Windows.Forms.ToolStripMenuItem tsmExploreFolder;
        private System.Windows.Forms.ToolStripMenuItem tsmNextInFolder;
        private System.Windows.Forms.ToolStripMenuItem tsmPreviousInFolder;
        private System.Windows.Forms.ToolStripMenuItem tsmExitExploreFolder;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator3;
        private System.Windows.Forms.ToolStripMenuItem tsmSettings;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator4;
        private System.Windows.Forms.ToolStripMenuItem tsmFullscreen;
        private System.Windows.Forms.ToolStripMenuItem tsmShowMetadata;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator5;
        private System.Windows.Forms.ToolStripMenuItem tsmExit;
        private System.Windows.Forms.ToolStripMenuItem tsmFileDropDown;
        private System.Windows.Forms.ToolStripMenuItem tsmBlacklist;
        private System.Windows.Forms.ToolStripMenuItem tsmOpenFileExplorer;
        private System.Windows.Forms.ToolStripMenuItem tsmRating;
        private System.Windows.Forms.ToolStripMenuItem tsmOneStar;
        private System.Windows.Forms.ToolStripMenuItem tmsTwoStar;
        private System.Windows.Forms.ToolStripMenuItem tsmThreeStar;
        private System.Windows.Forms.ToolStripMenuItem tsmFourStar;
        private System.Windows.Forms.ToolStripMenuItem tsmFiveStar;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator10;
        private System.Windows.Forms.ToolStripMenuItem tsmRemoveRating;
        private System.Windows.Forms.ToolStripSeparator tsmSeparator6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsmEditComments;
        private System.Windows.Forms.ToolStripMenuItem tsmEditTags;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem tsmMetadataShowHide;
        private System.Windows.Forms.ToolStripMenuItem tsmMetaDataChangeFont;
        private System.Windows.Forms.ToolStripMenuItem tsmMetaDataChangeColor;
        private System.Windows.Forms.ToolStripMenuItem tsmMetadataMoveOnScreen;
    }
}

