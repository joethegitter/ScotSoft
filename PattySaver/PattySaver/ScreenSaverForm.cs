using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Text;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;
using ScotSoft.PattySaver.LaunchManager;

namespace ScotSoft.PattySaver
{
    public partial class ScreenSaverForm : Form
    {

        #region Fields

        // Public Fields accessed by other forms
        public Settings settingsForm;                           // the Settings Form object, accessable by other forms; null when not in DoSettings() scope
        public PictureBox refto_pbMainPhoto;                    // this public reference to our pb allows other forms to call Invalidate on it
        public FontDialog fontdlg;                              // the Font Dialog we'll use from FullScreen and Settings forms
        public Font fdFont;                                     // the Font we use to format the font dialog
        public ColorDialog colordlg;                            // The Color Dialog we'll use from FullScreen and Settings forms
        public FontData metaFontData;                           // An object we use to hold the data we use to build our metadata font
        public ScrollingTextWindow debugOutputWindow = null;    // The debug output window, null if not in use

        // Objects which provide access to Files
        private MainFileInfoSource MainFiles;                   // Class we defined, object holding Main fileinfo list, with methods for manipulation thereof.
        private ExploreThisFolderFileInfoSource ETFFiles;       // Class we defined, object holding the current ETF fileinfo list. Null if we're not in that mode.

        // Form-Wide Modes and States
        private bool fFormConstructorIsRunning = false;         // if true, the constructor code is running; lets us defer/abort things like Resize, etc
        private bool fFormConstructorHasCompleted = false;      // the Constructor has finished
        private bool fFormLoadIsRunning = false;                // the FormLoad event handlers is running
        private bool fFormLoadHasCompleted = false;             // the FormLoad code has finished
        private bool fFormClosingIsRunning = false;             // the FormClosing event handler has been called and is in progress
        private bool fFormClosingHasCompleted = true;           // the formClosing code has finished
        private bool fShowingEmbeddedFileImage;                 // we are showing an embedded resource, not a photo from file.                    
        private bool fInETFMode = false;                        // we are in ExploreThisFolder mode.
        private bool fShiftDown = false;                        // the ShiftKey is down.  Probably should move to some kind of Keyboard State object...
        private bool fCtrlDown = false;                         // the ControlKey is down. 
        private bool fWaitingForFileToLoad = false;             // we've started loading a file, but the loadcompleted event has not fired, and file is not drawn yet.
        private bool fScreenSaverWindowStyle = false;           // our window is maximized, topmost and not-resizable (or the opposite if false)
        private bool fWindowStyleIsChanging = false;            // tells us that we're in the middle of changing window styles
        private bool fWasInSlideshowModeWhenMenuOpened = false; // lets us pause and resume Slideshow Mode when menu opens and closes.
        private bool fWasInSlideshowModeWhenETFStarted = false; // lets us pause and resume Slideshow Mode entering and exiting ETF mode.
        private bool fWasInSlideshowModeWhenDeactivated = false;// lets us pause and resume Slideshow Mode when window is deactivate/activated
        private bool fShowingDialog = false;                    // lets us know that we're losing activation because we're showing dialog

        // Slideshow object
        public Slideshow ourSlideshow;                          // the object which controls our slideshow mechanics

