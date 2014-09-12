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

namespace JKSoft
{
    public partial class CP_Configuration : Form
    {
        public bool fDebugOutput = true;
        public bool fDebugAtTraceLevel = true;
        public bool fTrace = false;     // calculated in constructor
        List<int> msgsToIgnore = new List<int>();

        // states
        bool fInitializeComponentHasCompleted = false;

        bool fWndProcSentCloseCommandBecauseWMDESTROY = false;
        bool fWndProcSentCloseCommandBecauseWMCLOSE = false;

        bool fCloseMethodHasBeenCalled = false;

        bool fClosingEventHandlerIsRunning = false;
        bool fClosingEventHandlerHasCompleted = false;

        bool fClosedEventHandlerIsRunning = false;
        bool fClosedEventHandlerHasCompleted = false;



        public Timer tock = null;
        public ScrollingTextWindow debugOutputWindow = null;

        public bool fShownHandlerHasCompleted = false;
        public Point lastFormLocation = new Point(0, 0);

        public IntPtr passedWnd = IntPtr.Zero;



        public CP_Configuration()
        {
            InitializeComponent();
        }

        private void CP_Configuration_Load(object sender, EventArgs e)
        {

            Logging.LogLineIf(fTrace, "CP_Configuration_Load(): entered.");

            if (EntryPoint.fShowDebugOutputWindow)
            {
                tock = new Timer();
                tock.Interval = 4000;
                tock.Tick += tock_Tick;
                tock.Start();
            }

            Logging.LogLineIf(fTrace, "CP_Configuration_Load(): exiting.");
        }

        void tock_Tick(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "tock_Tick(): entered.");
            Logging.LogLineIf(fTrace, "   tock_Tick(): creating debugOutputWindow:");

            debugOutputWindow = new ScrollingTextWindow(this);
            debugOutputWindow.CopyTextToClipboardOnClose = true;
            debugOutputWindow.ShowDisplay();

            Logging.LogLineIf(fTrace, "  tock_Tick(): Killing timer.");

            tock.Stop();
            tock.Tick -= tock_Tick;
            tock.Dispose();
            tock = null;

            Logging.LogLineIf(fTrace, "tock_Tick(): exiting.");


        }
    }
}
