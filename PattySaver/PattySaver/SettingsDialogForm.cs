using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using ScotSoft;
using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;
using ScotSoft.PattySaver.LaunchManager;


namespace ScotSoft.PattySaver
{
    public partial class Settings : Form
    {
        #region Data

        bool fDebugOutput = true;
        bool fDebugAtTraceLevel = true;
        bool fDebugTrace = false; // do not modify here, it's recalculated at method level
        ScrollingTextWindow debugOutputWindow = null;

        private bool fConstructorHasCompleted = false;
        private bool fConstructorIsRunning = false;
        private bool fLoadHasCompleted = false;
        private bool fLoadIsRunning = false;

        private bool OpenedFromControlPanel = false;
        private bool OpenedFromNakedCommandArg = false;
        private bool OpenedFromScreenSaverForm = false;
        private bool OpenedFromOtherForm = false;
        Form CallingForm;                                           // The form that launched the Settings form, if any

        private IntPtr ControlPanelPassedhWnd = new IntPtr(0);      // handle to window that we want to be a child of
        bool fRebuildNeeded = false;
        bool InitialValueOfShuffle = false;
        bool InitialValueOfRecursion = false;
        bool InitialValueOfShowMetadata = false;
        bool InitialValueOfUseOnlyChecked = false;

        ScreenSaverForm myParentFullScreenForm;

        private Timer delayedCalcTimer;                             // Timer we use to delay calculation of warning label status

        FolderBrowserDialog folderBrowserDlg = new FolderBrowserDialog();       // The Browse Folder object we show when you add Directories
        private string _lastSelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures); // the path last selected in BrowseFolder, so we can open to that

        private object _foldersLock = new object();                  // lock this when adding or removing items from Folders list

        #endregion Data


        #region Constructors

        /// <summary>
        /// Parameterless constructor, called when Settings dialog is opened from the "Configure" shell command.
        /// </summary>
        public Settings()
        {
            fConstructorIsRunning = true;
            InitializeComponent();
            OpenedFromNakedCommandArg = true;
            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
        }

        /// <summary>
        /// Constructor called when Settings dialog opened from within Control Panel.
        /// </summary>
        /// <param name="hwnd">Handle to Window to make parent of Settings window.</param>
        public Settings(IntPtr hwnd)
        {
            fConstructorIsRunning = true;
            InitializeComponent();
            OpenedFromControlPanel = true;
            ControlPanelPassedhWnd = hwnd;
            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
        }

        /// <summary>
        /// Constructor called when Settings dialog is opened from one of our Forms.
        /// </summary>
        /// <param name="LaunchedFromScreenSaverForm">True if Settings opened from the FullScreen form.</param>
        /// <param name="callingForm">Form that opened the Settings dialog.</param>
        public Settings(bool LaunchedFromScreenSaverForm, Form callingForm)
        {
            fConstructorIsRunning = true;
            if (LaunchedFromScreenSaverForm && (callingForm == null))
            {
                throw new ArgumentNullException("If LaunchedFromFullScreenForm, callingForm cannot be null.");
            }
            InitializeComponent();
            CallingForm = callingForm;
            OpenedFromScreenSaverForm = LaunchedFromScreenSaverForm;
            OpenedFromOtherForm = !LaunchedFromScreenSaverForm;
            if (OpenedFromScreenSaverForm && callingForm.WindowState == FormWindowState.Maximized) this.TopMost = true;
            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
        }

        #endregion Constructors

        #region Form Events

        private void Settings_Load(object sender, EventArgs e)
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            // set flags
            fLoadIsRunning = true;

            // Trace
            Logging.LogLineIf(fDebugTrace, "Settings_Load(): entered.");
            
            // set title bar
            Text = ProductName + " Settings";

            // Setting the owner of this Form to the FullScreen Form allows us to call its methods
            if (OpenedFromScreenSaverForm) myParentFullScreenForm = (ScreenSaverForm)this.Owner;

            // If Settings dialog was opened by a form, center it on the calling form
            if (CallingForm != null)
            {
                // TODO: this does not center when calling form is smaller than Settings form. Use absolute values?
                Point p = new Point(0, 0);
                if (CallingForm.Height > this.Height)
                {
                    p.Y = (CallingForm.Height / 2) - (this.Height / 2);
                }
                else
                {
                    p.Y = (this.Height / 2) - (CallingForm.Height / 2);
                }

                if (CallingForm.Width > this.Width)
                {
                    p.X = (CallingForm.Width / 2) - (this.Width / 2);
                }
                else
                {
                    p.X = (this.Width / 2) - (CallingForm.Width / 2);
                }
                this.Location = p;
            }