        // These values are used by our metadata/ETF text drawing code
        private string currentExploreFolderData = "";                                           // data
        private string currentImageMetadata = "";                                               // data
        private float allTextLeft = 20.0F;                                                      // left location of all text (metadata, etf)
        private float ETFdataTop = 20.0F;                                                       // top location of etf text
        private float metadataTop = 20.0F;                                                      // initial top location of metadta text
        private float ETFtoMetadataOffsetBase = 40.0F;                                          // vertical spacing between etf text and metadata text
        public Font metaFont = new Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);      // *default* font used for metadata text
        public Brush metaBrush = new SolidBrush(Color.Aqua);                                    // *default* font color used for metadata text
        private Font etfFont = new Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);      // *default* font used for etf text; syncs to metadata eventually
        private Brush etfBrush = new SolidBrush(Color.Yellow);                                  // font color always used by etf text
        private Color shadowBrushColor = Color.Black;                                           // color always used by text "shadowing"

        // debug
        long dbgTotalMemoryHighWaterMark = 0;

        #endregion Fields


        #region Constructors

        /// <summary>
        /// Constructor for the ScreenSaverForm, which is the primary display for our screen saver.
        /// </summary>
        /// <param name="Bounds">Size of the rectangle in which to draw form.</param>
        public ScreenSaverForm(Rectangle Bounds)
        {
            // set state flags
            fFormConstructorIsRunning = true;

            // This method is auto-generated by the VS Designer; it adds all of
            // the controls it knows about to the Form, and then binds event
            // handlers to those controls. Ignore, but do not remove.
            this.InitializeComponent();

            SettingsInfo.InitializeAndLoadConfigSettingsFromStorage();  // Load configutation data from disk
            metaFontData = new FontData();                              // Build a default fontData block
            SetFontDataFromConfigData();                                // Update the fontData with saved data

            this.Bounds = Bounds;           // Sets the size of our screen saver window to the passed in value

            // set state flags
            fFormConstructorIsRunning = false;
            fFormConstructorHasCompleted = true;
        }

        #endregion Constructors


        #region Public Members

        // see the following files:
        // FontData.cs
        // Slideshow.cs

        #endregion Public Members


        #region Form Events

        /// <summary>
        /// Code that runs after the Form is created but before the Form is Displayed.
        /// </summary>
        private void ScreenSaverForm_Load(object sender, EventArgs e)
        {
            // Scot, this runs immediately after the constructor (well, almost. There are events which can come in 
            // between constructor and Load - resize events, etc). Here we prep and initialize everything before we show the Form.
            // After that, it's all a matter of reacting to events (clicks, keypresses, timers, etc).

            // set debug output controls
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Load(): entered.");

            // set state flags
            fFormLoadIsRunning = true;

            // create the slideshow object, set its properties
            ourSlideshow = new Slideshow(this);
            ourSlideshow.IntervalInMilliSeconds = SettingsInfo.SlideshowIntervalInSecs * 1000;
            ourSlideshow.DeferralTimeWindowInMilliseconds = 1500;
            ourSlideshow.DeferralIntervalInMilliseconds = ourSlideshow.IntervalInMilliSeconds;

            // set the style of our window (maximized, topmost, etc) appropriately
            Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_Load(): entering or exiting ScreenSaverWindowStyle, as appropriate.");

            if (Modes.fOpenInScreenSaverMode)
            {
                EnterScreenSaverWindowStyle();
            }
            else
            {
                ExitScreenSaverWindowStyle(true);       // restore us to "normal" using the size/position data from Settings
            }

            // point a public field to our picturebox, so that through this field the pb can be addressed by other forms (settings, for example)
            refto_pbMainPhoto = pbMain;

            // create a public font dialog that we can access from this and other forms
            fontdlg = new FontDialog();

            // set the base font dialog properties
            fontdlg.ShowColor = true;
            fontdlg.ShowEffects = true;
            fontdlg.ShowApply = true;
            fontdlg.FontMustExist = true;

            // set the specific font dialog properties to match the current font data in metaFontData
            FormatFontDialogFromFontData(metaFontData);

            // bind the event raised by the Font Dialog Apply Button to our handler
            fontdlg.Apply += new System.EventHandler(FontDialog_Apply);

            // Create the Color Picker Dialog - removed for bugs.  Will restore later.
            // colordlg = new ColorDialog();

            // bind the MouseWheel event (can't be done from VS Designer UI)
            MouseWheel += ScreenSaverForm_MouseWheel;

            // add the context menu to our form
            this.ContextMenuStrip = contextMenuMain;

            // set title of window
            this.Text = ProductName;

            // show a picture immediately;
            // create the MainFiles object - this leaves the index pointed at -1
            Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_Load(): creating MainFiles, should kick off disk scan.");
            MainFiles = new MainFileInfoSource(
                (List<DirectoryInfo>)SettingsInfo.GetListOfDirectoryInfo(), 
                SettingsInfo.GetBlacklistedFullFilenames(), 
                SettingsInfo.UseRecursion, 
                SettingsInfo.ShuffleMode);

            // advance one file; this leaves the index pointed at zero
            Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_Load(): calling DoPreviousOrNext(false).");
            DoPreviousOrNext(false);

            // if we opened in screen saver mode, start the slideshow
            if (Modes.fOpenInScreenSaverMode) ourSlideshow.Start();

            // set state flags
            fFormLoadIsRunning = false;
            fFormLoadHasCompleted = true;

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Load(): exiting.");

        }

        /// <summary>
        /// Code that runs when the Form loses focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenSaverForm_Deactivate(object sender, EventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Deactivate(): entered.");

            if (ourSlideshow.IsRunning)
            {
                Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_Deactivate(): turning off SlideshowMode.");
                fWasInSlideshowModeWhenDeactivated = true;
                ourSlideshow.Exit();
            }
            else
            {
                fWasInSlideshowModeWhenDeactivated = false;
            }

            if (fScreenSaverWindowStyle && !fShowingDialog)   // don't exit ScreenSaverWindowStyle if we're just showing our own dialog
            {
                Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_Deactivate(): Exiting ScreenSaverWindowStyle.");
                ExitScreenSaverWindowStyle();
            }

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Deactivate(): exiting.");
        }

        /// <summary>
        /// Code that runs when the Form regains focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenSaverForm_Activated(object sender, EventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            // never try to re-enter ScreenSaverWindowStyle when activating, half of Alt-Tab
            // events we don't get the Activated event 

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Activated(): entered.");

            if (fWasInSlideshowModeWhenDeactivated)
            {
                Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_Activated(): Entering ScreenSaverWindowStyle.");
                fWasInSlideshowModeWhenDeactivated = false;
                ourSlideshow.Start();
            }

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Activated(): exiting.");
        }

        /// <summary>
        /// Code that runs when the Form knows it is about to Close, but has not yet Closed.
        /// </summary>       
        private void ScreenSaverForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_FormClosing(): entered.");

            // This is your opportunity to prompt the user to save, or 
            // Cancel the Closure. You may end up here because somebody closed your window
            // in a way you weren't expecting (ie, Windows Shutdown, etc).

            fFormClosingIsRunning = true;

            // if slideShowTimer is active, kill it
            ourSlideshow.Exit();

            // Store away the window location and size
            if (this.WindowState != FormWindowState.Maximized && this.WindowState != FormWindowState.Minimized)
            {
                SettingsInfo.dbgLastWindowLocationPoint = this.Location;
                SettingsInfo.dbgLastWindowSize = this.Size;
            }

            // Get this session's blacklist from the MainFiles object and add it to config settings
            SettingsInfo.AddFullFilenamesToBlacklist((List<String>)MainFiles.BlacklistedFullFilenames);

            // Store away the current font info... although it should already be current...
            UpdateConfigSettingsFromMetaFontData();

            // Save the ConfigSettings
            SettingsInfo.SaveConfigSettingsToStorage();

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_FormClosing(): exiting.");

            this.fFormClosingIsRunning = false;
        }

        /// <summary>
        /// Code that runs immediatelly after the Form Closes, but before the Form Object passes out of Scope.
        /// </summary>
        private void ScreenSaverForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_FormClosed(): entered.");

            fFormClosingHasCompleted = true;

            // Any time the ScreenSaverForm closes, quit the app.
            Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_FormClosed(): calling Application.Exit().");
            CancelEventArgs cea = new CancelEventArgs();
            Application.Exit(cea);
            Logging.LogLineIf(fDebugTrace, "   ScreenSaverForm_FormClosed(): CancelEventArgs.Cancel: " + cea.Cancel.ToString());
            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_FormClosed(): exiting.");
        }

        /// <summary>
        /// Code which runs AFTER the size of the form window has changed, for any reason.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenSaverForm_SizeChanged(object sender, EventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_SizeChanged(): entered.");

            if (fFormConstructorHasCompleted && fFormLoadHasCompleted)  // don't react before constructor runs or form_loads
            {
                if (fScreenSaverWindowStyle)
                {
                    if (WindowState == FormWindowState.Minimized)
                    {
                        Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_SizeChanged(): exiting the slideshow because we are minimized.");
                        ourSlideshow.Exit();
                    }
                }
            }
            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_SizeChanged(): exiting.");
        }

        /// <summary>
        /// Code that runs on our Form when the Apply button of the Font Dialog is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontDialog_Apply(object sender, System.EventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "FontDialog_Apply(): entered.");

            metaFontData.SetPropertiesFromFontDlg(fontdlg);
            pbMain.Invalidate();

            Logging.LogLineIf(fDebugTrace, "FontDialog_Apply(): exiting.");
        }

        #endregion Form Events


        #region Control Events
        // Not all Control Event code is in this section.  Some of it
        // resides in sections closer to their purpose.

        // Code which runs every time the pictureBox is Painted
        private void pbMain_Paint(object sender, PaintEventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugoutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugoutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "pbMain_Paint(): entered.");

            // Draw the image metadata and ETF data
            if (fFormConstructorHasCompleted && fFormLoadHasCompleted)
            {
                PaintText(e);
            }
            else
            {
                Logging.LogLineIf(fDebugTrace, "pbMain_Paint(): not painting text, as fFormConstructorHasCompleted && fFormLoadHasCompleted != true.");
            }
            Logging.LogLineIf(fDebugTrace, "pbMain_Paint(): exiting.");
        }

        // For Context Menu Events, see file KeyboardMouseMenu.cs
        // For pbMain LoadCompleted event, see the region
        // "Methods Called By Form and Control Events", under the 
        // "Putting the Picture On Screen" section

        #endregion Control Events


        #region Methods Called By Form and Control Events

        // ----- Putting the Picture On Screen ----- //

        /// <summary>
        /// Gets metadata from the file, then loads the image from the file into the PictureBox, asynchronously.
        /// </summary>
        /// <param name="file">File from which to get metadata and load image into PictureBox.</param>
        /// <remarks>This is actually a synchronous operation, as we theoretically block user input until 
        /// the the pictureBox's LoadCompleted event occurs, by setting the pictureBox's WaitOnLoad property to true.</remarks>
        private void LoadFileIntoPictureBoxAsync(FileInfo file)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "LoadFileIntoPictureBoxAsync(): entered.");

            try
            {
                if (file != null && File.Exists(file.FullName))
                {
                    // clear data
                    if (currentImageMetadata != null) currentImageMetadata = "Getting metadata...";
                    if (currentExploreFolderData != null) currentExploreFolderData = "Getting folder data...";

                    // clear picture box
                    pbMain.ImageLocation = String.Empty;
                    pbMain.Image = null;

                    // restore Zoom mode, if not already there
                    if (pbMain.SizeMode != PictureBoxSizeMode.Zoom) pbMain.SizeMode = PictureBoxSizeMode.Zoom;

                    // set current data
                    currentImageMetadata = GetMetadata(file);      // yes, this does mean we are fetching the image twice,
                    // but if we don't, we won't draw the metadata until after the async
                    // file load, and that means text will pop onto screen after picture is drawn

                    // update the current ETF data if necessary
                    if (fInETFMode)
                    {
                        SetExploreFolderData(file);
                    }

                    fWaitingForFileToLoad = true;                       // this will be reset to false in the pbMain_LoadCompleted event handler

                    // When the image has finished loading, we'll get an pbMain_LoadCompleted event, where we will continue things
                    pbMain.WaitOnLoad = true;
                    pbMain.LoadAsync(file.FullName);

                }
                else  // file was null, or didn't exist any more
                {
                    Logging.LogLineIf(fDebugOutput, "   LoadFileIntoPictureBoxAsync(): File was null or did not exist.");
                    ShowNoImageError();
                }
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "  * LoadFileIntoPictureBoxAsync(): Exception loading image from file '" + file.FullName + "'. Exception: " + ex.Message);
                ShowNoImageError();
            }

            Logging.LogLineIf(fDebugTrace, "LoadFileIntoPictureBoxAsync(): exiting.");

        }

        /// <summary>
        /// Control Event handler - handles event which fires when PictureBox has completed loading of file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbMain_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "pbMain_LoadCompleted(): entered.");

            // Our memory footprint goes up with each image loaded, until the CLR thinks
            // it looks too high; then the CLR calls GC.Collect to force garbage collection.
            // We can instead force the CLR to garbage collect after each image, which keeps
            // our memory footprint smaller, but can possibly slow down very fast scrolling
            // through a list of images. Uncomment the following line to make that happen.

            //GC.Collect();

            if (fDebugTrace)
            {
                long newTotalMemory = GC.GetTotalMemory(false);
                string output = "   pbMain_LoadCompleted(): Total Memory occupied: " + newTotalMemory;
                if (newTotalMemory > dbgTotalMemoryHighWaterMark)
                {
                    output += " new high (old high was " + dbgTotalMemoryHighWaterMark + ")";
                    dbgTotalMemoryHighWaterMark = newTotalMemory;
                }
                Logging.LogLineIf(fDebugTrace, output);
            }

            // In theory, if we are here, file has loaded and been drawn. I certainly hope so.
            fShowingEmbeddedFileImage = false;
            fWaitingForFileToLoad = false;

            // if the image is less than 1/3 of the picturebox both dimensions, change zoom mode to center
            if ((pbMain.Image.PhysicalDimension.Height * 3 ) < pbMain.Height &&
                (pbMain.Image.PhysicalDimension.Width * 3 ) < pbMain.Width) pbMain.SizeMode = PictureBoxSizeMode.CenterImage;

            Logging.LogLineIf(fDebugTrace, "pbMainPhoto_LoadCompleted(): exiting.");
        }

        /// <summary>
        /// Collects all the file and image metadata.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetMetadata(FileInfo file)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "GetMetadata(): entered.");

            string retVal = "";
            retVal += GetBasicFileInfo(file) + Environment.NewLine;
            retVal += GetImageMetadata(file) + Environment.NewLine;
            retVal += GetExtendedFileDetails(file);

            Logging.LogLineIf(fDebugTrace, "GetMetadata(): exiting.");
            return retVal;
        }

        /// <summary>
        /// Gets the most basic file information from the FileInfo block.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetBasicFileInfo(FileInfo file)
        {
            string retVal = "";
            // get the full path and collapse it down to an "ellipsis" version
            string fullPath = Path.GetFullPath(file.DirectoryName).EllipsisString(35);
            if (!fullPath.Contains(@"\"))
            {
                fullPath = Path.GetPathRoot(file.DirectoryName) + @"...\" + fullPath;
            }

            // first two items: filename and shortened path
            retVal += file.Name;
            retVal += Environment.NewLine + fullPath;
            return retVal;
        }

        /// <summary>
        /// Gets the metadata stored in the image itself.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetImageMetadata(FileInfo file)
        {
            string retVal = "";
            string strTemp;

            // get an image from the file, so we can get its details
            Image image = Image.FromFile(file.FullName);

            // Next item, DateTaken
            // In this logic, we only add Date Taken if we can find a Date Taken value
            DateTime? myDateTime = image.GetDateTaken();
            if (myDateTime != null)
            {
                DateTime myOtherDateTime = (DateTime)myDateTime;
                //stMeta = myOtherDateTime.ToString("yyyy/MM/dd  HH:mm:ss");
                strTemp = myOtherDateTime.ToString();
                retVal += strTemp;
            }

            // Description, which isn't Comments, but might be worth listing if it's filled with something.
            // Doesn't get added if nothing is found.
            strTemp = null;
            strTemp = image.GetDescription();
            if (strTemp != null)
            {
                retVal += Environment.NewLine + strTemp;
            }

            //if ((strTemp = image.GetDescription()) != null) retVal += Environment.NewLine + strTemp;

            return retVal;
        }

        /// <summary>
        /// Gets the file information stored in the Extended File Details.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fUseCachedFolders"></param>
        /// <returns></returns>
        private string GetExtendedFileDetails(FileInfo file, bool fUseCachedFolders = true)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "GetExtendedFileDetails(): entered.");

            string retVal = "";

            // Note that you can still force the read-from-disk way by calling GetExtendedFileDetails(FileFullname, false).

            // create our Shell32 object
            Shell32.Folder objFolder;

            // Cached method: get the objFolder from a Dictionary of 
            // Shell32.Folders we built during startup, instead of creating a new
            // shell object every time.
            if (fUseCachedFolders)
            {
                if (!MainFiles.ShellDict.TryGetValue(file.DirectoryName, out objFolder))
                {
                    Logging.LogLineIf(fDebugOutput, "  GetExtendedFileDetails(): Failed to get objFolder from Dictionary. Falling back to read from disk method.");
                    GetExtendedFileDetails(file, false);
                }
            }
            else        // for some reason, desired objfolder was not found in cache, so build it
            {
                Shell32.Shell shell = new Shell32.Shell();
                // objFolder = shell.NameSpace(Path.GetDirectoryName(file.FullName));
                objFolder = shell.NameSpace(file.DirectoryName);
            }

            // get the data block for the file
            Shell32.FolderItem2 xItem = (Shell32.FolderItem2)objFolder.ParseName(file.Name);

            // get the Comments at index 24 from the data
            bool fGot24 = false;
            string stReturn = objFolder.GetDetailsOf(xItem, 24).ToString().Trim();
            if (!string.IsNullOrEmpty(stReturn) && !string.IsNullOrWhiteSpace(stReturn))
            {
                retVal += stReturn;
                fGot24 = true;
            }

            // Get the Tags at index 18
            stReturn = objFolder.GetDetailsOf(xItem, 18).ToString().Trim();
            if (!string.IsNullOrEmpty(stReturn) && !string.IsNullOrWhiteSpace(stReturn))
            {
                // Let's give ourselves a new line for every tag
                stReturn = stReturn.Replace(";", Environment.NewLine + "  ");
                if (fGot24) retVal += Environment.NewLine;
                retVal += "Tags: " + Environment.NewLine + "   " + stReturn;
            }

            Logging.LogLineIf(fDebugTrace, "GetExtendedFileDetails(): exiting.");
            return retVal;
        }

        /// <summary>
        /// Shows the "Error" image, and sets the metadata for it.
        /// </summary>
        private void ShowNoImageError()
        {
            pbMain.Image = PattySaverResources.noimage;
            pbMain.ImageLocation = String.Empty;
            fShowingEmbeddedFileImage = true;
            currentImageMetadata = "Well_that_happened.jpg" + Environment.NewLine +
                @"C:\That's Just Great\...\Just Perfect" + Environment.NewLine +
                "Date: Right About Now" + Environment.NewLine +
                "Tags:" + Environment.NewLine +
                "  No picture" + Environment.NewLine +
                "  Better apologize" + Environment.NewLine +
                "  Try to smile" + Environment.NewLine +
                "  Probably User error" + Environment.NewLine;
        }

        /// <summary>
        /// Collects the ETF display data and puts it into the currentExploreFolderData field.
        /// </summary>
        /// <param name="file"></param>
        private void SetExploreFolderData(FileInfo file = null)
        {
            string fullFilename = "";
            if (file != null)
            {
                fullFilename = file.FullName;
            }
            else
            {
                fullFilename = pbMain.ImageLocation;
            }

            string retVal = "";

            if (!String.IsNullOrEmpty(fullFilename) || !String.IsNullOrWhiteSpace(fullFilename))
            {
                if (fInETFMode)
                {
                    int index = ETFFiles.IndexOf(ETFFiles.GetFileByFullName(fullFilename));
                    int count = ETFFiles.Count;

                    int blackListCount = ETFFiles.DirectoryInfo.GetFiles().Count(c => ETFFiles.BlacklistedFullFilenames.Contains(c.FullName) || MainFiles.BlacklistedFullFilenames.Contains(c.FullName));

                    retVal = "Exploring: " + ETFFiles.DirectoryInfo.FullName + Environment.NewLine;
                    retVal = retVal + "(Use ˄ or ˅ ) - Viewing File " + (index + 1) + " of " + count + " (plus " + blackListCount + " blacklisted files)";
                }
            }

            currentExploreFolderData = retVal;
        }



        // -------------------- Navigation Stuff -------------------- //

        /// <summary>
        /// Gets the next or previous file in the FileInfo list, and calls the drawing code on it.
        /// </summary>
        /// <param name="fPrevious">Pass True if you want the previous file, not the next.</param>
        private void DoPreviousOrNext(bool fPrevious)
        {
            // if we are in ETF mode, exit it in the correct direction
            if (fInETFMode)
            {
                ExitExploreFolderMode(fPrevious);
                return;
            }

            // Get the appropriate File.
            FileInfo file = null;

            // If we're going backwards...
            if (fPrevious)
            {
                file = MainFiles.GetPreviousFile();
            }
            else // If we're going forwards...
            {
                // Get the next file so we can show it
                file = MainFiles.GetNextFile();
            }

            // Draw it
            LoadFileIntoPictureBoxAsync(file);
        }

        /// <summary>
        /// Same as DoPreviousOrNext(), except it is called when in ETF Mode.
        /// </summary>
        /// <param name="fPrevious"></param>
        private void DoETFPreviousOrNext(bool fPrevious)
        {
            FileInfo file;

            if (fPrevious)
            {
                file = ETFFiles.GetPreviousFile();
            }
            else
            {
                file = ETFFiles.GetNextFile();
            }

            LoadFileIntoPictureBoxAsync(file);
        }

        /// <summary>
        /// Enters the Explore This Folder modality.
        /// </summary>
        private void EnterExploreFolderMode()
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = true;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "EnterExploreFolderMode(): entered.");

            if (fInETFMode)
            {
                Logging.LogLineIf(fDebugTrace, "    EnterExploreFolderMode(); Not Entering Explore This Folder mode, we're already in it.");
                Logging.LogLineIf(fDebugTrace, "EnterExploreFolderMode(): Exiting method.");
                return;
            }

            if (fShowingEmbeddedFileImage)
            {
                // do nothing, we're showing an embedded resource
                Logging.LogLineIf(fDebugOutput, "   EnterExploreFolderMode(): called while we were displaying an embedded image, not a file. How did that happen?");
                MessageBox.Show("Explore This Folder is not available right now.", ProductName + " - Nice Try",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.LogLineIf(fDebugTrace, "EnterExploreFolderMode(): Exiting method.");
                return;
            }

            if (String.IsNullOrEmpty(pbMain.ImageLocation) || String.IsNullOrWhiteSpace(pbMain.ImageLocation))
            {
                Logging.LogLineIf(fDebugOutput, "   * EnterExploreFolderMode(): Not entering Explore This Folder mode because one of these failed:" + Environment.NewLine +
                    "String.IsNullOrEmpty(pbMainPhoto.ImageLocation) || String.IsNullOrWhiteSpace(pbMainPhoto.ImageLocation)");

                System.Diagnostics.Debug.Assert((false), "   EnterExploreFolderMode():  Not entering Explore This Folder mode, as one of these failed:" + Environment.NewLine +
                    "String.IsNullOrEmpty(pbMainPhoto.ImageLocation) || String.IsNullOrWhiteSpace(pbMainPhoto.ImageLocation)",
                    "Safe to click Continue.");

                MessageBox.Show("There has been an error. Cannot explore this folder at this time." + Environment.NewLine + Environment.NewLine +
                "Filename: " + pbMain.ImageLocation + Environment.NewLine, ProductName + " - Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.LogLineIf(fDebugTrace, "EnterExploreFolderMode(): Exiting method.");
                return;
            }

            bool fWasInSlideshowMode = ourSlideshow.IsRunning;
            string fullFilename = pbMain.ImageLocation;

            Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): Attempting to enter ETF mode for file: " + pbMain.ImageLocation);
            Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): MainFiles CurrentIndex / Count: " + MainFiles.CurrentIndex + " / " + MainFiles.Count);

            Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): About to call MainFiles.GetFileByFullName(CurrentFileName)...");
            FileInfo etfEntryFile = MainFiles.GetFileByFullName(fullFilename);
            if (etfEntryFile == null)
            {
                Logging.LogLineIf(fDebugOutput, "   * EnterExploreFolderMode(): etfEntryFile == null.");
#if DEBUG
                System.Diagnostics.Debug.Assert((false), "   EnterExploreFolderMode(): etfEntryFile == null.", "Will throw exception when you click Continue.");
#endif
                throw new InvalidOperationException("   EnterExploreFolderMode(): etfEntryFile == null.");
            }

            // Create a new FileInfoSource, and store it at the FormWide level
            Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): About to create ETF object...");
            ETFFiles = new ExploreThisFolderFileInfoSource(MainFiles, etfEntryFile);

            if ((ETFFiles != null) && (ETFFiles.DirectoryInfo != null))   // if there was an error etf.DirectoryInfo will be null. Probably.
            {
                Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): ETF object created successfully.");
                fInETFMode = true;
                fWasInSlideshowModeWhenETFStarted = fWasInSlideshowMode;
                if (fWasInSlideshowMode) ourSlideshow.Exit();

                // Update text for "You are in ETF mode" indicator
                SetExploreFolderData();

            }
            else
            {
                fInETFMode = false;
                ETFFiles = null;
                if (fWasInSlideshowMode) fWasInSlideshowModeWhenETFStarted = false;
                if (fWasInSlideshowMode) ourSlideshow.Start();

                Logging.LogLineIf(fDebugOutput, "   * EnterExploreFolderMode(): FAILED test: (ETFFiles != null) && (ETFFiles.DirectoryInfo != null)");
#if DEBUG
                System.Diagnostics.Debug.Assert((false), "   EnterExploreFolderMode(): FAILED test: (ETFFiles != null) && (ETFFiles.DirectoryInfo != null).", "Will throw exception when you click Continue.");
                throw new InvalidOperationException("   EnterExploreFolderMode(): FAILED test: (ETFFiles != null) && (ETFFiles.DirectoryInfo != null).");
#else

                MessageBox.Show("There has been an error. Cannot explore this folder at this time." + Environment.NewLine + Environment.NewLine +
                    "Filename: " + pbMainPhoto.ImageLocation + Environment.NewLine, ProductName + " - Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
#endif
            }

            Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): Entered ETF mode for file: " + fullFilename);
            Logging.LogLineIf(fDebugTrace, "   EnterExploreFolderMode(): ETFFiles CurrentIndex / Count: " + ETFFiles.CurrentIndex + " / " + ETFFiles.Count);

            // Tell PictureBox to update so new text will be drawn
            pbMain.Invalidate();
            Logging.LogLineIf(fDebugTrace, "EnterExploreFolderMode(): Exiting method.");

        }

        /// <summary>
        /// Exits Explore This Folder mode, in the direction specified in fPrevious.
        /// </summary>
        /// <param name="fPrevious"></param>
        private void ExitExploreFolderMode(bool fPrevious, bool fExternallyCalled = false)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = true;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "ExitExploreFolderMode(): Entering method, fExternallyCalled = " + fExternallyCalled);

            if (fInETFMode)
            {
                Logging.LogLineIf(fDebugTrace, "   ExitExploreFolderMode(): MainFile CurrentIndex / Count: " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                Logging.LogLineIf(fDebugTrace, "   ExitExploreFolderMode(): exiting mode by going " + ((fPrevious) ? "backwards." : "forwards."));

                // Kill off the ETF object
                ETFFiles = null;
                fInETFMode = false;

                // Get the file in the direction passed to us
                Logging.LogLineIf(fDebugTrace, "   ExitExploreFolderMode(): about to call DoPreviousOrNext(fPrevious)...");
                DoPreviousOrNext(fPrevious);
                Logging.LogLineIf(fDebugTrace, "   ExitExploreFolderMode(): After DoPreviousOrNext(), MainFiles CurrentIndex / Count: " + MainFiles.CurrentIndex + " / " + MainFiles.Count);

                // Restart the slideshow if necessary
                Logging.LogLineIf(fDebugTrace, "   ExitExploreFolderMode(): about to restart slideshow, if necessary.");
                if (fWasInSlideshowModeWhenETFStarted) ourSlideshow.Start();
            }
            else
            {
                Logging.LogLineIf(fDebugTrace, "ExitExploreFolderMode(): Not exiting mode, as we were not in ETF mode.");
            }
            Logging.LogLineIf(fDebugTrace, "ExitExploreFolderMode(): Exiting method.");
        }

        /// <summary>
        /// Called when user hits Up arrow key on keyboard.
        /// </summary>
        private void DoArrowKeyUp()
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "DoArrowKeyUp(): entered.");

            if (fWaitingForFileToLoad)
            {
                // do nothing
                Logging.LogLineIf(fDebugOutput, "   DoArrowKeyUp(): Ignoring key, still waiting for file to load.");
                return;
            }


            if (fInETFMode) // we're already in mode, so we must want next file
            {
                DoETFPreviousOrNext(false);
            }
            else
            {
                if (ourSlideshow.IsRunning)
                {
                    fWasInSlideshowModeWhenETFStarted = true;
                    ourSlideshow.Exit();
                }

                EnterExploreFolderMode();
            }

            Logging.LogLineIf(fDebugTrace, "DoArrowKeyUp(): Exiting.");
        }

        /// <summary>
        /// Called when user hits Down arrow key on keyboard.
        /// </summary>
        private void DoArrowKeyDown()
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "DoArrowKeyDown(): entered.");

            if (fWaitingForFileToLoad)
            {
                // do nothing
                Logging.LogLineIf(fDebugOutput, "   DoArrowKeyDown(): Ignoring key, still waiting for file to load.");
                return;
            }


            if (fInETFMode) // we're already in mode, so we must want next file
            {
                DoETFPreviousOrNext(true);
            }
            else
            {
                if (ourSlideshow.IsRunning)
                {
                    fWasInSlideshowModeWhenETFStarted = true;
                    ourSlideshow.Exit();
                }

                EnterExploreFolderMode();
            }
            Logging.LogLineIf(fDebugTrace, "DoArrowKeyDown(): Exiting.");
        }

        /// <summary>
        /// Called when user hits Left arrow key on keyboard.
        /// </summary>
        private void DoArrowKeyLeft()
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "DoArrowKeyLeft(): entered.");

            // we need to either:
            // 0. If in a state where we should just ignore the key, do that
            // 1. if not in slideshow mode, just go to previous photo
            // 2. if we're in slideshow mode, defer it, so that the user can view current photo longer; 
            // 3. if we're actually already in defer mode, force previous photo immediately

            if (fWaitingForFileToLoad)
            {
                // do nothing
                Logging.LogLineIf(fDebugOutput, "   DoArrowKeyLeft(): Ignoring key, still waiting for file to load.");
                return;
            }

            if (ourSlideshow.IsRunning)
            {
                if (ourSlideshow.Defer()) // returns false if left arrow keys struck close to each other
                {
                    // we just deferred
                    Logging.LogLineIf(fDebugTrace, "   DoArrowKeyLeft(): Defer returned true.");
                    return;
                }
                else
                {
                    // user pressed left key twice in a row
                    Logging.LogLineIf(fDebugTrace, "   DoArrowKeyLeft(): Defer returned false, DoPreviousOrNext() will be called.");
                    ourSlideshow.Exit();
                    DoPreviousOrNext(true);
                    ourSlideshow.Start();
                    return;
                }
            }
            else
            {
                DoPreviousOrNext(true);
            }

            Logging.LogLineIf(fDebugTrace, "DoArrowKeyLeft(): Exiting.");
        }

        /// <summary>
        /// Called when user hits Right arrow key on keyboard.
        /// </summary>
        private void DoArrowKeyRight()
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "DoArrowKeyRight(): entered.");

            // we need to either:
            // 0. If in a state where we should just ignore the key, do that
            // 1. if not in slideshow mode, just go to next photo
            // 2. if we're in slideshow mode, force next photo immediately, and reset any slideshow mode stuff we need to

            if (fWaitingForFileToLoad)
            {
                // do nothing
                Logging.LogLineIf(fDebugOutput, "   DoArrowKeyRight(): Ignoring key, still waiting for file to load.");
                return;
            }

            if (ourSlideshow.IsRunning)
            {
                ourSlideshow.Exit();
                DoPreviousOrNext(false);
                ourSlideshow.Start();
                return;
            }
            else
            {
                DoPreviousOrNext(false);
            }
            Logging.LogLineIf(fDebugTrace, "DoArrowKeyRight(): Exiting.");
        }

        /// <summary>
        /// Marks the current file as "Do not display", then navigates to next file.
        /// </summary>
        private void DoBlacklistCurrentFile()
        {

            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = true;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "DoBlacklistFile(): Entering method.");

            // ------------  Let's Try To Get Out of Doing It  ------------ //

            if (fShowingEmbeddedFileImage)
            {
                // do nothing, we're showing an embedded resource
                Logging.LogLineIf(fDebugTrace, "   * DoBlacklistFile(): called while we were displaying an embedded image, not a file.");
                Logging.LogLineIf(fDebugTrace, "DoBlacklistFile(): Exiting method.");
                MessageBox.Show("You cannot blacklist this Picture.", ProductName + " - Nice Try",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (String.IsNullOrEmpty(pbMain.ImageLocation) || String.IsNullOrWhiteSpace(pbMain.ImageLocation))
            {
                Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): Aborting, as one of these failed:" + Environment.NewLine +
                    "String.IsNullOrEmpty(pbMainPhoto.ImageLocation) || String.IsNullOrWhiteSpace(pbMainPhoto.ImageLocation)");

                System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): Aborting, as one of these failed:" + Environment.NewLine +
                    "String.IsNullOrEmpty(pbMainPhoto.ImageLocation) || String.IsNullOrWhiteSpace(pbMainPhoto.ImageLocation)",
                    "Please click Continue.");

                MessageBox.Show("There has been an error. This Picture cannot be blacklisted." + Environment.NewLine + Environment.NewLine +
                "Filename: " + pbMain.ImageLocation + Environment.NewLine, ProductName + " - Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.LogLineIf(fDebugTrace, "DoBlacklistFile(): Exiting method.");
                return;
            }

            if (MessageBox.Show("Never show this picture again?" + Environment.NewLine + Environment.NewLine +
                        "Filename: " + pbMain.ImageLocation + Environment.NewLine, ProductName + " - Blacklist this Picture?",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Cancel)
            {
                Logging.LogLineIf(fDebugTrace, "DoBlacklistFile(): Exiting method.");
                return;
            }

            // ------------  Okay, Then Let's Get To Work  ------------ //

            if (fInETFMode)
            {
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): ETF CurrentIndex / Count : " + ETFFiles.CurrentIndex + " / " + ETFFiles.Count);
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): Main CurrentIndex / Count : " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call ETF.GetFileByFullName(CurrentFileName)...");

                // get a FileInfo block from current file
                FileInfo etfFileToBeBlacklisted = ETFFiles.GetFileByFullName(pbMain.ImageLocation);
                FileInfo mainFileToBeBlacklisted = MainFiles.GetFileByFullName(pbMain.ImageLocation);

                if (etfFileToBeBlacklisted == null)
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): etfFileToBeBlacklisted == null.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): etfFileToBeBlacklisted == null.", "Will throw exception when you click Continue.");
#endif
                    throw new InvalidOperationException("   DoBlacklistFile(): etfFileToBeBlacklisted == null.");
                }

                if (mainFileToBeBlacklisted == null)
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): mainFileToBeBlacklisted == null.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): mainFileToBeBlacklisted == null.", "Will throw exception when you click Continue.");
#endif
                    throw new InvalidOperationException("   DoBlacklistFile(): mainFileToBeBlacklisted == null.");
                }

                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): fileToBeBlacklisted located at ETFIndex / MainIndex : " +
                    ETFFiles.IndexOf(etfFileToBeBlacklisted) + " / " + MainFiles.IndexOf(mainFileToBeBlacklisted));

                // blacklist the file in the ETFIS list
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call ETF.BlacklistCurrentFile()...");
                FileInfo nextFile; // unused, but required for next method

                if (ETFFiles.BlacklistCurrentFile(out nextFile))
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): ETF.BlacklistCurrentFile() reports success.");
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): ETF CurrentIndex / Count : " + ETFFiles.CurrentIndex + " / " + ETFFiles.Count);
                }
                else
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): ETF.BlacklistCurrentFile() reports FAILURE.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): ETF.BlacklistCurrentFile() reports FAILURE.", "Will throw exception if you click Continue.");
                    throw new InvalidOperationException("   DoBlacklistFile(): ETF.BlacklistCurrentFile() reports FAILURE.");
