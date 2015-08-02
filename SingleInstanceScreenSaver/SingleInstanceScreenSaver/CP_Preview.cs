using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;

namespace SingleInstanceScreenSaver
{

    public partial class CP_PreviewForm : Form
    {
        // Debug Output controls
        bool fDebugOuput = true;
        bool fDebugOutputAtTraceLevel = true;
        bool fDebugTrace = false;  // do not modify value here, it is set in constructor
        List<int> msgsToIgnore = new List<int>();
        public Timer tock = null;

        // Debug Output window
        public ScrollingTextWindow debugOutputWindow = null;

        // States
        bool fConstructorIsRunning = false;
        bool fConstructorHasCompleted = false;
        bool fFormLoadHandlerIsRunning = false;
        bool fFormLoadHandlerHasCompleted = false;
        bool fClosingHandlerIsRunning = false;
        bool fClosingHandlerHasCompleted = false;
        bool fShownHandlerIsRunning = false;
        bool fShownHandlerHasCompleted = false;

        // hWnd of window to make our parent window
        IntPtr iphWnd;      // value is passed in the form constructor

        public CP_PreviewForm(IntPtr hWnd)
        {
            fConstructorIsRunning = true;
            fDebugTrace = fDebugOuput && fDebugOutputAtTraceLevel;
            Logging.LogLineIf(fDebugTrace, "CP_PreviewForm.ctor(): entered.");

            if (hWnd != null)
            {
                Logging.LogLineIf(fDebugTrace, "   CP_PreviewForm.ctor(): hWnd = " + EntryPoint.DecNHex(hWnd));
            }
            else
            {
                throw new ArgumentNullException("CP_PreviewForm.ctor(): hWnd cannot be null.");
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

            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
            Logging.LogLineIf(fDebugTrace, "CP_PreviewForm.ctor(): exiting.");
        }

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
                //    if (true)
                //        //if (!this.Disposing && !this.IsDisposed && fShownHandlerHasCompleted && !fClosingHandlerIsRunning && !fClosingHandlerHasCompleted)
                //        {
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
                Logging.LogLineIf(fDebugTrace, "   CreateParams Property (override): base style equals: " + EntryPoint.DecNHex(cp.Style));

                // modify base params
                cp.Style |= NativeMethods.WindowStyles.WS_CHILD;
                Logging.LogLineIf(fDebugTrace, "   CreateParams Property (override): returning  modified style (base.CreateParams.Style |= WS_CHILD), which equals: " + EntryPoint.DecNHex(cp.Style));

                Logging.LogLineIf(fDebugTrace, "CreateParams Property (override): get accessor exiting.");
                return cp;
            }
        }

        private void CP_PreviewForm_Load(object sender, EventArgs e)
        {
            // Start a timer, so we can (optionally) show the debug window AFTER we've already shown the form
            //if (EntryPoint.fPopUpDebugOutputWindowOnTimer)
            //{
            //    Logging.LogLineIf(fDebugTrace, "  miniControlPanelForm_Load(): Starting timer to pop up debug output window:");
            //    tock = new Timer();
            //    tock.Interval = 3000;       // 3 seconds
            //    tock.Tick += tock_Tick;     // bind the event handler
            //    tock.Start();
            //}

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
            debugOutputWindow.CopyTextToClipboardOnClose = true;
            debugOutputWindow.ShowDisplay();

            Logging.LogLineIf(fDebugTrace, "  tock_Tick(): Killing timer.");
            tock.Tick -= tock_Tick;
            tock.Dispose();
            tock = null;

            Logging.LogLineIf(fDebugTrace, "tock_Tick(): exiting.");
        }




    }
}