            // If not opened from one of our forms, we need to load config data from disk.
            // If we are opened from one of our forms, we can assume that ConfigSettings has already been initialized
            if (!OpenedFromScreenSaverForm && !OpenedFromOtherForm)
            {
                // if this fails to read from disk, it sets default values
                SettingsInfo.InitializeAndLoadConfigSettingsFromStorage();
            }

            // Now fill out the dialog values from the ConfigData
            FillOutDialogFromConfigSettings();

            // Light up the warning if necessary
            TestForPictureFolderWarning();

            fLoadIsRunning = false;
            fLoadHasCompleted = true;

            Logging.LogLineIf(fDebugTrace, "Settings_Load(): entered.");
        }

        private void FillOutDialogFromConfigSettings()
        {
            // Using the ConfigSettings, fill out the dialog. First, the Folders list. 
            // "Checked" is stored in KeyValuePair.Key, path in KeyValuePair.Value.
            int currIndex = 0;
            lock (_foldersLock) // don't let anybody else mess with the list
            {
                foreach (KeyValuePair<bool, string> kvp in SettingsInfo.DirectoriesList)
                {
                    Folders.Items.Add(kvp.Value);
                    Folders.SetItemChecked(currIndex, kvp.Key);
                    currIndex++;
                }
            }

            // Set the appropriate Shuffle check box
            Shuffle.Checked = SettingsInfo.ShuffleMode;
            InitialValueOfShuffle = Shuffle.Checked;

            // Set the appropriate Show Metadata check box
            ShowMetaData.Checked = SettingsInfo.ShowMetadata;
            InitialValueOfShowMetadata = ShowMetaData.Checked;

            // Set the appropriate UseOnlyChecked check box
            UseOnlyChecked.Checked = SettingsInfo.UseCheckedFoldersOnly;
            InitialValueOfUseOnlyChecked = UseOnlyChecked.Checked;

            // Set the appropriate Recursion check box
            Recursion.Checked = SettingsInfo.UseRecursion;
            InitialValueOfRecursion = Recursion.Checked;

            // Load the SpeedVal dropdown with choices
            for (int i = 3; i < 31; i++)
            {
                SlideshowInterval.Items.Add(i.ToString());
            }

            // Set the Speed Val combo's edit control
            SlideshowInterval.Text = SettingsInfo.SlideshowIntervalInSecs.ToString();

            // Set the path for the Add Folder dialog
            _lastSelectedPath = SettingsInfo.SettingsDialogAddFolderLastSelectedPath;

            // Format the font dialog, then steal that info for desc label
            if (OpenedFromScreenSaverForm)
            {
                FormatFontDialogFromDataFont();
                UpdateFontDescriptionFromFontDlg();
            }
            else
            {
                // TODO: hack. Figure out how to make it work when not opened from ScreenSaverForm
                btnChooseFont.Enabled = false;
            }
        }

        private void Settings_FormClosed(object sender, FormClosedEventArgs e)
        {
            // If the Settings dialog was opened with no owning form, quit the app
            if (OpenedFromControlPanel || OpenedFromNakedCommandArg)
            {
                Application.Exit();
            }
        }


        #endregion Form Events


        #region Control and Timer Events

        // Add Folder button
        private void Add_Click(object sender, EventArgs e)
        {
            folderBrowserDlg.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowserDlg.SelectedPath = _lastSelectedPath;
            folderBrowserDlg.ShowNewFolderButton = false;
            folderBrowserDlg.Description = "Choose a folder that the screensaver should search for graphics files.";

            if (folderBrowserDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)  // user clicked OK in dialog
            {
                if (folderBrowserDlg.SelectedPath != String.Empty)
                {
                    lock (_foldersLock) // as good practice, lock the CheckedListBox Folders when adding or removing items
                    {
                        if (!Folders.Items.Contains(folderBrowserDlg.SelectedPath))
                            Folders.Items.Add(folderBrowserDlg.SelectedPath);
                        fRebuildNeeded = true;
                    }
                    // remember the path they selected for next invocation of dialog
                    _lastSelectedPath = folderBrowserDlg.SelectedPath;
                }
            }
            // See if we need to light up the caution sign
            TestForPictureFolderWarning();
        }

        // Remove Folder button
        private void Remove_Click(object sender, EventArgs e)
        {
            if (Folders.SelectedIndex == -1)  //no selection
            {
                // do nothing
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to remove the selected Folder?", ProductName + " Remove Folder From List", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
                {
                    lock (_foldersLock)
                    {
                        Folders.Items.Remove(Folders.SelectedItem);
                        fRebuildNeeded = true;
                    }
                }
            }

            // check to see if we need to highlight the warning
            TestForPictureFolderWarning();
        }

        // Choose Metadata button
        private void ChooseMetadata_Click(object sender, EventArgs e)
        {
            debugOutputWindow = new ScrollingTextWindow(this);
            debugOutputWindow.CopyTextToClipboardOnClose = true;
            debugOutputWindow.ShowDisplay();

        }

        // Check Item in Folders List
        private void Folders_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (fConstructorHasCompleted && fLoadHasCompleted)
            {
                // TODO: make config settings non-static, and generate a clone. Compare the two when dialog closes.

                fRebuildNeeded = true; // super conservative, but tracking if all checks in list are still the same when Save is clicked would be very hard...

                if (UseOnlyChecked.Checked && (Folders.CheckedItems.Count < 2)) // If only one item, checking/unchecking can change message
                {
                    TestForPictureFolderWarning(true);
                }
            }
        }

        // Use Only Checked Folders checkbox
        private void UseOnlyChecked_CheckedChanged(object sender, EventArgs e)
        {
            TestForPictureFolderWarning();
        }

        // Choose Font button
        private void btnChooseFont_Click(object sender, EventArgs e)
        {
            // Configure Font Dialog
            myParentFullScreenForm.fontdlg.ShowColor = true;
            myParentFullScreenForm.fontdlg.ShowEffects = true;
            myParentFullScreenForm.fontdlg.ShowApply = true;
            myParentFullScreenForm.fontdlg.ScriptsOnly = true;
            myParentFullScreenForm.fontdlg.FontMustExist = true;
            FormatFontDialogFromDataFont();

            // Hide Settings Dialog
            this.Hide();

            // Show the Font Dialog
            DialogResult dr = myParentFullScreenForm.fontdlg.ShowDialog(myParentFullScreenForm);
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                myParentFullScreenForm.metaFontData.SetPropertiesFromFontDlg(myParentFullScreenForm.fontdlg);
                myParentFullScreenForm.refto_pbMainPhoto.Invalidate();
            }

            // Update the font description text
            UpdateFontDescriptionFromFontDlg();

            // Show the Settings Dialog
            this.Show();
        }

        // Choose Color button
        private void btnChooseFontColor_Click(object sender, EventArgs e)
        {
            // Configure the color dialog
            myParentFullScreenForm.colordlg.AllowFullOpen = true;
            myParentFullScreenForm.colordlg.AnyColor = true;
            myParentFullScreenForm.colordlg.FullOpen = true;

            // Hide Settings Dialog
            this.Hide();

            // Show the Color Dialog
            DialogResult dr = myParentFullScreenForm.colordlg.ShowDialog(myParentFullScreenForm);
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                myParentFullScreenForm.metaFontData.SetColorFromColorDlg(myParentFullScreenForm.colordlg);
                myParentFullScreenForm.refto_pbMainPhoto.Invalidate();
            }

            // Show the Settings Dialog
            this.Show();
        }

        // Tick Event for delayedCalcTimer
        private void delayedCalcTimerTick(object obj, EventArgs e)
        {
            delayedCalcTimer.Stop();
            delayedCalcTimer.Tick -= new EventHandler(delayedCalcTimerTick);     // unbind the timer.tick event from the method
            delayedCalcTimer.Dispose();                               // dispose of the timer object

            TestForPictureFolderWarning();
        }

        // Cancel button
        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        // Save button
        private void Save_Click(object sender, EventArgs e)
        {
            // Save the data to disk
            UpdateConfigAndSave();

            // Update Any config settings in memory that may need it

            // Exit the dialog. Set Dialog Result = OK if config data was written to disk, whether it 
            // changed or not. Set Dialog Result = Retry if a rebuild of the MainFiles is in order.
            if ((!fRebuildNeeded) && (InitialValueOfShuffle == Shuffle.Checked &&
                    InitialValueOfUseOnlyChecked == UseOnlyChecked.Checked &&
                    InitialValueOfRecursion == Recursion.Checked))
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            else
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Retry;
            }
        }

        #endregion Control and Timer Events


        #region Methods Called by Events

        private void UpdateConfigAndSave()
        {
            // Save away the list of Folders
            lock (_foldersLock)
            {
                SettingsInfo.DirectoriesList.Clear();

                foreach (var item in Folders.Items)
                {
                    bool ckd = false;

                    // wow this is ugly, but I can't figure out the type, and I want to preserve the order, so
                    if (Folders.CheckedItems.Contains(item))
                    {
                        ckd = true;
                    }

                    KeyValuePair<bool, string> kvp = new KeyValuePair<bool, string>(ckd, item.ToString());
                    SettingsInfo.DirectoriesList.Add(kvp);
                }
            }

            // Validate ChooseSpeedVal; if it's bad, don't change the existing value.
            string str = SlideshowInterval.Text;
            int val = 0;
            if ((String.IsNullOrEmpty(str)) || (String.IsNullOrEmpty(str)) || int.TryParse(str, out val))
            {
                SettingsInfo.SlideshowIntervalInSecs = val;
            }

            // Save away booleans
            SettingsInfo.ShuffleMode = Shuffle.Checked;
            SettingsInfo.ShowMetadata = ShowMetaData.Checked;
            SettingsInfo.UseCheckedFoldersOnly = UseOnlyChecked.Checked;
            SettingsInfo.UseRecursion = Recursion.Checked;

            // Save away the last path
            SettingsInfo.SettingsDialogAddFolderLastSelectedPath = _lastSelectedPath;

            // Save away the WhichMetada info block.  Or should we write this when that form closes?
            // MetaData not yet implemented. Code already works to save and read it, though.

            // Save to Disk
            SettingsInfo.SaveConfigSettingsToStorage();

            // Close the dialog
            this.Close();
        }

        private void UpdateFontDescriptionFromFontDlg()
        {
            string desc = myParentFullScreenForm.fontdlg.Font.Name + ", " +
                myParentFullScreenForm.fontdlg.Font.Size + ", " +
                myParentFullScreenForm.fontdlg.Font.Style + ", " +
                myParentFullScreenForm.fontdlg.Color.Name;
            lblFontDescription.Text = desc;
        }

        private void FormatFontDialogFromDataFont()
        {
            if (myParentFullScreenForm.fdFont != null)
            {
                myParentFullScreenForm.fdFont.Dispose();
                myParentFullScreenForm.fdFont = null;
            }
            myParentFullScreenForm.fdFont = new Font(myParentFullScreenForm.metaFontData.FontName,
                myParentFullScreenForm.metaFontData.FontSize, myParentFullScreenForm.metaFontData.FontStyle);
            myParentFullScreenForm.fontdlg.Font = myParentFullScreenForm.fdFont;
            myParentFullScreenForm.fontdlg.Color = Color.FromName(myParentFullScreenForm.metaFontData.FontColorName);
        }

        private void TestForPictureFolderWarning()
        {
            TestForPictureFolderWarning(false);
        }

        private void TestForPictureFolderWarning(bool fDelayCalculation)
        {
            // This method decides whether the "Hey, if you don't have any folders listed (or checked),
            // we're going to use your Pictures Folder instead" label gets turned red or not.
            // It also decides whether to say "listed" or "Checked".

            // In the case where the user has clicked a checkbox in the Folders list, we are only alerted BEFORE
            // the checked value is set, so our code cannot correctly test with "current" conditions. For us to 
            // properly calculate the state of the PictureWarning label, we need to delay the calculation. So we 
            // set ourselves a timer, and have that timer call our calculation once the checkbox value is actually set.
            if (fDelayCalculation)
            {
                // caller has asked for delayed execution, so
                delayedCalcTimer = new Timer();                           // create a new timer
                delayedCalcTimer.Tick += new EventHandler(delayedCalcTimerTick);     // bind the timer.tick event to a handler method
                delayedCalcTimer.Interval = 1250;                         // set delay for two seconds
                delayedCalcTimer.Start();
            }
            else
            {
                SetPictureWarningLabelText();
                SetPictureWarningLabelColor();

                if ((UseOnlyChecked.Checked && Folders.CheckedItems.Count == 0) || (!UseOnlyChecked.Checked && Folders.Items.Count == 0))
                {
                    HighlightPictureWarningLabel(true);
                }
                else
                {
                    HighlightPictureWarningLabel(false);
                }
            }
        }

        private void SetPictureWarningLabelColor()
        {
            if ((Folders.Items.Count == 0) || ((Folders.CheckedItems.Count == 0) && (UseOnlyChecked.Checked)))
            {
                HighlightPictureWarningLabel(true);
            }
            else
            {
                HighlightPictureWarningLabel(false);
            }
        }

        private void HighlightPictureWarningLabel(bool fHighlightNeeded)
        {
            if (fHighlightNeeded)
            {
                this.PictureFolderWarning.ForeColor = Color.Firebrick;
            }
            else
            {
                this.PictureFolderWarning.ForeColor = SystemColors.ControlText;
            }
        }

        private void SetPictureWarningLabelText()
        {
            if (UseOnlyChecked.Checked)
            {
                this.PictureFolderWarning.Text = "If no Folders are checked, the Pictures Folder will be used.";
            }
            else
            {
                this.PictureFolderWarning.Text = "If no folders are listed, the Pictures folder will be used.";
            }
        }

        #endregion Methods Called by Events

    }
}