#else
                            MessageBox.Show("Could not blacklist this Picture, an error occurred." + Environment.NewLine + Environment.NewLine +
                            "Filename: " + pbMainPhoto.ImageLocation + Environment.NewLine, ProductName + "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
#endif
                }

#if DEBUG
                // For the hell of it, validate nextFile, even though we will not use it
                if (nextFile != null)
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): BlacklistCurrentFile() offers us as nextFile (which we will ignore): " + nextFile.FullName);
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): nextFile located at MainIndex : " + ETFFiles.IndexOf(nextFile));
                }
                else
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): BlacklistCurrentFile() offers us nextFile == NULL. In theory this means that ETFFiles is now empty. Is it?");
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): Main CurrentIndex / Count : " + ETFFiles.CurrentIndex + " / " + ETFFiles.Count);
                }
#endif

                // Now, remove the file from the Main List
                if (!MainFiles.RemoveBlacklistedETFFile(mainFileToBeBlacklisted))
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): MainFiles.RemoveBlacklistedETFFile(mainFileToBeBlacklisted) FAILED.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): MainFiles.RemoveBlacklistedETFFile(mainFileToBeBlacklisted) FAILED.", "Will throw exception if you click Continue.");
#endif
                    Logging.LogLineIf(fDebugOutput, "   DoBlacklistFile(): After Failure, Main CurrentIndex / Count : " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                    throw new InvalidOperationException("   DoBlacklistFile(): MainFiles.RemoveBlacklistedETFFile(mainFileToBeBlacklisted) FAILED.");
                }
                else
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): After removal of mainFileToBeBlacklisted, Main CurrentIndex / Count : " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                }

                // If ETFFiles is now empty, exit Explore mode
                if (ETFFiles.Count < 1)
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): ETFFiles is now empty, so forcing exit of ETF Mode.");
                    ExitExploreFolderMode(false, true);
                    return;
                }

                // Draw the file from the ETF list
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call ETFFiles.GetCurrentFile()...");
                FileInfo etfToBeDrawn = ETFFiles.GetCurrentFile();
