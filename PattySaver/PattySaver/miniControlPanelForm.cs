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
        bool fDebugOuput = true;
        bool fDebugOutputAtTraceLevel = true;
        bool fDebugTrace = false;  // do not modify here, this is set in constructor or createHandle event

        //Timer tock;
        //int tockCount = 0;

        IntPtr iphWnd;      // will be passed in the form constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hWnd">IntPtr conversion of long-based hWnd passed to us by Windows, along with the /p parameter.</param>
        public miniControlPanelForm(IntPtr hWnd)
        {
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

            InitializeComponent();
            iphWnd = hWnd;

            // temporarily, provide us with a scrolling text window for debug output
            Logging.ShowHideDebugWindow(this);
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm.ctor: exited.");
        }

        /// <summary>
        /// Override of WndProc, so we can detect WM_DESTROY messages sent to our form and quit the Application.
        /// </summary>
        /// <param name="m">Message being sent to our Window.</param>
        /// <remarks>This is required because when a form is used to present the "mini-preview" window in the
        /// Screen Saver Control Panel, that form does NOT receive the normal Close, Closed, Closing events
        /// expected in a .Net app.  Instead, Windows sends WM_DESTROY messages to the form each time it removes
        /// that window from the Control Panel. So we close the app each time that occurs.</remarks>
        protected override void WndProc(ref Message m)
        {
            Logging.LogLineIf(fDebugTrace , m.Msg.ToString() + ": " + m.ToString());

            if (m.Msg == (int)0x0002)   // WM_DESTROY
            {
                Application.Exit();
                return;
            }

            base.WndProc(ref m);
        }


        private void miniControlPanelForm_Load(object sender, EventArgs e)
        {
            // Things we've been investigating...
            // this.OnParentVisibleChanged
            // this.OnParentChanged;
            // this.OnNotifyMessage;
            // this.OnHandleDestroyed;
            // this.HandleDestroyed;
            // this.DefWndProc;

            //// start a timer, so we can check if we need to destroy ourselves
            //tock = new Timer();
            //tock.Interval = 10000;      // ten seconds
            //tock.Tick += tock_Tick;     // destruction code in that event handler
            //tock.Start();

            // set our window style to WS_CHILD, so that our window is destroyed when parent window is destroyed.
            // get the current window style, but with WS_CHILD set
            IntPtr ip = new IntPtr();
            int index = (int)NativeMethods.WindowLongFlags.GWL_STYLE | 0x40000000;   
            Logging.LogLineIf(fDebugTrace, "About to call GetWindowLongPtr:");
            ip = NativeMethods.GetWindowLongPtr(this.Handle, index);
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "GetWindowLongPtr returned IntPtr: " + ip.ToString() + ", GetLastError() returned: " + error.ToString());

            // set that value as our current Style
            object ohRef = new object();
            HandleRef hRef = new HandleRef(ohRef, this.Handle);
            IntPtr ip2 = new IntPtr();
            Logging.LogLineIf(fDebugTrace, "About to call SetWindowLongPtr:");
            int index2 = (int)NativeMethods.WindowLongFlags.GWL_STYLE;
            ip2 = NativeMethods.SetWindowLongPtr(hRef, index2, ip);
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "SetWindowLongPtr returned IntPtr: " + ip2.ToString() + ", GetLastError() returned: " + error.ToString());

            //set the passed preview window as the parent of this window
            Logging.LogLineIf(fDebugTrace, "Calling SetParent to set new Parent for our form:");
            IntPtr newOldParent = NativeMethods.SetParent(this.Handle, iphWnd);
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "SetParent() returned newParent = " + newOldParent.ToString() + ", GetLastError() returned: " + error.ToString());

            //set our window's size to the size of our window's new parent
            Rectangle ParentRect = new Rectangle();
            NativeMethods.GetClientRect(iphWnd, ref ParentRect);
            this.Size = ParentRect.Size;

            //set our location at (0, 0)
            this.Location = new Point(0, 0);
        }

        void tock_Tick(object sender, EventArgs e)
        {
            //tockCount++;

            //Logging.LogLineIf(fDebugTrace, "tockTick hit #" + tockCount);

            //int error = 0;

            ////Logging.LogLineIf("Calling GetParent to see if our parent still exists:");
            ////IntPtr newOldParent = NativeMethods.GetParent(this.Handle);
            ////error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            ////Logging.LogLineIf("GetParent() returned newParent = " + newOldParent.ToString() + ", GetLastError() returned: " + error.ToString());

            ////NativeMethods.SetLastErrorEx(0, 0);

            ////Logging.LogLineIf("Calling IsVisible to see if our parent still exists:");
            ////bool IsVis = NativeMethods.IsWindowVisible(this.Handle);
            ////error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            ////Logging.LogLineIf("IsWindowVisible() returned newParent = " + IsVis + ", GetLastError() returned: " + error.ToString());

            ////NativeMethods.SetLastErrorEx(0, 0);

            //Logging.LogLineIf("Calling GetWindow to see if our parent still exists:");
            //IntPtr child = NativeMethods.GetWindow(iphWnd, NativeMethods.GetWindow_Cmd.GW_CHILD);
            //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf("GetWindow() returned newParent = " + child.ToString() + ", GetLastError() returned: " + error.ToString());

            //if (tockCount > 2)
            //{

            //    CancelEventArgs cea = new CancelEventArgs();
            //    Application.Exit(cea);
            //    Logging.LogLineIf("Application.Exit() returned cancel = " + cea.Cancel);
            //}

        }

        private void miniControlPanelForm_Shown(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Shown(): called.");
        }

        private void miniControlPanelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosing(): called.");
        }

        private void miniControlPanelForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_FormClosed(): called.");
        }

        private void miniControlPanelForm_ParentChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_ParentChanged(): called.");
        }

        private void miniControlPanelForm_Resize(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Resize(): called.");
        }

        private void miniControlPanelForm_VisibleChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_VisibleChanged(): called.");
        }

        private void miniControlPanelForm_Activated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Activated(): called.");
        }

        private void miniControlPanelForm_Deactivate(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Deactivate(): called.");
        }

        private void miniControlPanelForm_Enter(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Enter(): called.");
        }

        private void miniControlPanelForm_EnabledChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_EnabledChanged(): called.");
        }

        private void miniControlPanelForm_Leave(object sender, EventArgs e)
        {
            Logging.LogLineIf(fDebugTrace, "miniControlPanelForm_Leave(): called.");
        }
    }
}
