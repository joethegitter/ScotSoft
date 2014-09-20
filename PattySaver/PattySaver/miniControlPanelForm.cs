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
using JoeKCo.Utilities;
using JoeKCo.Utilities.Debug;

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

        // Slideshows
        MainFileInfoSource MainFiles;
        public string[] GraphicFileExtensions =          // File extensions that we'll allow in our app
            new string[] { ".png", ".bmp", ".gif", ".jpg", ".jpeg" };
        Timer miniSlideshowTimer;
        Image currentImage = null;
        bool fShowingEmbeddedImage = false;

        // hWnd of window to make our parent window
        IntPtr iphWnd;      // value is passed in the form constructor

        delegate void myDel();

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
                //case ((int)0x0002):   // WM_DESTROY - this is now "Just In Case".  We may not need it at all.
                //    if (!this.Disposing && !this.IsDisposed && fShownHandlerHasCompleted && !fClosingHandlerIsRunning && !fClosingHandlerHasCompleted)
                //    {
                //        Logging.LogLineIf(fDebugTrace, "* WndProc(): WM_DESTROY received. Calling this.Close().");

                //        this.Close();
                //        return;
                //    }
                //    break;

                default:
                    base.WndProc(ref m);
                    break;

            }
        }

        /// <summary>
        /// Override of the CreateParams property. We override so we can add "WS_CHILD" to the Style each
        /// time it is queried.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                Logging.LogLineIf(fDebugTrace, "CreateParams Property (override): get accessor entered.");

                // get the base params
                CreateParams cp = base.CreateParams;
                Logging.LogLineIf(fDebugTrace, "   CreateParams Property (override): base style equals: " + Logging.DecNHex(cp.Style));

                // modify base params
                cp.Style |= NativeMethods.WindowStyles.WS_CHILD;
                Logging.LogLineIf(fDebugTrace, "   CreateParams Property (override): returning  modified style (base.CreateParams.Style |= WS_CHILD), which equals: " + Logging.DecNHex(cp.Style));

                Logging.LogLineIf(fDebugTrace, "CreateParams Property (override): get accessor exiting.");
                return cp;
            }
        }

        /// <summary>
        /// This code runs after the constructor, but before form is displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_Load(object sender, EventArgs e)
        {
            // set state flags
            fFormLoadHandlerIsRunning = true;

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Load(): entered.");

            // Start a timer, so we can (optionally) show the debug window AFTER we've already shown the form
            if (EntryPoint.fPopUpDebugOutputWindowOnTimer)
            {
                Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Starting timer to pop up debug output window:");
                tock = new Timer();
                tock.Interval = 3000;       // 3 seconds
                tock.Tick += tock_Tick;     // bind the event handler
                tock.Start();
            }

            // Kick off the disk scan, so we can start a slideshow
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): creating MainFiles, should kick off disk scan.");
            MainFiles = new MainFileInfoSource(
                (List<DirectoryInfo>)SettingsInfo.GetListOfDirectoryInfo(),
                this.GraphicFileExtensions, 
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

        /// <summary>
        /// This code runs immediately after the form is displayed for the first time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_Shown(object sender, EventArgs e)
        {
            fShownHandlerIsRunning = true;
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): entered.");

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): exiting.");
            fShownHandlerIsRunning = false;
            fShownHandlerHasCompleted = true;
        }

        /// <summary>
        /// This code runs when the form learns it is about to Close, but before it has Closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            fClosingHandlerIsRunning = true;

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosing(): entered, Reason: " + e.CloseReason.ToString());

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

        /// <summary>
        /// This code runs once the Form has Closed, but before it is Disposed/Destroyed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): entered, calling Application.Exit()");
            Application.Exit();
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): exiting.");
        }

        #endregion Form Events and Overrides


        #region Methods called by Form and Control Events

        /// <summary>
        /// The code used to draw each picture on the mini Form.
        /// </summary>
        private void MiniPreviewDrawImageFile()
        {
            // Because the mini-preview is not interactive, images are only drawn
            // automatically, and going forward through list. So we can use a very
            // stripped down version of our navigation model.

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
                        // dispose of the old image
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
            else    // show the embedded "no image" image
            {
                fShowingEmbeddedImage = true;
                currentImage = PattySaverResources.noimage;
                BackgroundImage = currentImage;
            }
        }

        /// <summary>
        /// Code called when the slide show timer goes off, to draw a new picture.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void miniSlideshowTimer_Tick(object sender, EventArgs e)
        {
            MiniPreviewDrawImageFile();
        }

        /// <summary>
        /// Code called when tock Timer goes off, to create and show the debug output window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void tock_Tick(object sender, EventArgs e)
        {
            // if this method has been called, it's because we want the 
            // debug ouput window to pop up (ie, it can't be called directly
            // because there is no user UX - miniPreview mode, essentially)

            Logging.LogLineIf(fDebugTrace, "tock_Tick(): entered.");
            tock.Stop();

            Logging.LogLineIf(fDebugTrace, "   tock_Tick(): creating debugOutputWindow:");

            debugOutputWindow = new ScrollingTextWindow(this);
            myDel aDel = new myDel(debugOutputWindowHasClosed);
            debugOutputWindow.WindowHasClosedCallBack = aDel;
            debugOutputWindow.CopyWindowTextToClipboardOnClose = true;
            debugOutputWindow.ShowDisplay();

            Logging.LogLineIf(fDebugTrace, "  tock_Tick(): Killing timer.");
            tock.Tick -= tock_Tick;
            tock.Dispose();
            tock = null;

            Logging.LogLineIf(fDebugTrace, "tock_Tick(): exiting.");
        }

        public void debugOutputWindowHasClosed()
        {
            debugOutputWindow = null;
        }

        #endregion Methods called by Form and Control Events

    }
}