#if DEBUG
                if (etfToBeDrawn == null)
                {
                    Logging.LogLineIf(fDebugOutput, "   *DoBlacklistFile(): etf.GetCurrentFile() returned NULL.  No image will be drawn.");
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile():  etf.GetCurrentFile() returned NULL.  No image will be drawn.", "Will throw exception if you click Continue.");
                    throw new InvalidOperationException("   DoBlacklistFile():  etf.GetCurrentFile() returned NULL.  No image will be drawn.");
                }
#endif

                FileInfo mainToBeDrawn = MainFiles.GetFileByFullName(etfToBeDrawn.FullName);
#if DEBUG
                if (mainToBeDrawn == null)
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): MainFiles.GetFileByFullName(etfToBeDrawn.FullName) returned NULL. " +
                        "There is no file in the MainFiles list that matches the file we are drawing from the ETFFiles list.");
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile():  MainFiles.GetFileByFullName(etfToBeDrawn.FullName) returned NULL. " +
                        "There is no file in the MainFiles list that matches the file we are drawing from the ETFFiles list.", "Will throw exception if you click Continue.");
                    throw new InvalidOperationException("   DoBlacklistFile():  MainFiles.GetFileByFullName(etfToBeDrawn.FullName) returned NULL.");
                }
#endif

                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): ETFFiles.GetCurrentFile() result located at ETFIndex / MainIndex : " +
                    ETFFiles.IndexOf(etfToBeDrawn) + " / " + MainFiles.IndexOf(mainToBeDrawn));

                // Even if files were null, we draw anyway. Drawing Code handles it.
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call DrawImageFromFile(result)...");
                LoadFileIntoPictureBoxAsync(etfToBeDrawn);

