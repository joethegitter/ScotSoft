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

        // States
        bool fInConstructor = false;
        bool fConstructorHasCompleted = false;
        bool fInFormLoad = false;
        bool fFormLoadHasCompleted = false;
        Timer tock;
        int tockCount = 0;

        // Slideshow
        MainFileInfoSource MainFiles;
        Timer miniSlideshowTimer;
        Image currentImage = null;
        bool fShowingEmbeddedImage = false;

        // hWnd of window to make our parent window
        IntPtr iphWnd;      // value is passed in the form constructor

        // debug output window
        ScrollingTextWindow debugOutputWindow = null;

        #endregion Fields


        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hWnd">IntPtr conversion of long-based hWnd passed to us by Windows, along with the /p parameter.</param>
        public miniControlPanelForm(IntPtr hWnd)            // get the stored config data

        {
            fInConstructor = true;

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

            // add msgs to ignore list
            // #define WM_MOUSEMOVE                    0x0200
            // #define WM_NCMOUSEHOVER                 0x02A0
            // #define WM_MOUSEHOVER                   0x02A1
            // #define WM_MOUSELEAVE                   0x02A3
            // #define WM_NCHITTEST                    0x0084
            // #define WM_NCMOUSELEAVE                 0x02A2
            // #define WM_NCMOUSEMOVE                  0x00A0

            msgsToIgnore.Add((int)0x0200);
            msgsToIgnore.Add((int)0x02A0);
            msgsToIgnore.Add((int)0x02A1);
            msgsToIgnore.Add((int)0x02A3);
            msgsToIgnore.Add((int)0x0084);
            msgsToIgnore.Add((int)0x02A2);
            msgsToIgnore.Add((int)0x00A0);
            msgsToIgnore.Add((int)0x0020);   // WM_SETCURSOR

            // get the stored (or newly initialized) settings info
            Logging.LogLineIf(fDebugTrace, "   miniControlPanelForm.ctor(): calling InitializeAndLoadConfigSettingsFromStorage()");
            SettingsInfo.InitializeAndLoadConfigSettingsFromStorage();            // Load configutation data from disk

            fInConstructor = false;
            fConstructorHasCompleted = true;

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm.ctor(): exiting.");
        }

        #endregion Constructor


        #region Form Events and Overrides

        /// <summary>
        /// Override of WndProc, so we can detect WM_DESTROY messages sent to
        /// our form and quit the Application.
        /// </summary>
        /// <param name="m">Message being sent to our Window.</param>
        /// <remarks>This is required because when a form is used to present 
        /// the "mini-preview" window in the Screen Saver Control Panel, that
        /// form does NOT receive the normal Close, Closed, Closing events
        /// expected in a .Net app. Instead, Windows sends WM_DESTROY messages 
        /// to the form each time it removes that window from the Control Panel. 
        /// So we close the app each time that occurs.</remarks>
        protected override void WndProc(ref Message m)
        {
            bool fverbose = false;
            bool fblastme = fDebugTrace && fverbose;

            // #define WM_ACTIVATEAPP                  0x001C
            // #define WM_CHILDACTIVATE                0x0022
            // #define WM_SHOWWINDOW                   0x0018


            // if fblastme, spew out every message we receive, unless on ignore list
            if (!msgsToIgnore.Contains<int>(m.Msg))
            {
                Logging.LogLineIf(fblastme, "  --> " + m.Msg.ToString() + ": " + m.ToString());
            }

            // if we receive WM_DESTROY, close the form, where we'll quit the app
            if (m.Msg == (int)0x0002)   // WM_DESTROY
            {
                this.Close();
                return;
            }

            // if we didn't handle it, let the base class handle it
            base.WndProc(ref m);
        }

        /// <summary>
        /// Override of ShowWithoutActivation property.  In theory, this tells
        /// Windows that when our window gets shown, it should not receive activation.
        /// </summary>
        protected override bool ShowWithoutActivation
        {
            get
            {
                Logging.LogLineIf(fDebugTrace, "ShowWithoutActivation property accessed, returning true.");
                return true;
            }
        }

        /// <summary>
        /// This code runs after constructor, but before form is displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniControlPanelForm_Load(object sender, EventArgs e)
        {
            // set state flags
            fInFormLoad = true;

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Load(): entered.");

            // Now we make our form a child window of the Control Panel window
            // hWnd passed to us in the constructor.

            // Set our window style to WS_CHILD, so that our window is 
            // destroyed when parent window is destroyed. Start by getting
            // the value which represents the current window style, and modifying
            // that value to include WS_CHILD.
            IntPtr ip = new IntPtr();
            int index = (int)NativeMethods.WindowLongFlags.GWL_STYLE | 0x40000000;
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): About to call GetWindowLongPtr:");
            ip = NativeMethods.GetWindowLongPtr(this.Handle, index);
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): GetWindowLongPtr returned IntPtr: " + ip.ToString() + ", GetLastError() returned: " + error.ToString());

            // Now use that value to set our window style.
            object ohRef = new object();
            HandleRef hRef = new HandleRef(ohRef, this.Handle);
            IntPtr ip2 = new IntPtr();
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): About to call SetWindowLongPtr:");
            index = (int)NativeMethods.WindowLongFlags.GWL_STYLE;
            ip2 = NativeMethods.SetWindowLongPtr(hRef, index, ip);
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): SetWindowLongPtr returned IntPtr: " + ip2.ToString() + ", GetLastError() returned: " + error.ToString());

            // Now make the passed hWnd our parent window.
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Calling SetParent to set new Parent for our form:");
            NativeMethods.SetLastErrorEx(0, 0);
            IntPtr newOldParent = NativeMethods.SetParent(this.Handle, iphWnd);
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): SetParent() returned IntPtr = " + newOldParent.ToString() + ", GetLastError() returned: " + error.ToString());

            // Set our window's size to the size of our window's new parent.
            // First, get that size.
            Rectangle ParentRect = new Rectangle();
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Calling GetClientRect to get a new rect for our form:");
            bool fSuccecss = NativeMethods.GetClientRect(iphWnd, ref ParentRect);
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): GetClientRect() returned bool = " + fSuccecss + ", GetLastError() returned: " + error.ToString());

            // Set our size to new rect and location at (0, 0)
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Setting Size and Position:");
            this.Size = ParentRect.Size;
            this.Location = new Point(0, 0);

            // Start a timer, so we can (optionally) show the debug window AFTER we've already shown the form
            if (LaunchManager.Modes.fPopUpDebugOutputWindowOnTimer)
            {
                Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Starting timer to pop up debug output window:");
                tock = new Timer();
                tock.Interval = 6000;       // six seconds
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
            fInFormLoad = false;
            fFormLoadHasCompleted = true;
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): exiting.");
        }

        private void MiniPreviewDrawImageFile()
        {
            // Because the mini-preview is not interactive, we don't need to use DrawNextImage and DrawPreviousImage,
            // or keep a history of files shown. So, we use this stripped down version of DrawImageFile, instead of 
            // what FullScreen uses.

            // Get the filename
            string filename = MainFiles.GetNextFile().FullName;

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
            Logging.LogLineIf(fDebugTrace, "   tock_Tick(): creating debugOutputWindow:");

            debugOutputWindow = new ScrollingTextWindow(this);
            debugOutputWindow.CopyTextToClipboardOnClose = true;
            debugOutputWindow.ShowDisplay();

            Logging.LogLineIf(fDebugTrace, "  tock_Tick(): Killing timer.");

            tock.Stop();
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
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): entered.");

            // We want to avoid our little form getting "focus" or stealing the
            // Activated look from the Control Panel. So, first time we are shown,
            // immediately set our parent window to be the foreground window
            bool fSuccess = false;
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Shown(): About to call SetForegroundWindow:");
            fSuccess = NativeMethods.SetForegroundWindow(iphWnd);
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Shown(): SetForegroundWindow returned bool: " + fSuccess + ", GetLastError() returned: " + error.ToString());

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): exiting.");
        }

        private void miniControlPanelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
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
        }

        private void miniControlPanelForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): entered, calling Application.Exit()");
            Application.Exit();
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): exiting.");
        }

        private void miniControlPanelForm_Activated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Activated(): entering.");

            if (fConstructorHasCompleted && fFormLoadHasCompleted)
            {
                bool fSuccess = false;
                NativeMethods.SetLastErrorEx(0, 0);

                Logging.LogLineIf(fDebugTrace, "About to call SetForegroundWindow:");
                fSuccess = NativeMethods.SetForegroundWindow(iphWnd);
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "SetForegroundWindow returned bool: " + fSuccess + ", GetLastError() returned: " + error.ToString());
            }
            else
            {
                Logging.LogLineIf(fDebugTrace, "   Doing nothing, fConstructorHasCompleted = " + fConstructorHasCompleted + ", fFormLoadHasCompleted = " + fFormLoadHasCompleted);
            }

            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Activated(): exiting.");
        }

        private void miniControlPanelForm_Deactivate(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Deactivate(): entered.");
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Deactivate(): exiting.");
        }

        #endregion Form Events and Overrides
    }
}
