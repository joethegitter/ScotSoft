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
using System.IO;
using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver
{
    public partial class miniControlPanelForm : Form
    {

        // Scot, this is the tiny form we show in the little computer in the Screen Saver Control Panel.

        #region Fields

        // Debug Output controls
        bool fDebugOuput = true;
        bool fDebugOutputAtTraceLevel = true;
        bool fDebugTrace = false;  // do not modify value here, it is set in constructor
        List<int> msgsToIgnore = new List<int>();
        public Timer tock = null;

        // Debug Output window
        ScrollingTextWindow debugOutputWindow = null;

        // States
        bool fConstructorIsRunning = false;
        bool fConstructorHasCompleted = false;
        bool fFormLoadHandlerIsRunning = false;
        bool fFormLoadHandlerHasCompleted = false;
        bool fClosingHandlerIsRunning = false;
        bool fClosingHandlerHasCompleted = false;
        bool fShownHandlerIsRunning = false;
        bool fShownHandlerHasCompleted = false;

        // Slideshow
        MainFileInfoSource MainFiles;
        Timer miniSlideshowTimer;
        Image currentImage = null;
        bool fShowingEmbeddedImage = false;

        // hWnd of window to make our parent window
        IntPtr iphWnd;      // value is passed in the form constructor


        #endregion Fields


        #region Constructor

        /// <summary>
        /// Constructor for form when used to create Mini Preview Window in Control Panel.
        /// </summary>
        /// <param name="hWnd">IntPtr conversion of long-based hWnd passed to us by Windows, along with the /p parameter.</param>
        public miniControlPanelForm(IntPtr hWnd)

        {
            fConstructorIsRunning = true;
            fDebugTrace = fDebugOuput && fDebugOutputAtTraceLevel;
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm.ctor(): entered.");

            if (hWnd != null)
            {
                Logging.LogLineIf(fDebugTrace, "   miniControlPanelForm.ctor(): hWnd = " + hWnd.ToString());
            }
            else
            {
                throw new ArgumentNullException("miniControlPanelForm.ctor(): hWnd cannot be null.");
            }

            // boilerplate; ignore, but do not remove
            InitializeComponent();                              

            // store away the passed hWnd
            iphWnd = hWnd;

            // make a list of window messages that our debug output code will NOT emit
            msgsToIgnore.Add((int)0x0200);      // WM_MOUSEMOVE                    0x0200
            msgsToIgnore.Add((int)0x02A0);      // WM_NCMOUSEHOVER                 0x02A0
            msgsToIgnore.Add((int)0x02A1);      // WM_MOUSEHOVER                   0x02A1
            msgsToIgnore.Add((int)0x02A3);      // WM_MOUSELEAVE                   0x02A3
            msgsToIgnore.Add((int)0x0084);      // WM_NCHITTEST                    0x0084
            msgsToIgnore.Add((int)0x02A2);      // WM_NCMOUSELEAVE                 0x02A2
            msgsToIgnore.Add((int)0x00A0);      // WM_NCMOUSEMOVE                  0x00A0
            msgsToIgnore.Add((int)0x0020);      // WM_SETCURSOR
            msgsToIgnore.Add((int)0x14);        // (WM_ERASEBKGND)
            msgsToIgnore.Add((int)0xe);         // (WM_GETTEXTLENGTH)
            msgsToIgnore.Add((int)0xd);         // (msg=0xd (WM_GETTEXT))
            msgsToIgnore.Add((int)0xf);         // (0xf (WM_PAINT))

            // get the stored (or newly initialized) settings info
            Logging.LogLineIf(fDebugTrace, "   miniControlPanelForm.ctor(): calling InitializeAndLoadConfigSettingsFromStorage()");
            SettingsInfo.InitializeAndLoadConfigSettingsFromStorage();            // Load configutation data from disk

            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm.ctor(): exiting.");
        }

        #endregion Constructor


        #region Form Events and Overrides

        /// <summary>
        /// Override of WndProc, so we can detect various messages sent to
        /// our form and quit the Application.
        /// </summary>
        /// <param name="m">Message being sent to our Window.</param>
        /// <remarks>This is required because when a form is used to present 
        /// the "mini-preview" window in the Screen Saver Control Panel, that
        /// form does NOT receive the normal Close, Closed, Closing events
        /// expected in a .Net app. Instead, Windows sends WM_DESTROY messages 
        /// to the form each time it removes that window from the Control Panel. 
        /// So we close the app each time that occurs. Note also the work we have
        /// done for NC_PAINT.</remarks>
        protected override void WndProc(ref Message m)
        {
            bool fverbose = true;
            bool fblastme = fDebugTrace && fverbose;

            // if fblastme, spew out every message we receive, unless on ignore list
            if (!msgsToIgnore.Contains<int>(m.Msg))
            {
                Logging.LogLineIf(fblastme, "  --> " + m.Msg.ToString() + ": " + m.ToString());
            }

            switch ((int)m.Msg)
            {
                case ((int)0x0002):   // WM_DESTROY
                    if (!this.Disposing && !this.IsDisposed && fShownHandlerHasCompleted && !fClosingHandlerIsRunning && !fClosingHandlerHasCompleted)
                    {
                        Logging.LogLineIf(fDebugTrace, "* WndProc(): WM_DESTROY received. Calling this.Close().");

                        this.Close();
                        return;
                    }
                    break;

                //case ((int)0x85):     // NC_PAINT - we use this to detect that user has switched sceen savers
                //    if ((int)m.WParam == 1)
                //    {
                //        if (!this.Disposing && !this.IsDisposed && fShownHandlerHasCompleted && !fClosingHandlerIsRunning && !fClosingHandlerHasCompleted)
                //        {
                //            Logging.LogLineIf(fDebugTrace, "Received dreaded NC_PAINT, fleeing in terror.");
                //            this.Close();
                //            return;
                //        }
                //    }
                //    break;

                default:
                    base.WndProc(ref m);
                    break;

            }
        }

        // Override of the CreateParams property, so that we can add "child window" as a Style when queried
        protected override CreateParams CreateParams
        {
            get
            {
                Logging.LogLineIf(fDebugTrace, "CreateParams Property (override): get accessor entered.");

                // get the base params
                CreateParams cp = base.CreateParams;
                Logging.LogLineIf(fDebugTrace, "   CreateParams Property (override): base style equals: " + EntryPoint.DecNHex(cp.Style));

                // modify base params
                cp.Style |= NativeMethods.WindowStyles.WS_CHILD;
                Logging.LogLineIf(fDebugTrace, "   CreateParams Property (override): returning  modified style (base.CreateParams.Style |= WS_CHILD), which equals: " + EntryPoint.DecNHex(cp.Style));

                Logging.LogLineIf(fDebugTrace, "CreateParams Property (override): get accessor exiting.");
                return cp;
            }
        }


        ///// <summary>
        ///// Override of ShowWithoutActivation property.  In theory, this tells
        ///// Windows that when our window gets shown, it should not receive activation.
        ///// </summary>
        //protected override bool ShowWithoutActivation
        //{
        //    get
        //    {
        //        Logging.LogLineIf(fDebugTrace, "ShowWithoutActivation property accessed, returning true.");
        //        return true;
        //    }
        //}

        /// <summary>
        /// This code runs after constructor, but before form is displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_Load(object sender, EventArgs e)
        {
            // set state flags
            fFormLoadHandlerIsRunning = true;

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Load(): entered.");

            // Start a timer, so we can (optionally) show the debug window AFTER we've already shown the form
            if (LaunchManager.Modes.fPopUpDebugOutputWindowOnTimer)
            {
                Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Starting timer to pop up debug output window:");
                tock = new Timer();
                tock.Interval = 3000;       // 3 seconds
                tock.Tick += tock_Tick;     // bind the event handler
                tock.Start();
            }

            // Kick off the disk scan, so we can start a slideshow
            Logging.LogLineIf(fDebugTrace, "ScreenSaverForm_Load(): creating MainFiles, should kick off disk scan.");
            MainFiles = new MainFileInfoSource(
                (List<DirectoryInfo>)SettingsInfo.GetListOfDirectoryInfo(),
                SettingsInfo.GetBlacklistedFullFilenames(),
                SettingsInfo.UseRecursion,
                SettingsInfo.ShuffleMode);

            // Format the background, and set the first image
            this.BackgroundImageLayout = ImageLayout.Zoom;
            MiniPreviewDrawImageFile();

            // Start a timer for the slideshow
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Starting timer:");
            miniSlideshowTimer = new Timer();
            miniSlideshowTimer.Interval = 3000;                     // 3  seconds
            miniSlideshowTimer.Tick += miniSlideshowTimer_Tick;     // bind the event handler
            miniSlideshowTimer.Start();

            // clear or set state flags
            fFormLoadHandlerIsRunning = false;
            fFormLoadHandlerHasCompleted = true;
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): exiting.");
        }

        private void MiniPreviewDrawImageFile()
        {
            // Because the mini-preview is not interactive, we don't need to use DrawNextImage and DrawPreviousImage,
            // or keep a history of files shown. So, we use this stripped down version of DrawImageFile, instead of 
            // what FullScreen uses.

            // Get the filename
            string filename = null;
            FileInfo fi = MainFiles.GetNextFile();
            if (fi != null)
            {
                filename = fi.FullName;
            }

            if (filename != null)           // if null, then there were no graphics files in any of the users directories
            {
                if (File.Exists(filename)) // if file missing or filename was mangled, just do nothing, wait for next filename
                {
                    if (currentImage != null && !fShowingEmbeddedImage) currentImage.Dispose();

                    try
                    {
                        if (currentImage != null) currentImage.Dispose();

                        // generate the image and store it in a private field
                        currentImage = Image.FromFile(filename);

                        // point picturebox at that field
                        BackgroundImage = currentImage;
                    }
                    catch (Exception)
                    {
                        // Do nothing
                    }
                }
            }
            else    // For now, do nothing. We may show the embedded "noimage" resource later
            {
                fShowingEmbeddedImage = true;
                currentImage = PattySaverResources.noimage;
                BackgroundImage = currentImage;
            }
        }

        void miniSlideshowTimer_Tick(object sender, EventArgs e)
        {
            MiniPreviewDrawImageFile();
        }

        void tock_Tick(object sender, EventArgs e)
        {
            // if this method has been called, it's because we want the 
            // debug ouput window to pop up (ie, it can't be called directly
            // because there is no user UX - miniPreview mode, essentially)

            Logging.LogLineIf(fDebugTrace, "tock_Tick(): entered.");
            tock.Stop();

            Logging.LogLineIf(fDebugTrace, "   tock_Tick(): creating debugOutputWindow:");

            debugOutputWindow = new ScrollingTextWindow(this);
            debugOutputWindow.CopyTextToClipboardOnClose = true;
            debugOutputWindow.ShowDisplay();

            Logging.LogLineIf(fDebugTrace, "  tock_Tick(): Killing timer.");
            tock.Tick -= tock_Tick;
            tock.Dispose();
            tock = null;

            Logging.LogLineIf(fDebugTrace, "tock_Tick(): exiting.");
        }

        /// <summary>
        /// This event occurs immediately after the form is displayed for the first time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_Shown(object sender, EventArgs e)
        {
            fShownHandlerIsRunning = true;
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): entered.");

            //// We want to avoid our little form getting "focus" or stealing the
            //// Activated look from the Control Panel. So, first time we are shown,
            //// immediately set our parent window to be the foreground window
            //bool fSuccess = false;
            //NativeMethods.SetLastErrorEx(0, 0);
            //Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Shown(): About to call SetForegroundWindow:");
            //fSuccess = NativeMethods.SetForegroundWindow(iphWnd);
            //int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Shown(): SetForegroundWindow returned bool: " + fSuccess + ", GetLastError() returned: " + error.ToString());

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): exiting.");

            fShownHandlerIsRunning = false;
            fShownHandlerHasCompleted = true;
        }

        private void miniControlPanelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            fClosingHandlerIsRunning = true;

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosing(): entered, disposing of timers.");

            if (tock != null)
            {
                tock.Stop();                // stop the timer
                tock.Tick -= tock_Tick;     // unbind the tick event handler
                tock.Dispose();             // dispose of the timer
            }

            if (miniSlideshowTimer != null)
            {
                miniSlideshowTimer.Stop();
                miniSlideshowTimer.Tick -= miniSlideshowTimer_Tick;
                miniSlideshowTimer.Dispose();
            }

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosing(): exiting.");
            fClosingHandlerHasCompleted = true;
            fClosingHandlerIsRunning = false;
        }

        private void miniControlPanelForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): entered, calling Application.Exit()");
            Application.Exit();
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): exiting.");
        }

        //private void miniControlPanelForm_Activated(object sender, EventArgs e)
        //{
        //    Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Activated(): entering.");

        //    if (fConstructorHasCompleted && fFormLoadHandlerHasCompleted && fShownHandlerHasCompleted)
        //    {
        //        bool fSuccess = false;
        //        NativeMethods.SetLastErrorEx(0, 0);

        //        Logging.LogLineIf(fDebugTrace, "About to call SetForegroundWindow:");
        //        fSuccess = NativeMethods.SetForegroundWindow(iphWnd);
        //        int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        //        Logging.LogLineIf(fDebugTrace, "SetForegroundWindow returned bool: " + fSuccess + ", GetLastError() returned: " + error.ToString());
        //    }
        //    else
        //    {
        //        Logging.LogLineIf(fDebugTrace, "   Doing nothing, because fConstructorHasCompleted = " + 
        //            fConstructorHasCompleted + ", fFormLoadHasCompleted = " + fFormLoadHandlerHasCompleted +
        //            ", fShownHandlerHasCompleted = fShownHandlerHasCompleted" + fShownHandlerHasCompleted);
        //    }

        //    Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Activated(): exiting.");
        //}

        //private void miniControlPanelForm_Deactivate(object sender, EventArgs e)
        //{
        //    Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Deactivate(): entered.");
        //    Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Deactivate(): exiting.");
        //}

        #endregion Form Events and Overrides
    }
}