#if DEBUG
                // Just out of curiousity...
                if (nextFile != null && etfToBeDrawn != null)
                {
                    if (nextFile.FullName != etfToBeDrawn.FullName)
                    {
                        Logging.LogLineIf(fDebugOutput, "     DoBlacklistFile(): Weird, but safe - nextFile.FullName != result.FullName. How did that happen?");
                    }
                }
#endif
            }

            else        // In Main Mode
            {
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): Main CurrentIndex / Count : " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call Main.GetFileByFullName(pbMainPhoto.ImageLocation)...");

                // get a FileInfo block from current file
                FileInfo fileToBeBlacklisted = MainFiles.GetFileByFullName(pbMain.ImageLocation);

                if (fileToBeBlacklisted == null)
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): fileToBeBlacklisted == null.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): fileToBeBlacklisted == null.", "Will throw exception if you click Continue.");
#endif
                    throw new InvalidOperationException("   DoBlacklistFile(): fileToBeBlacklisted == null.");
                }
                else
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): fileToBeBlacklisted located at MainIndex : " + MainFiles.IndexOf(fileToBeBlacklisted));
                }

                // blacklist the file in the Main list
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call Main.BlacklistCurrentFile()...");
                FileInfo nextFile; // unused, but required for next method

                if (MainFiles.BlacklistCurrentFile(out nextFile))
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): MainFiles.BlacklistCurrentFile() reports success.");
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): MainFiles CurrentIndex / Count : " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                }
                else
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): MainFiles.BlacklistCurrentFile() reports FAILURE.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): MainFiles.BlacklistCurrentFile() reports FAILURE.", "Will throw exception if you click Continue.");
                    throw new InvalidOperationException("   DoBlacklistFile(): MainFiles.BlacklistCurrentFile() reports FAILURE.");
