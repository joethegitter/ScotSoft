using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver.DebugUtils;
using ScotSoft.PattySaver;


namespace SingleInstanceScreenSaver
{
    public partial class CP_SI_Controller : Form
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

        // hWnd to be passed to Preview constructor
        public IntPtr hWndForCPPreview = IntPtr.Zero;


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams ret = base.CreateParams;
                ret.Style = (int)NativeMethods.WindowStyles.WS_THICKFRAME; // |
                   // (int)NativeMethods.WindowStyles.WS_CHILD;
                ret.ExStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE |
                   (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                return ret;
            }
        } 

        public CP_SI_Controller(IntPtr hWndForPreview)
        {
            fConstructorIsRunning = true;
            fDebugTrace = fDebugOuput && fDebugOutputAtTraceLevel;
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller.ctor(): entered.");

            InitializeComponent();

            hWndForCPPreview = hWndForPreview;

            this.GotFocus += CP_SI_Controller_GotFocus;
            this.HandleCreated += CP_SI_Controller_HandleCreated;
            this.HandleDestroyed += CP_SI_Controller_HandleDestroyed;
            this.LostFocus += CP_SI_Controller_LostFocus;

            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller.ctor(): exiting.");
        }

        protected override void WndProc(ref Message m)
        {
            bool fverbose = true;
            bool fblastme = fDebugTrace && fverbose;

            // if fblastme, spew out every message we receive, unless on ignore list
            if (!msgsToIgnore.Contains<int>(m.Msg))
            {
                Logging.LogLineIf(fblastme, "  --> Controller.WndProc() " + m.Msg.ToString() + ": " + m.ToString());
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

        private void CP_SI_Controller_Load(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Load(): entered.");

            // create and show the preview form
            EntryPoint.ShowMiniPreview(hWndForCPPreview);

            // Start a timer, so we can (optionally) show the debug window AFTER we've already shown the form
            if (EntryPoint.fPopUpDebugOutputWindowOnTimer)
            {
                Logging.LogLineIf(fDebugTrace, "  CP_SI_Controller_Load(): Starting timer to pop up debug output window:");
                tock = new Timer();
                tock.Interval = 3000;       // 3 seconds
                tock.Tick += tock_Tick;     // bind the event handler
                tock.Start();
            }

            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Load(): exiting.");
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


        private void CP_SI_Controller_Shown(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Shown(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Shown(): exiting.");
        }

        private void CP_SI_Controller_Paint(object sender, PaintEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Paint(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Paint(): exiting.");
        }

        private void CP_SI_Controller_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_FormClosing(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_FormClosing(): exiting.");
        }

        private void CP_SI_Controller_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_FormClosed(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_FormClosed(): exiting.");
        }

        private void CP_SI_Controller_StyleChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_StyleChanged(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_StyleChanged(): exiting.");
        }

        private void CP_SI_Controller_EnabledChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_EnabledChanged(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_EnabledChanged(): exiting.");
        }

        private void CP_SI_Controller_LocationChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_LocationChanged(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_LocationChanged(): exiting.");
        }

        private void CP_SI_Controller_VisibleChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_VisibleChanged(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_VisibleChanged(): exiting.");
        }

        private void CP_SI_Controller_Activated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Activated(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Activated(): exiting.");
        }

        private void CP_SI_Controller_Deactivate(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Deactivate(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Deactivate(): exiting.");
        }

        private void CP_SI_Controller_Enter(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Enter(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Enter(): exiting.");
        }

        private void CP_SI_Controller_Leave(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Leave(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_Leave(): exiting.");
        }

        void CP_SI_Controller_LostFocus(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_LostFocus(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_LostFocus(): exiting.");
        }

        void CP_SI_Controller_HandleDestroyed(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_HandleDestroyed(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_HandleDestroyed(): exiting.");
        }

        void CP_SI_Controller_HandleCreated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_HandleCreated(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_HandleCreated(): exiting.");
        }

        void CP_SI_Controller_GotFocus(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_GotFocus(): entered.");
            Logging.LogLineIf(fDebugTrace, "CP_SI_Controller_GotFocus(): exiting.");
        }
    }
}
