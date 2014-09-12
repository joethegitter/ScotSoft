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

    public partial class CP_Preview : Form
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
        public Point lastFormLocation = new Point(0,0);

        public IntPtr passedWnd = IntPtr.Zero;

        public CP_Preview(IntPtr setAsParent)
        {
            // debug flags and trace
            fTrace = fDebugOutput && fDebugAtTraceLevel;
            Logging.LogLineIf(fTrace, "CP_Preview.ctor(): entered.");

            Logging.LogLineIf(fTrace, "   CP_Preview.ctor(): calling InitializeComponent()...");
            InitializeComponent();
            fInitializeComponentHasCompleted = true;
            Logging.LogLineIf(fTrace, "   CP_Preview.ctor(): InitializeComponent() returned.");

            passedWnd = setAsParent;
            Logging.LogLineIf(fTrace, "   CP_Preview.ctor(): passedWnd was " + EntryPoint.DecNHex(passedWnd));

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

            // bind all of these form events to handlers (so that we can trace them in debug output)
            this.ChangeUICues += CPPreview_ChangeUICues;
            this.Activated += CPPreview_Activated;
            this.Deactivate += CPPreview_Deactivate;
            this.EnabledChanged += CPPreview_EnabledChanged;
            this.Enter += CPPreview_Enter;
            this.FormClosed += CPPreview_FormClosed;
            this.FormClosing += CPPreview_FormClosing;
            this.GotFocus += CPPreview_GotFocus;
            this.HandleCreated += CPPreview_HandleCreated;
            this.HandleDestroyed += CPPreview_HandleDestroyed;
            this.ImeModeChanged += CPPreview_ImeModeChanged;
            this.Invalidated += CPPreview_Invalidated;
            this.Layout += CPPreview_Layout;
            this.Leave += CPPreview_Leave;
            this.Load += CPPreview_Load;
            this.LostFocus += CPPreview_LostFocus;
            this.MdiChildActivate += CPPreview_MdiChildActivate;
            this.Move += CPPreview_Move;
            this.ParentChanged += CPPreview_ParentChanged;
            this.RegionChanged += CPPreview_RegionChanged;
            this.Shown += CPPreview_Shown;
            this.StyleChanged += CPPreview_StyleChanged;
            this.Validated += CPPreview_Validated;
            this.Validating += CPPreview_Validating;
            this.VisibleChanged += CPPreview_VisibleChanged;
            this.LocationChanged += CPPreview_LocationChanged;

            Logging.LogLineIf(fTrace, "CP_Preview.ctor(): exiting.");

        }

        // override the CreateParams property, so that we can add "child window" to it when queried
        protected override CreateParams CreateParams
        {
            get
            {
                Logging.LogLineIf(fTrace, "CreateParams Property (override): get accessor entered.");
                
                // get the base params
                CreateParams cp = base.CreateParams;
                Logging.LogLineIf(fTrace, "   CreateParams Property (override): base style equals: " + EntryPoint.DecNHex(cp.Style));

                // modify base params
                cp.Style |= NativeMethods.WindowStyles.WS_CHILD;
                Logging.LogLineIf(fTrace, "   CreateParams Property (override): returning  modified style (base.CreateParams.Style |= WS_CHILD), which equals: " + EntryPoint.DecNHex(cp.Style));

                Logging.LogLineIf(fTrace, "CreateParams Property (override): get accessor exiting.");
                return cp;
            }
        }

        // override the Window Proc, so that we can trace messages, and intercept
        // those we need to track independently
        protected override void WndProc(ref Message m)
        {
            // additional level of debug output control
            bool fOutputMessages = true;
            bool fTraceMessages = fTrace && fOutputMessages;

            object lockWMClose = new object();
            object lockWMDestroy = new object();

            // if fTraceMessages, spew out every message we receive, unless on ignore list
            if (!msgsToIgnore.Contains<int>(m.Msg))
            {
                Logging.LogLineIf(fTraceMessages, "  --> " + m.Msg.ToString() + ": " + m.ToString());
            }

            // if we receive WM_DESTROY message, we should close form, which will quit app
            switch ((int)m.Msg)
            {
                case ((int)0x0002):   // WM_DESTROY
                    lock (lockWMDestroy)   // prevent overlapping destroy messages
                    {
                        // 1. don't send another Close if we're already disposing/disposed or closing the form;
                        if (!this.Disposing && !this.IsDisposed && !fCloseMethodHasBeenCalled && !fClosingEventHandlerIsRunning && !fClosingEventHandlerHasCompleted)
                        {
                            Logging.LogLineIf(fTrace, "WndProc(): WM_DESTROY msg received, closing form.");
                            fWndProcSentCloseCommandBecauseWMDESTROY = true;
                            fCloseMethodHasBeenCalled = true;
                            this.Close();
                            return;
                        }
                        else
                        {
                            Logging.LogLineIf(fTrace, "WndProc(): WM_DESTROY msg received, but failed: " + Environment.NewLine +
                                "this.Disposing = " + this.Disposing.ToString() + ", " +
                                "this.IsDisposed = " + this.IsDisposed.ToString() + ", " +
                                "fCloseMethodHasBeenCalled = " + fCloseMethodHasBeenCalled.ToString() + ", " +
                                "fClosingEventHandlerIsRunning = " + fClosingEventHandlerIsRunning.ToString() + ", " +
                                "fClosingEventHandlerHasCompleted = " + fClosingEventHandlerHasCompleted.ToString());
                        }
                    }
                    break;

                case ((int)0x0010):   // WM_CLOSE
                    lock (lockWMClose)   // prevent overlapping close messages
                    {
                        if (!this.Disposing && !this.IsDisposed && !fCloseMethodHasBeenCalled && !fClosingEventHandlerIsRunning && !fClosingEventHandlerHasCompleted)
                        {
                            if (fWndProcSentCloseCommandBecauseWMDESTROY || fWndProcSentCloseCommandBecauseWMCLOSE)
                            {
                                // either the WM_DESTROY or WM_CLOSE message was processed once already, so just let it pass
                                Logging.LogLineIf(fTrace, "WndProc(): WM_CLOSE msg received, fWndProcSentCloseCommandBecauseWMDESTROY = " + fWndProcSentCloseCommandBecauseWMDESTROY.ToString() +
                                    ", fWndProcSentCloseCommandBecauseWMCLOSE = " + fWndProcSentCloseCommandBecauseWMCLOSE.ToString());
                                Logging.LogLineIf(fTrace, "WndProc(): Passing base WM_CLOSE message.");
                                fCloseMethodHasBeenCalled = true;
                                this.Close();
                                base.WndProc(ref m);
                            }
                            else
                            {
                                fWndProcSentCloseCommandBecauseWMCLOSE = true;
                                //fCloseMethodHasBeenCalled = true;
                                //this.Close();
                            }
                            return;
                        }
                        else
                        {
                            Logging.LogLineIf(fTrace, "WndProc(): WM_DESTROY msg received, but failed: " + Environment.NewLine +
                                "this.Disposing = " + this.Disposing.ToString() + ", " +
                                "this.IsDisposed = " + this.IsDisposed.ToString() + ", " +
                                "fCloseMethodHasBeenCalled = " + fCloseMethodHasBeenCalled.ToString() + ", " +
                                "fClosingEventHandlerIsRunning = " + fClosingEventHandlerIsRunning.ToString() + ", " +
                                "fClosingEventHandlerHasCompleted = " + fClosingEventHandlerHasCompleted.ToString());
                        }
                    }
                    break;

                // TODO: hack. Rewrite with a better model.
                
                // Problem: in the control panel, when our mini preview form is running, if the user chooses another
                // screen saver from the preview drop down list, or if the user clicks the Settings button, then
                // our window does not get any messages which tell us this is occuring.  Instead, Windows just covers
                // our window with another window, and our timer/drawing code keeps running. This causes flashing in
                // the window that covers us.
                
                // Unfortunately, our code is also "suspended" in some specific way.  If the user has clicked the Settings button,
                // we are placed in a state where the StartNextInstance event (if we are in SingleInstanceApp mode) is 
                // not sent to us, so we don't know that our Settings dialog has been launched.
                
                // So, we have chosen a model where we are multiple instance.  When the user chooses another screen saver,
                // or even chooses our screen saver again, or clicks the Settings button, we detect this through a hack,
                // and we close ourselves, just as if we had received a WM_DESTROY message. When Windows wants us to draw 
                // our little preview form again, it will launch us again with new a new window handle.
                
                // The hack we use to detect the situation described is not perfectly reliable.  When the users chooses
                // another screen saver, or our screen saver again, or clicks Settings, we get two NC_PAINT messages 
                // (request to draw our non-client area - title bar, window borders - even though we don't have these),
                // with a specific wParam.
                
                // Unfortunately, that is not the ONLY time we may get this message + param combo.  We also get NC_PAINT messages
                // while our Window is initially being built, and when our window is dragged offscreen and then (partially or 
                // completely) back on screen. 
                
                // So, in our detection code, when we get the NC_PAINT message, we check to see if:
                // 1. our window is entirely on screen - rules out the partially off screen method
                // 2. our window's location has changed since we last updated it (poor method of detecting if we are in the middle
                // of a move/drag; we don't get any move/drag messages as a child window).

                // Even this is not perfectly reliable. So, the end result is that we may occasionally kill our instance for
                // no apparent reason (so far, only when dragging the Control Panel near the edge of the screen). This is not 
                // fatal in any way, we just stop drawing our screen saver in the little control panel window. 

                // Various fixes:
                // 1. when we get the NC_PAINT & WPARAM = 1, check to see if our form is partially or completely
                // offscreeen, and our parent or owner windows are not in the middle of a drag operation, or have
                // just been moved.

                //case ((int)0x85):           // NC_PAINT
                //    lock (lockNCPaint)      // lock, to prevent overlapping message processing
                //    {
                //        if ((int)m.WParam == 1)
                //        {
                //            if (!this.Disposing && !this.IsDisposed && fShownHandlerHasCompleted) // && !fClosingHandlerIsRunning && !fClosingHandlerHasCompleted)
                //            {
                //                if (!NativeMethods.IsWindowVisible(this.Handle))
                //                {
                //                    Logging.LogLineIf(fTrace, "WndProc(): NC_PAINT msg received, window visibility = FALSE.");
                //                }

                //                if (FormIsEntirelyOnPrimaryScreen())
                //                {
                //                    if (this.Location == lastFormLocation)    // we were NOT just moved
                //                    {
                //                        Logging.LogLineIf(fTrace, "WndProc(): NC_PAINT received, window is entirely on screen, we did not just move, so closing form.");
                //                        // this.Close();
                //                        // return;
                //                    }
                //                }
                //            }
                //        }
                //    }
                //    break;

                default:
                    base.WndProc(ref m);
                    break;

            }
        }

        public bool FormIsEntirelyOnPrimaryScreen()
        {
            // Create rectangle which describes our form relative to the desktop
            Rectangle formRectangle = new Rectangle(this.Left, this.Top, this.Width, this.Height);

            // Create rectangle from the screen that our form is entirely or mostly on
            Rectangle scrRect = new Rectangle(0, 0, Screen.GetBounds(formRectangle).Width, Screen.GetBounds(formRectangle).Height);

            // TODO: hack, this will only work on single screens
            // Test to see if the screen entirely contains the form rectangle
            return scrRect.Contains(formRectangle);

        }

        void CPPreview_Load(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Load(): entered.");

            if (EntryPoint.fShowDebugOutputWindow)
            {
                tock = new Timer();
                tock.Interval = 4000;
                tock.Tick += tock_Tick;
                tock.Start();
            }

            Logging.LogLineIf(fTrace, "CPPreview_Load(): exiting.");
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

        void CPPreview_LocationChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_LocationChanged(): entered.");
            //Logging.LogLineIf(fTrace, "    CPPreview_LocationChanged(): updating lastFormLocation:");
            //lastFormLocation = this.Location;
            Logging.LogLineIf(fTrace, "CPPreview_LocationChanged(): exiting.");
        }


        void CPPreview_VisibleChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_VisibleChanged(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_VisibleChanged(): exiting.");
        }

        void CPPreview_Validating(object sender, CancelEventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Validating(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Validating(): exiting.");
        }

        void CPPreview_Validated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Validated(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Validated(): exiting.");
        }

        void CPPreview_StyleChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_StyleChanged(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_StyleChanged(): exiting.");
        }

        void CPPreview_Shown(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Shown(): entered.");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): this.Handle = " + EntryPoint.DecNHex(this.Handle));
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): Getting initial owner: calling GetWindow(this.Handle, GW_OWNER)...");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewOwner = NativeMethods.GetWindow(this.Handle, NativeMethods.GetWindow_Cmd.GW_OWNER);
            //int Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetWindow() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewOwner));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): Getting Root Ancestor: calling GetAncestor(this.Handle, GetRoot)...");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewRoot = NativeMethods.GetAncestor(this.Handle, NativeMethods.GetAncestorFlags.GetRoot);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetAncestor() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewRoot));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): Getting top window of root: calling GetTopWindow(" + EntryPoint.DecNHex(CPPreviewRoot) + ")...");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewTopWindow = NativeMethods.GetTopWindow(CPPreviewRoot);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetTopWindow() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewTopWindow));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): Getting top window of THAT window: calling GetTopWindow(" + EntryPoint.DecNHex(CPPreviewTopWindow) + ")...");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewGetTopWindow2 = NativeMethods.GetTopWindow(CPPreviewTopWindow);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetTopWindow(CPPreviewGetTopWindow) returned IntPtr: " + EntryPoint.DecNHex(CPPreviewGetTopWindow2));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): And getting top window of THAT window: calling GetTopWindow(" + EntryPoint.DecNHex(CPPreviewGetTopWindow2) + ")...");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewGetTopWindow3 = NativeMethods.GetTopWindow(CPPreviewGetTopWindow2);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetTopWindow(CPPreviewGetTopWindow2) returned IntPtr: " + EntryPoint.DecNHex(CPPreviewGetTopWindow3));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): And finally getting the top of THAT window: calling GetTopWindow(" + EntryPoint.DecNHex(CPPreviewGetTopWindow3) + "):");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewGetTopWindow4 = NativeMethods.GetTopWindow(CPPreviewGetTopWindow3);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetTopWindow(CPPreviewGetTopWindow3) returned IntPtr: " + EntryPoint.DecNHex(CPPreviewGetTopWindow4));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): getting parent of our window: calling GetParent(this.Handle):");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewParent = NativeMethods.GetParent(this.Handle);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetParent() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewParent));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): then getting ITS parent: calling GetParent(" + EntryPoint.DecNHex(CPPreviewParent) + "):");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewParent2 = NativeMethods.GetParent(CPPreviewParent);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetParent() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewParent2));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): then getting ITS parent: caliing GetParent(" + EntryPoint.DecNHex(CPPreviewParent2) + "):");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewParent3 = NativeMethods.GetParent(CPPreviewParent2);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetParent() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewParent3));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): Finally, getting ITS parent: callig GetParent(" + EntryPoint.DecNHex(CPPreviewParent3) + "):");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr CPPreviewParent4 = NativeMethods.GetParent(CPPreviewParent3);
            //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fTrace, "  CPPreview_Shown(): GetParent() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewParent4));
            //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
            //Logging.LogLineIf(fTrace, " ");

            //// Create the killFormTimer
            //killFormTimer = new Timer();
            //killFormTimer.Interval = 5000;
            //killFormTimer.Tick += killFormTimer_Tick;
            //killFormTimer.Start();


            fShownHandlerHasCompleted = true;
            Logging.LogLineIf(fTrace, "CPPreview_Shown(): exiting.");
        }

        //void killFormTimer_Tick(object sender, EventArgs e)
        //{
        //    //Logging.LogLineIf(fTrace, "  killFormTimer_Tick(): getting parent of our window: calling GetParent(this.Handle):");
        //    //NativeMethods.SetLastErrorEx(0, 0);
        //    //IntPtr CPPreviewParent = NativeMethods.GetParent(this.Handle);
        //    //int Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        //    //Logging.LogLineIf(fTrace, "  killFormTimer_Tick(): GetParent() returned IntPtr: " + EntryPoint.DecNHex(CPPreviewParent));
        //    //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
        //    //Logging.LogLineIf(fTrace, " ");

        //    //Logging.LogLineIf(fTrace, "  killFormTimer_Tick(): testing visibility of our parent: calling IsWindowVisible():");
        //    //NativeMethods.SetLastErrorEx(0, 0);
        //    //bool viz = NativeMethods.IsWindowVisible(CPPreviewParent);
        //    //Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        //    //Logging.LogLineIf(fTrace, "  killFormTimer_Tick(): IsWindowVisible() returned bool: " + viz.ToString());
        //    //Logging.LogLineIf(fTrace, "      GetLastError() returned: " + Error.ToString());
        //    //Logging.LogLineIf(fTrace, " ");

        //}

        void CPPreview_RegionChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Shown(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Shown(): exiting.");
        }

        void CPPreview_ParentChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_ParentChanged(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_ParentChanged(): exiting.");
        }

        void CPPreview_Move(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Move(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Move(): exiting.");
        }

        void CPPreview_MdiChildActivate(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_MdiChildActivate(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_MdiChildActivate(): exiting.");
        }

        void CPPreview_LostFocus(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_LostFocus(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_LostFocus(): exiting.");
        }

        void CPPreview_Leave(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Leave(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Leave(): exiting.");
        }

        void CPPreview_Layout(object sender, LayoutEventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Layout(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Layout(): exiting.");
        }

        void CPPreview_Invalidated(object sender, InvalidateEventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Invalidated(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Invalidated(): exiting.");
        }

        void CPPreview_ImeModeChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_ImeModeChanged(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_ImeModeChanged(): exiting.");
        }

        void CPPreview_HandleDestroyed(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_HandleDestroyed(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_HandleDestroyed(): exiting.");
        }

        void CPPreview_HandleCreated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_HandleCreated(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_HandleCreated(): exiting.");
        }

        void CPPreview_GotFocus(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_GotFocus(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_GotFocus(): exiting.");
        }

        void CPPreview_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_FormClosing(): entered.");
            fClosingEventHandlerIsRunning = true;

            fClosingEventHandlerHasCompleted = true;
            Logging.LogLineIf(fTrace, "CPPreview_FormClosing(): exiting.");
        }

        void CPPreview_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_FormClosed(): entered.");
            fClosedEventHandlerIsRunning = true;
            Logging.LogLineIf(fTrace, "    CPPreview_FormClosed(): calling Application.Exit()");
            Application.Exit();
            fClosedEventHandlerIsRunning = true;
            Logging.LogLineIf(fTrace, "CPPreview_FormClosed(): exiting.");
        }

        void CPPreview_Enter(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Enter(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Enter(): exiting.");
        }

        void CPPreview_EnabledChanged(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_EnabledChanged(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_EnabledChanged(): exiting.");
        }

        void CPPreview_Deactivate(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Deactivate(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Deactivate(): exiting.");
        }

        void CPPreview_Activated(object sender, EventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_Activated(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_Activated(): exiting.");
        }

        void CPPreview_ChangeUICues(object sender, UICuesEventArgs e)
        {
            Logging.LogLineIf(fTrace, "CPPreview_ChangeUICues(): entered.");

            Logging.LogLineIf(fTrace, "CPPreview_ChangeUICues(): exiting.");
        }

        
    }
}