#else
                            MessageBox.Show("Could not blacklist this Picture, an error occurred." + Environment.NewLine + Environment.NewLine +
                            "Filename: " + pbMainPhoto.ImageLocation + Environment.NewLine, ProductName + "Error",
                            MessageBoxButtons.OK);
                            return;
#endif
                }

#if DEBUG
                // Just for the hell of it, let's validate nextFile, even though we will not use it
                if (nextFile != null)
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): BlacklistCurrentFile() offers us as nextFile (which we will ignore): " + nextFile.FullName);
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): nextFile located at MainIndex : " + MainFiles.IndexOf(nextFile));
                }
                else
                {
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): BlacklistCurrentFile() offers us as nextFile NULL. In theory this means that MainFiles is now empty. Is it?");
                    Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): Main CurrentIndex / Count : " + MainFiles.CurrentIndex + " / " + MainFiles.Count);
                }
#endif

                // And then draw the current Main file
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call MainFiles.GetCurrentFile()...");
                FileInfo toDraw = MainFiles.GetCurrentFile();

                // Null is only unexpected if MainFiles is not empty
                if (toDraw == null && MainFiles.Count != 0)
                {
                    Logging.LogLineIf(fDebugOutput, "   * DoBlacklistFile(): MainFiles.GetCurrentFile() result == null.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "   DoBlacklistFile(): MainFiles.GetCurrentFile() result == null.", "Will throw exception if you click Continue.");
                    throw new InvalidOperationException("   DoBlacklistFile(): MainFiles.GetCurrentFile() result == null.");
#endif
                }
                else
                {
                    if (toDraw != null)
                    {
                        Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): MainFiles.GetCurrentFile() result located at MainIndex : " +
                           MainFiles.IndexOf(toDraw));
                    }
                }

                // Even if files were null, we draw anyway. Drawing Code handles it.
                Logging.LogLineIf(fDebugTrace, "   DoBlacklistFile(): About to call GetImageFromFileAndLoadAsync(result)...");
                LoadFileIntoPictureBoxAsync(toDraw);

#if DEBUG
                // Just out of curiousity...
                if (nextFile != null && toDraw != null)
                {
                    if (nextFile.FullName != toDraw.FullName)
                    {
                        Logging.LogLineIf(fDebugOutput, "     DoBlacklistFile(): Weird, but safe - nextFile.FullName != result.FullName. How did that happen?");
                    }
                }
#endif
            }

            Logging.LogLineIf(fDebugTrace, "DoBlacklistFile(): Entering method.");
        }




        // -------------------- Lifecycle Stuff -------------------- //

        private void DoSettingsDialog()
        {

            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = true;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "DoSettingsDialog(): Entered.");


            // pause became redundant with our activation/deactivation code;
            // restore this code if we remove the activation/deactivation logic

            //// Pause the slideshow if necessary
            //bool fWasSlideshowMode = false;
            //if (fSlideshowMode)
            //{
            //    fWasSlideshowMode = true;
            //    ExitSlideshowMode();
            //}

            // Create the settings form and show it as a modal dialog.
            // Doing it all in one statement like this causes the Settings form to be created, used,
            // disposed of, and all references to it nulled, all in one line.

            settingsForm = new Settings(true, this);
            settingsForm.Owner = this;
            fShowingDialog = true;

            //if ((new Settings(true, this)).ShowDialog(this.Owner) == System.Windows.Forms.DialogResult.Retry)
            if (settingsForm.ShowDialog(this) == System.Windows.Forms.DialogResult.Retry)
            {
                fShowingDialog = false;

                // User changed something that requires a rebuild of the file list.
                MainFiles.Rebuild((List<DirectoryInfo>)SettingsInfo.GetListOfDirectoryInfo(), SettingsInfo.GetBlacklistedFullFilenames(), SettingsInfo.UseRecursion, SettingsInfo.ShuffleMode);

                // Don't go back to an old stale pic. Force ourselves to get a new one.
                DoPreviousOrNext(false);
            }
            else
            {
                fShowingDialog = false;
            }
            
            // dispose of the form, since we create a new one every time
            settingsForm.Dispose();

            // update the slideshow data, just in case
            ourSlideshow.IntervalInMilliSeconds = SettingsInfo.SlideshowIntervalInSecs * 1000;
            ourSlideshow.DeferralTimeWindowInMilliseconds = 1500;
            ourSlideshow.DeferralIntervalInMilliseconds = ourSlideshow.IntervalInMilliSeconds;

            // pause became redundant with our activation/deactivation code;
            // restore this code if we remove the activation/deactivation logic

            //// Resume the slideshow if necessary
            //if (fWasSlideshowMode)
            //{
            //    EnterSlideshowMode();
            //}

            Logging.LogLineIf(fDebugTrace, "DoSettingsDialog(): Exiting.");

        }

        private void DoHelpAboutDialog()
        {
            // Pause the slideshow if necessary
            bool fWasSlideshowMode = false;
            if (ourSlideshow.IsRunning)
            {
                fWasSlideshowMode = true;
                ourSlideshow.Exit();
            }

            fShowingDialog = true;

            // Create the form and show it as a modal dialog.
            // Doing it all in one statement like this causes the form to be created, used,
            // disposed of, and all references to it nulled, all in one line.
            if ((new HelpAbout(true, this)).ShowDialog(this.Owner) == System.Windows.Forms.DialogResult.OK)
            {
                // do nothing for now
            }

            fShowingDialog = false;

            // Resume the slideshow if necessary
            if (fWasSlideshowMode)
            {
                ourSlideshow.Start();
            }
        }

        private void EnterScreenSaverWindowStyle()
        {
            fWindowStyleIsChanging = true;

            this.ShowIcon = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            fScreenSaverWindowStyle = true;
            fWindowStyleIsChanging = false;

        }

        private void ExitScreenSaverWindowStyle(bool UseSavedWindowPosition = false)
        {
            fWindowStyleIsChanging = true;

            this.ShowIcon = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.ShowInTaskbar = true;
            this.TopMost = false;
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            if (UseSavedWindowPosition)
            {
                this.Location = SettingsInfo.dbgLastWindowLocationPoint;
                this.Size = SettingsInfo.dbgLastWindowSize;
            }

            fScreenSaverWindowStyle = false;
            fWindowStyleIsChanging = false;
        }

        private void ToggleScreenSaverWindowStyle()
        {
            if (fScreenSaverWindowStyle)
            {
                ExitScreenSaverWindowStyle();
            }
            else
            {
                EnterScreenSaverWindowStyle();
            }
        }

        private void ToggleShowMetadata()
        {
            SettingsInfo.ShowMetadata = !SettingsInfo.ShowMetadata;
            pbMain.Invalidate();
        }

        private void OpenFileExplorer(string FullFilename)
        {
            string args = @"/n, /select," + "\"" + FullFilename + "\"";
            System.Diagnostics.Process.Start("explorer.exe", args);
        }

        private void DoQuit()
        {
            this.Close();
        }



        // -------------------- Handling Of Screen Text -------------------- //

        private void PaintText(PaintEventArgs e)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            Logging.LogLineIf(fDebugTrace, "PaintText(): entered.");

            // Retrieve the graphics object.
            Graphics formGraphics = e.Graphics;

            // Set the TextRenderingHint property (we get this var at Load)
            formGraphics.TextRenderingHint = metaFontData.TextRenderingHint;

            // Set the text contrast (we get this var at Load).
            formGraphics.TextContrast = metaFontData.ContrastLevel;

            // Build the fonts
            BuildMetaFontFromFontData();
            BuildETFFontFromFontData();
            Brush shadowBrush = new SolidBrush(shadowBrushColor);

            // Draw the ETF Folder text if appropriate
            float adjustedMetadataTop = metadataTop;
            if (fInETFMode)
            {
                // Move the top of the metadata text down based on font size
                if (metaFontData.FontSize > 38)
                {
                    adjustedMetadataTop = (float)((ETFtoMetadataOffsetBase * 2) + (metaFontData.FontSize * 2.8));
                }
                else
                {
                    adjustedMetadataTop = (float)(ETFtoMetadataOffsetBase + (metaFontData.FontSize * 2.8));
                }

                // Draw the shadowing if appropriate
                if (metaFontData.Shadowing)
                {
                    formGraphics.DrawString(currentExploreFolderData, etfFont,
                        shadowBrush, allTextLeft + 1, ETFdataTop + 1);
                }

                // then draw the font
                formGraphics.DrawString(currentExploreFolderData, etfFont,
                    etfBrush, allTextLeft, ETFdataTop);
            }

            // Draw the metadata if appropriate
            // TODO: Direct read from ConfigSettings field.  Consider replacing with a method or property.
            if (SettingsInfo.ShowMetadata)
            {
                if (metaFontData.Shadowing)
                {
                    formGraphics.DrawString(currentImageMetadata, metaFont,
                        shadowBrush, allTextLeft + 1, adjustedMetadataTop + 1);
                }

                formGraphics.DrawString(currentImageMetadata, metaFont,
                    metaBrush, allTextLeft, adjustedMetadataTop);
            }

            // Dispose of any resources we created in this method
            shadowBrush.Dispose();

            Logging.LogLineIf(fDebugTrace, "PaintText(): exiting.");
        }

        private void DoFontDialog()
        {
            FormatFontDialogFromFontData(metaFontData);
            fontdlg.ShowApply = true;
            fontdlg.ShowColor = true;

            fShowingDialog = true;

            DialogResult dr = fontdlg.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                fShowingDialog = false;
                if (metaFontData.SetPropertiesFromFontDlg(fontdlg))
                {
                    pbMain.Invalidate();
                }
            }
            fShowingDialog = false;
        }

        private void DoColorDialog()
        {
            // Configure the color dialog
            colordlg.AllowFullOpen = true;
            colordlg.AnyColor = true;
            colordlg.FullOpen = true;

            // Show the Color Dialog
            DialogResult dr = colordlg.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                metaFontData.SetColorFromColorDlg(colordlg);
                pbMain.Invalidate();
            }
        }

        private void BuildMetaFontFromFontData()
        {
            if (metaFont != null)
            {
                metaFont.Dispose();
                metaFont = null;
            }
            if (metaBrush != null)
            {
                metaBrush.Dispose();
                metaBrush = null;
            }

            metaFont = new Font(metaFontData.FontName, metaFontData.FontSize, metaFontData.FontStyle);
            metaBrush = new SolidBrush(Color.FromName(metaFontData.FontColorName));
        }

        private void BuildETFFontFromFontData()
        {
            if (etfFont != null)
            {
                etfFont.Dispose();
                etfFont = null;
            }

            etfFont = new Font(metaFontData.FontName, metaFontData.FontSize, metaFontData.FontStyle);
        }

        private void SetFontDataFromConfigData()
        {
            // metaFontData.AllowNonAntiAliasedFonts = ConfigSettings.metaDataFont_allowNonAntiAliased;
            metaFontData.ContrastLevel = SettingsInfo.metaDataFont_contrastLevel;
            metaFontData.FontColorName = SettingsInfo.metaDataFont_fontColorName;
            metaFontData.FontName = SettingsInfo.metaDataFont_fontName;
            metaFontData.FontSize = SettingsInfo.metaDataFont_fontSize;
            metaFontData.FontStyle = SettingsInfo.metaDataFont_fontStyle;
            metaFontData.MaxFontSize = SettingsInfo.metaDataFont_maxFontSize;
            metaFontData.MinFontSize = SettingsInfo.metaDataFont_minFontSize;
            metaFontData.Shadowing = SettingsInfo.metaDataFont_shadowing;
            metaFontData.SetTextRenderingHint(SettingsInfo.metaDataFont_textRenderingHint);
        }

        private void FormatFontDialogFromFontData(FontData fontData)
        {
            if (fdFont != null)
            {
                fdFont.Dispose();
                fdFont = null;
            }
            fdFont = new Font(fontData.FontName, fontData.FontSize, fontData.FontStyle);
            fontdlg.Font = fdFont;
            fontdlg.Color = Color.FromName(fontData.FontColorName);
        }

        private void DoRestoreTextValuesToLoadedValues()
        {
            // Initialize Values, in case there are no saved values
            metaFontData = new FontData();

            // Now overwrite those values with saved values
            SetFontDataFromConfigData();
            pbMain.Invalidate();

        }

        private void DoUpdateFontColor(int delta)
        {
            metaFontData.IncrementOrDecrementFontColorName(delta < Math.Abs(delta), true);
            pbMain.Invalidate();
        }

        private void DoUpdateFontSize(int delta)
        {
            metaFontData.IncrementOrDecrementFontSize(delta < Math.Abs(delta));
            pbMain.Invalidate();
        }

        private void UpdateConfigSettingsFromMetaFontData()
        {
            SettingsInfo.metaDataFont_allowNonAntiAliased = metaFontData.AllowNonAntiAliasedFonts;
            SettingsInfo.metaDataFont_contrastLevel = metaFontData.ContrastLevel;
            SettingsInfo.metaDataFont_fontColorName = metaFontData.FontColorName;
            SettingsInfo.metaDataFont_fontName = metaFontData.FontName;
            SettingsInfo.metaDataFont_fontSize = metaFontData.FontSize;
            SettingsInfo.metaDataFont_fontStyle = metaFontData.FontStyle;
            SettingsInfo.metaDataFont_maxFontSize = metaFontData.MaxFontSize;
            SettingsInfo.metaDataFont_minFontSize = metaFontData.MinFontSize;
            SettingsInfo.metaDataFont_shadowing = metaFontData.Shadowing;
            SettingsInfo.metaDataFont_textRenderingHint = metaFontData.TextRenderingHint;
        }



        // -------------------- Stuff We're Not Using Yet -------------------- //

        #region Experimental Things

        private void MessingAround()
        {

            // Color avg = Color.Black;

            //using (Bitmap bmp =
            //  new Bitmap(pbMainPhoto.ClientSize.Width, pbMainPhoto.ClientSize.Height))
            //{
            //    pbMainPhoto.DrawToBitmap(bmp, pbMainPhoto.ClientRectangle);
            //    // bmp.Save(yourfilename.png);
            //    avg = CalculateAverageColor(bmp, 20, 20, 200, 30);
            //    //avg = CalculateAverageColor(bmp);
            //}

            //        // Point pointy = new Point(DisplayContainer.Left, DisplayContainer.Top);
            //        //if ((NativeMethods.ClientToScreen(this.Handle, ref pointy)) && (pointy != null))
            //        //{
            //        //    using (Graphics graphics = Graphics.FromImage(bm))
            //        //    {
            //        //        graphics.CopyFromScreen(pointy.X, pointy.Y, 0, 0, new Size(DisplayContainer.Width, DisplayContainer.Height), CopyPixelOperation.SourceCopy);
            //        //        clr = CalculateAverageColor(bm);

            //Point pointy = new Point(pbMainPhoto.Left, pbMainPhoto.Top);
            //Bitmap bm = new Bitmap(400,90,pbMainPhoto.Image.PixelFormat);
            //if ((NativeMethods.ClientToScreen(this.Handle, ref pointy)) && (pointy != null))
            //{
            //    using (Graphics graphics = Graphics.FromImage(bm))
            //    {
            //        graphics.CopyFromScreen(pointy.X, pointy.Y, 0, 0, new Size(400, 90), CopyPixelOperation.SourceCopy);
            //        avg = CalculateAverageColor(bm);
            //        Logging.LogLineIf("AverageColor was: " + avg.ToString());
            //    }
            //}
            //else
            //{
            //    Logging.LogLineIf("ClientToScreen(): failed or was null.");
            //}
        }

        private Color GetContrastingFontColor(Color AverageColorOfBitmap, List<Color> FavoriteColors)
        {
            bool fDebugOutput = true;
            bool fDebugOutputTraceLevel = false;
            bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

            // float brightness = AverageColorOfBitmap.GetBrightness();
            float CDiff = (float)500;
            float BDiff = (float).125;

            Logging.LogLineIf(fDebugTrace, "GetContrastingFontColor(): Entered.");

            IEnumerable<Color> AcceptableColors = new List<Color>();

            // In retail, we should use this code

            //AcceptableColors =
            //    (IEnumerable<Color>)FavoriteColors.Where(clr =>
            //        (WithinColorDifferenceRange(AverageColorOfBitmap, clr, CDiff) &&
            //        (WithinBrightnessDifferenceRange(AverageColorOfBitmap, clr, BDiff)))).
            //        OrderBy(clr => GetColorDifference(AverageColorOfBitmap, clr));

            List<Color> temp;
            // temp = AcceptableColors.ToList();

            // In DEBUG, we should use this code, as it's instrumented

            AcceptableColors =
                (IEnumerable<Color>)FavoriteColors.Where(clr =>
                    (WithinColorDifferenceRange(AverageColorOfBitmap, clr, CDiff)));

            temp = AcceptableColors.ToList();
            Logging.LogLineIf(fDebugTrace, "  Count of colors passing GetColorDifference: " + temp.Count);

            AcceptableColors = temp.Where(clr => WithinBrightnessDifferenceRange(AverageColorOfBitmap, clr, BDiff));

            temp = AcceptableColors.ToList();
            Logging.LogLineIf(fDebugTrace, "  ...and then passing GetBrightness: " + temp.Count);

            // TODO: figure out a good order that gives "best" result
            AcceptableColors = temp.OrderBy(c => GetColorDifference(AverageColorOfBitmap, c));
            temp = AcceptableColors.ToList();

            Logging.LogLineIf(fDebugTrace, "  List of AcceptableColors: ");
            foreach (Color c in temp)
            {
                Logging.LogLineIf(fDebugTrace, "          " + c.Name + ", CDiff = " + GetColorDifference(AverageColorOfBitmap, c) +
                    ", BDiff = " + GetBrightnessDifference(AverageColorOfBitmap, c));
            }

            return AcceptableColors.DefaultIfEmpty(Color.Aqua).First();

        }

        private bool WithinBrightnessDifferenceRange(Color avg, Color proposed, float desiredDifference)
        {
            return GetBrightnessDifference(avg, proposed) > desiredDifference;
        }

        private bool WithinColorDifferenceRange(Color avg, Color proposed, float desiredDifference)
        {

            return GetColorDifference(avg, proposed) > desiredDifference;
        }

        private float GetBrightnessDifference(Color avg, Color proposed)
        {
            float result = Math.Abs(proposed.GetBrightness() - avg.GetBrightness());
            // Logging.LogLineIf("     Proposed color: " + proposed.Name + ", Brightness Difference: " + result.ToString());
            return result;
        }

        private float GetColorDifference(Color avg, Color proposed)
        {
            float r1 = Convert.ToSingle(MaxByte(Color.Red, avg, proposed));
            float r2 = Convert.ToSingle(MinByte(Color.Red, avg, proposed));
            float r3 = Convert.ToSingle(MaxByte(Color.Green, avg, proposed));
            float r4 = Convert.ToSingle(MinByte(Color.Green, avg, proposed));
            float r5 = Convert.ToSingle(MaxByte(Color.Blue, avg, proposed));
            float r6 = Convert.ToSingle(MinByte(Color.Blue, avg, proposed));

            float result = Math.Abs((r1 - r2) + (r3 - r4) + (r5 - r6));
            // Logging.LogLineIf("     Proposed color: " + proposed.Name + ", Color Difference: " + result.ToString());

            return result;
        }

        private byte MaxByte(Color rgb, Color x, Color y)
        {
            if (rgb == Color.Red) return (x.R >= y.R) ? x.R : y.R;
            if (rgb == Color.Green) return (x.G >= y.G) ? x.G : y.G;
            if (rgb == Color.Blue) return (x.B >= y.B) ? x.B : y.B;
            return byte.MinValue;
        }

        private byte MinByte(Color rgb, Color x, Color y)
        {
            if (rgb == Color.Red) return (x.R <= y.R) ? x.R : y.R;
            if (rgb == Color.Green) return (x.G <= y.G) ? x.G : y.G;
            if (rgb == Color.Blue) return (x.B <= y.B) ? x.B : y.B;
            return byte.MinValue;
        }

        private bool TryDeleteFile(string target)
        {
            bool fFileDeleted = false;

            try
            {
                File.Delete(target);
                fFileDeleted = true;
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(true, "Exception trying to delete file: " + target + ", Exception: " + ex.Message);

                MessageBox.Show("The file could not be deleted at this time:" + Environment.NewLine + Environment.NewLine +
                "Filename:  " + target, ProductName + " -- Delete A File... For Real", MessageBoxButtons.OK, MessageBoxIcon.Error);
                fFileDeleted = false;
            }

            return fFileDeleted;
        }

        private Color CalculateAverageColor(Bitmap bm)
        {
            int width = bm.Width;
            int height = bm.Height;
            int red = 0;
            int green = 0;
            int blue = 0;
            int minDiversion = 0; // drop pixels that do not differ by at least minDiversion between color values (white, gray or black)
            int dropped = 0; // keep track of dropped pixels
            long[] totals = new long[] { 0, 0, 0 };
            int bppModifier = bm.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images

            BitmapData srcData = bm.LockBits(new System.Drawing.Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            int stride = srcData.Stride;
            IntPtr Scan0 = srcData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = (y * stride) + x * bppModifier;
                        red = p[idx + 2];
                        green = p[idx + 1];
                        blue = p[idx];
                        if (Math.Abs(red - green) > minDiversion || Math.Abs(red - blue) > minDiversion || Math.Abs(green - blue) > minDiversion)
                        {
                            totals[2] += red;
                            totals[1] += green;
                            totals[0] += blue;
                        }
                        else
                        {
                            dropped++;
                        }
                    }
                }
            }

            int avgR = 0;
            int avgG = 0;
            int avgB = 0;

            int count = width * height - dropped;
            if (count != 0)
            {
                avgR = (int)(totals[2] / count);
                avgG = (int)(totals[1] / count);
                avgB = (int)(totals[0] / count);
            }
            else
            {
                Logging.LogLineIf(true, "CalculateAverageColor(): bad bitmap? Count was zero, returning black.");
            }

            bm.UnlockBits(srcData);

            return System.Drawing.Color.FromArgb(avgR, avgG, avgB);
        }

        private Color CalculateAverageColor(Bitmap bm, int left, int top, int right, int bottom)
        {
            int width = right - left;
            int height = top - bottom;
            int red = 0;
            int green = 0;
            int blue = 0;
            int minDiversion = 0; // drop pixels that do not differ by at least minDiversion between color values (white, gray or black)
            int dropped = 0; // keep track of dropped pixels
            long[] totals = new long[] { 0, 0, 0 };
            int bppModifier = bm.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images

            BitmapData srcData = bm.LockBits(new System.Drawing.Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            int stride = srcData.Stride;
            IntPtr Scan0 = srcData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = top; y < height; y++)
                {
                    for (int x = left; x < width; x++)
                    {
                        int idx = (y * stride) + x * bppModifier;
                        red = p[idx + 2];
                        green = p[idx + 1];
                        blue = p[idx];
                        if (Math.Abs(red - green) > minDiversion || Math.Abs(red - blue) > minDiversion || Math.Abs(green - blue) > minDiversion)
                        {
                            totals[2] += red;
                            totals[1] += green;
                            totals[0] += blue;
                        }
                        else
                        {
                            dropped++;
                        }
                    }
                }
            }

            int avgR = 0;
            int avgG = 0;
            int avgB = 0;

            int count = width * height - dropped;
            if (count != 0)
            {
                avgR = (int)(totals[2] / count);
                avgG = (int)(totals[1] / count);
                avgB = (int)(totals[0] / count);
            }
            else
            {
                Logging.LogLineIf(true, "CalculateAverageColor(): bad bitmap? Count was zero, returning black.");
            }

            bm.UnlockBits(srcData);

            return System.Drawing.Color.FromArgb(avgR, avgG, avgB);
        }

        #endregion Experimental Things

        #endregion Methods


    }
}
