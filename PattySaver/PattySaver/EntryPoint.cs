using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.LaunchManager;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver
{
    static class EntryPoint
    {
        #region Data

        // command line strings and file extensions
        public const string VSHOSTED = ".vshost";                   // string appended to our exe basename by VS when hosted
        public const string FROMSTUB = @"/scr";                     // tells us that our exe was launched from our .scr stub
        public const string POPDBGWIN = @"/popdbgwin";                    // pop up debugOutputWindow on timer after launch
        public const string STARTBUFFER = @"/startbuffer";          // place debug output in string buffer from the moment of launch

        // command line strings for launch modes
        public const string M_CP_CONFIGURE = @"/cp_configure";      // open settings dlg in control panel
        public const string M_CP_MINIPREVIEW = @"/cp_minipreview";  // open miniPreview form in control panel
        public const string M_DT_CONFIGURE = @"/dt_configure";      // open settings dlg on desktop
        public const string M_SCREENSAVER = @"/screensaver";        // open screenSaverForm
        public const string M_NO_MODE = "no_mode";                  // open screenSaverForm in windowed mode

        public static List<string> scrArgs = new List<string>() { M_CP_CONFIGURE, M_CP_MINIPREVIEW, M_DT_CONFIGURE, M_SCREENSAVER};

        public const string OLDCONFIGURE = @"/c";                   //  same as M_DT_CONFIGURE - but we will ignore any window handles
        public const string OLDSCREENSAVER = @"/s";                 //  same M_SCREENSAVER 

        public static List<string> oldArgs = new List<string>() { OLDCONFIGURE, OLDSCREENSAVER };

        // debug output controllers
        static bool fDebugOutput = true;                                    // controls whether any debug output is emitted
        static bool fDebugOutputAtTraceLevel = true;                        // impacts the granularity of debug output
        static bool fDebugTrace = fDebugOutput && fDebugOutputAtTraceLevel; // controls the granularity of debug output

        #endregion Data

        /// <summary>
        /// The entry point for our application.
        /// </summary>
        [STAThread]
        static void Main(string[] mainArgs)
        {
            Application.EnableVisualStyles();                       // boilerplate, ignore
            Application.SetCompatibleTextRenderingDefault(false);   // boilerplate, ignore

            // Provide exception handlers for exceptions that bubble up this high without being caught.
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

#if DEBUG
            // Uncomment the following lines and in DEBUG builds 
            // we'll put up this dialog every time we launch, showing command line 

            //MessageBox.Show("CommandLine: " + Environment.CommandLine , Application.ProductName,
            //    MessageBoxButtons.OK, MessageBoxIcon.Information);
#endif

            // Scan command line for debug logging and debug hosting options and set them
            SetDebugOutputAndHostOptions();

            // Start logging
            Logging.LogLineIf(fDebugTrace, "Main(): entered.");
            Logging.LogIf(fDebugTrace, "   Main(): Log Destination(s): ");
            foreach (Logging.LogDestination dest in Logging.LogDestinations)
            {
                Logging.LogIf(fDebugTrace, dest.ToString() + "; ");
            }
            Logging.LogIf(fDebugTrace, Environment.NewLine);
            Logging.LogLineIf(fDebugOutput, "   Main(): CommandLine was: " + Environment.CommandLine);

            if (Modes.fVSHOSTED) Logging.LogLineIf(fDebugOutput, "   Main(): process is hosted by Visual Studio.");

            // Process command line args and launch accordingly
            ProcessCommandLineAndLaunch(mainArgs);

            Logging.LogLineIf(fDebugTrace, "Main(): exiting.");
        }


        /// <summary>
        /// Scans command line options for debug logging and debug hosting options, and sets them accordingly.
        /// </summary>
        static void SetDebugOutputAndHostOptions()
        {
            // Store away the actual command line that launched us.
            string cmdLine = System.Environment.CommandLine;

            // Get the REAL command line args, because the full name and path of
            // our executable is conveniently stored in item zero.
            string[] EnvArgs = Environment.GetCommandLineArgs();

            // Determine if we are running hosted by Visual Studio.
            Modes.fVSHOSTED = EnvArgs[0].ToLowerInvariant().Contains(VSHOSTED.ToLowerInvariant());

            foreach (string arg in EnvArgs)
            {
                // Determine if we were run from the screen saver stub
                if (arg.ToLowerInvariant().Trim() == FROMSTUB)
                {
                    Modes.fRunFromScreenSaverStub = true;
                }

                // Determine if we need to maintain the debugOutput buffer
                if (arg.ToLowerInvariant().Trim() == STARTBUFFER)
                {
                    Modes.fMaintainBuffer = true;
                }

                // Determine if we need to put up the debug output window after a timer goes off
                // (for cases where there is no user interaction available, ie miniControlPanelForm)
                if (arg.ToLowerInvariant().Trim() == POPDBGWIN)
                {
                    Modes.fPopUpDebugOutputWindowOnTimer = true;
                    Modes.fMaintainBuffer = true;
                }
            }

            // If there is a debugger available and logging, add it to the list of LogDestinations
            if (!Logging.CannotLog()) Logging.AddLogDestination(Logging.LogDestination.Default);

            // If necessary, add the buffer to the logging destination list
            if (Modes.fMaintainBuffer || Modes.fPopUpDebugOutputWindowOnTimer) Logging.AddLogDestination(Logging.LogDestination.Buffer);
        }

        /// <summary>
        /// Parses command line options, sets modes, and calls the Launch Code 
        /// </summary>
        /// <param name="mainArgs"></param>
        static void ProcessCommandLineAndLaunch(string[] mainArgs)
        {
            // The only args that this exe cares about are:
            // 1. no mode args passed = M_NO_MODE (open screensaver in last remembered non-ScreenSaverStyle window, with no Slideshow)
            // 2. /scr, which tells us that we were launched from the .scr stub,
            //    and therefore need to respect arguments with window handles
            // 3. /dt_configure (open settings dialog on desktop)
            // 4. /c (same as dt_configure)
            // 5. /cp_configure -windowHandle (open settings owned by control panel)
            // 6. /cp_minipreview -windowHandle (put minipreview form in control panel)
            // 7. /screensaver (run the default screen saver with slideshow)
            // 8. /s (same as /screensaver)
            // 9. /popdbgwin (pop up the debugoutputwindow on a timer after launch)
            // 10. any 'unofficial' args we process with SetDebugOutputAndHostOptions() and/or HandleUnofficialArgs()

            // By the time we get to this method, the command line will have been scanned once by
            // SetDebugOutputAndHostOptions(), and certain LaunchManager.Modes will have been set:
            //     1. Modes.fRunFromScreenSaverStub
            //     2. Modes.VSHOSTED
            //     3. Modes.fPopUp
            //     4. Modes.fMaintainBuffer

            IntPtr hWnd = IntPtr.Zero;
            string launchString = M_SCREENSAVER;
         
            // First, handle only the very rigorous "we were launched from the scr stub" case.
            // Note "/scr" was already detected, and noted in Modes.fRunFromScreenSaverStub
            int lastProcessedIndex = -1;
            if (Modes.fRunFromScreenSaverStub)  // set for us by SetDebugOutputAndHostOptions()
            {
                // Logic:
                // A. If /scr exists, it must be first
                // B. If /scr, only one /scr arg allowed
                // C. If /scr, one /scr arg required
                // D. If /scr, validate that windowHandles parse to IntPtr 
                // E. If /scr, non-scr args not allowed except /popdbgwin or /startbuffer

                // first argument must be /scr
                if ((mainArgs.Length > 0) && (mainArgs[0].ToLowerInvariant() != @"/scr"))
                {
                    throw new ArgumentException(@"CommandLine: /scr can only be the first argument." + 
                    Environment.NewLine + Environment.CommandLine);
                }
                lastProcessedIndex = 0;

                // second arg must be one of four valid /scr-related arguments
                if ((mainArgs.Length > 1) && !scrArgs.Contains(mainArgs[1].ToLowerInvariant()))
                {
                    throw new ArgumentException(@"CommandLine: /scr can only be followed by a valid /scr-related argument." +
                    Environment.NewLine + Environment.CommandLine);
                }
                lastProcessedIndex = 1;

                // if second arg starts with cp_ it must be followed with a valid window handle
                if (mainArgs[1].ToLowerInvariant() == M_CP_CONFIGURE || mainArgs[1].ToLowerInvariant() == M_CP_MINIPREVIEW)
                {
                    if ((mainArgs.Length > 2) && mainArgs[2].ToLowerInvariant().StartsWith("-"))
                    {
                        string subArg = mainArgs[2].ToLowerInvariant();
                        string longCandidate = subArg.Substring(1);
                        if (!String.IsNullOrEmpty(longCandidate))
                        {
                            long val;
                            bool worked = long.TryParse(longCandidate, out val);
                            if (worked)
                            {
                                hWnd = new IntPtr(val);
                            }
                            else  // bad parse
                            {
                                throw new ArgumentException(@"CommandLine: invalid window handle passed: " + longCandidate +
                                    Environment.NewLine + Environment.CommandLine);
                            }
                        }
                        else   // null or empty
                        {
                            throw new ArgumentException(@"CommandLine: invalid window handle passed." +
                                Environment.NewLine + Environment.CommandLine);
                        }
                    }
                    else  // missing required sub argument
                    {
                        throw new ArgumentException(@"CommandLine: /cp_ argument missing required subargument." +
                            Environment.NewLine + Environment.CommandLine);
                    }
                    lastProcessedIndex = 2;
                }

                // at this point, lastProcessedIndex is either 1 or 2. The only valid arguments past here are 
                // either /dgbwin or /startbuffer, which will already have been detected, but not validated
                // in position. They are only allowed in index 2 or 3.

                // validate StartBuffer
                bool DidProcess = false;
                if (Modes.fMaintainBuffer)  // this was detected earlier
                {
                    if ((mainArgs[lastProcessedIndex + 1].ToLowerInvariant() != POPDBGWIN) &&
                        (mainArgs[lastProcessedIndex + 1].ToLowerInvariant() != STARTBUFFER))
                    {
                        string invalid = POPDBGWIN + " or " + STARTBUFFER;
                        throw new ArgumentException(@"CommandLine:" + invalid + " detected but not at valid index." +
                            Environment.NewLine + Environment.CommandLine);
                    }
                    DidProcess = true; ;
                }

                // validate POPDBGWIN
                if (Modes.fPopUpDebugOutputWindowOnTimer)  // this was detected earlier
                {
                    if (mainArgs[lastProcessedIndex + 1].ToLowerInvariant() != POPDBGWIN)
                    {
                        throw new ArgumentException(@"CommandLine:" + POPDBGWIN + " detected but not at valid index." +
                            Environment.NewLine + Environment.CommandLine);
                    }
                    DidProcess = true; ;
                }

                if (DidProcess) lastProcessedIndex++;

                // starting at lastProcessedIndex, there should be NO arguments
                if ((mainArgs.Length - 1) > lastProcessedIndex)
                {
                    throw new ArgumentException(@"CommandLine: too many arguments past /scr." +
                        Environment.NewLine + Environment.CommandLine);
                }

                // by this point, our mode is in mainArgs[1] and hWnd is either IntPtr.Zero or a numerically validated hWnd.
                launchString = mainArgs[1].ToLowerInvariant();
            }
            else // we were not launched from .scr stub.
            {
                // So valid args are:
                // - no args
                // - one of the four scrArgs
                //   - and possibly one of two subArgs
                // - one of the old args, which we'll map to a scrArg
                // - /popdbgwin, which we will already have detected

                // Apply some rules
                // 1. acceptable: no scrArg, no oldArg
                // 2. if any scrArg, only one allowed
                // 2. if any scrArg, no oldArgs allowed
                // 3. if no scrArg, only one oldArg allowed
                // 4. if scrArg or oldArg exists, it must be first
                // 5. if scrArg exists, any subArg must be second

                launchString = M_NO_MODE;
                int countOfscrArgsDetected = 0;
                int countOfoldArgsDetected = 0;
                if (mainArgs.Length > 0)
                {
                    foreach (string arg in mainArgs)
                    {
                        string testMe = arg.ToLowerInvariant().Trim();
                        if (scrArgs.Contains(testMe)) countOfscrArgsDetected++;
                        if (oldArgs.Contains(testMe)) countOfoldArgsDetected++;

                        if (testMe == M_DT_CONFIGURE) launchString = M_DT_CONFIGURE;
                        if (testMe == M_SCREENSAVER) launchString = M_SCREENSAVER;
                        if (testMe == OLDCONFIGURE) launchString = M_DT_CONFIGURE;
                        if (testMe == OLDSCREENSAVER) launchString = M_SCREENSAVER;
                        if (testMe == M_CP_CONFIGURE) launchString = M_CP_CONFIGURE;
                        if (testMe == M_CP_MINIPREVIEW) launchString = M_CP_MINIPREVIEW;
                    }

                    // no multiple modes or mixing old and new
                    if (countOfoldArgsDetected > 1 ||
                        countOfscrArgsDetected > 1 ||
                        (countOfoldArgsDetected + countOfscrArgsDetected > 1))
                    {
                        throw new ArgumentException("CommandLine: only one scrArg allowed, or only one oldArg allowed, or scrArg and oldArg cannot be combined." +
                            Environment.NewLine + Environment.CommandLine);
                    }

                    // mode must be first argument; so if we have a mode, compare it to first argument
                    if (launchString != M_NO_MODE)
                    {
                        if (mainArgs[0].ToLowerInvariant().Trim() != launchString)
                        {
                            // this may be because of old vs new args. check that first
                            if ((mainArgs[0].ToLowerInvariant().Trim() == OLDCONFIGURE && launchString == M_DT_CONFIGURE) ||
                                (mainArgs[0].ToLowerInvariant().Trim() == OLDSCREENSAVER && launchString == M_SCREENSAVER)
                                )
                            {
                                // do nothing, that's expected
                            }
                            else
                            {
                                // mode argument was out of order.  Reject.
                                throw new ArgumentException("CommandLine: any mode argument must be the first argument." +
                                    Environment.NewLine + Environment.CommandLine);
                            }
                        }
                    }

                    // require and validate window handles
                    if ((launchString == M_CP_CONFIGURE) || (launchString == M_CP_MINIPREVIEW))
                    {
                        if ((mainArgs.Length > 2) && mainArgs[2].ToLowerInvariant().StartsWith("-"))
                        {
                            string subArg = mainArgs[1].ToLowerInvariant().Trim();
                            string longCandidate = subArg.Substring(1);
                            if (!String.IsNullOrEmpty(longCandidate))
                            {
                                long val;
                                bool worked = long.TryParse(longCandidate, out val);
                                if (worked)
                                {
                                    hWnd = new IntPtr(val);
                                }
                                else  // bad parse
                                {
                                    throw new ArgumentException(@"CommandLine: invalid window handle passed: " + longCandidate +
                                        Environment.NewLine + Environment.CommandLine);
                                }
                            }
                            else   // null or empty
                            {
                                throw new ArgumentException(@"CommandLine: invalid window handle passed." +
                                    Environment.NewLine + Environment.CommandLine);
                            }
                        }
                        else  // missing required sub argument
                        {
                            throw new ArgumentException(@"CommandLine: /cp_ argument missing required subargument." +
                                    Environment.NewLine + Environment.CommandLine);
                        }
                    }
                    // by this point, our mode is in launchMode and hWnd is either IntPtr.Zero or a numerically validated hWnd.
                }
            }

            // Now map launchMode string to LaunchMode
            LaunchManager.Modes.LaunchModality LaunchMode = Modes.LaunchModality.Undecided;

            if (launchString == M_NO_MODE) LaunchMode = Modes.LaunchModality.ScreenSaverWindowed;
            if (launchString == M_CP_CONFIGURE) LaunchMode = Modes.LaunchModality.CP_Configure;
            if (launchString == M_CP_MINIPREVIEW) LaunchMode = Modes.LaunchModality.CP_MiniPreview;
            if (launchString == M_DT_CONFIGURE) LaunchMode = Modes.LaunchModality.DT_Configure;
            if (launchString == M_SCREENSAVER) LaunchMode = Modes.LaunchModality.ScreenSaver;

            // Handle any 'unofficial' arguments
            HandleUnofficialArguments(mainArgs);

            // Now launch us
            Launch(LaunchMode, hWnd);
        }


        /// <summary>
        /// Handles any arguments on the command line that are not 'official' screen saver arguments.
        /// </summary>
        /// <param name="countOfOfficialArgsConsumed">Number of arguments at beginning of command line to ignore, as they were used as 'official' args.</param>
        /// <remarks>For each unoficial argument understood, various state variables will be set, for later consumption.</remarks>
        private static void HandleUnofficialArguments(string [] mainArgs)
        {
            // TODO: rewrite to actually parse the args from System.Environment.CommandLine.  We're being incredibly lazy here.
            // Remember that /usebuffer will have been consumed already, so disregard it
        }


        /// <summary>
        /// Opens the necessary windows.
        /// </summary>
        /// <param name="LaunchMode"></param>
        /// <param name="hWnd"></param>
        static void Launch(LaunchManager.Modes.LaunchModality LaunchMode, IntPtr hWnd)
        {
            Logging.LogLineIf(fDebugTrace, "Launch(): entered.");

            // Store away the actual command line for debugging purposes
            Logging.LogLineIf(fDebugTrace, "   Launch(): Mode = " + LaunchMode.ToString());

#if DEBUG
            // Uncomment the following lines and in DEBUG builds not attached to a debugger (ie, in Control Panel)
            // we'll put up this dialog every time we launch, showing command line and launch mode.

            //if (Logging.CannotLog())                                // there's no debugger, so we'll put up a dialog
            //{
            //    MessageBox.Show("CommandLine: " + cmdLine + Environment.NewLine + Environment.NewLine +
            //        "Launch Mode = " + LaunchMode.ToString(), Application.ProductName,
            //        MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
#endif

            // Store away LaunchMode for later reference
            Modes.LaunchMode = LaunchMode;

            // Based on Launch Mode, show the correct window in the correct place
            if (LaunchMode == Modes.LaunchModality.DT_Configure)
            {
                ShowSettings();
                Logging.LogLineIf(fDebugTrace, "   Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.CP_Configure)
            {
                ShowSettings(hWnd);
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.ScreenSaver)
            {
                Modes.fOpenInScreenSaverMode = true;
                ShowScreenSaver();
                Logging.LogLineIf(fDebugTrace, "   Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.ScreenSaverWindowed)
            {
                Modes.fOpenInScreenSaverMode = false;
                ShowScreenSaver();
                Logging.LogLineIf(fDebugTrace, "   Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.CP_MiniPreview)
            {
                ShowMiniPreview(hWnd);
                Logging.LogLineIf(fDebugTrace, "   Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.NOLAUNCH)
            {
                MessageBox.Show("Command: " + Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                    "Sorry, an error prevented this screen saver from running.", Application.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                // since Application.Run was never called, exe will now terminate... // TODO: JOE: verify fallthough behavior
            }
            else if (LaunchMode == Modes.LaunchModality.Undecided)
            {
                Logging.LogLineIf(fDebugOutput, "Error: Apparently we are still in LaunchMode == Undecided.");
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    MessageBox.Show("Error: Apparently we are still in LaunchMode == Undecided. Gonna break into debugger now.",
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    System.Diagnostics.Debugger.Break();
                }
#endif

                // In release, we'll just quietly fail here, and launch in Configure mode
                ShowSettings();

                Logging.LogLineIf(fDebugOutput, " ** Launch(): we fell through to Mode.Undecided, calling Application.Run().");

                Application.Run();
                Logging.LogLineIf(fDebugTrace, "Launch(): exiting for realsies.");
            }
        }

        /// <summary>
        /// Displays the ScreenSaver form on each of the computer's monitors.
        /// </summary>
        static void ShowScreenSaver()
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                ScreenSaverForm screensaver = new ScreenSaverForm(screen.Bounds);
                screensaver.Show();
            }
        }


        /// <summary>
        /// Displays the Settings form; used when command line has no args, or when args = /c.
        /// </summary>
        static void ShowSettings()
        {
            Form settings = new Settings();
            settings.Show();
        }


        /// <summary>
        /// Displays the Settings form when requested the Control Panel.
        /// </summary>
        static void ShowSettings(IntPtr hwnd)
        {
            Form settings = new Settings(hwnd);
            ScrollingTextWindow debugOutputWindow = new ScrollingTextWindow(settings);

            // Find the Window whose caption says "Screen Saver Settings"
            Logging.LogLineIf(fDebugTrace, "  ShowSettings(): Calling FindWindowByCaption():");
            NativeMethods.SetLastErrorEx(0, 0);
            IntPtr daddy = NativeMethods.FindWindowByCaption(IntPtr.Zero, "Screen Saver Settings");
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  ShowSettings(): FindWindowByCaption() returned IntPtr = " + daddy.ToString() + ", GetLastError() returned: " + error.ToString());

            //Logging.LogLineIf(fDebugTrace, "  ShowSettings(): Calling SetParent to set new Parent for our form:");
            //NativeMethods.SetLastErrorEx(0, 0);
            //IntPtr newOldParent = NativeMethods.SetParent(settings.Handle, hwnd);
            //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            //Logging.LogLineIf(fDebugTrace, "  ShowSettings(): SetParent() returned IntPtr = " + newOldParent.ToString() + ", GetLastError() returned: " + error.ToString());

            NativeMethods.WindowWrapper ww = new NativeMethods.WindowWrapper(daddy);
            settings.Visible = false;
            settings.ShowDialog(ww);
        }


        /// <summary>
        /// Show the little miniControlPanelForm in the Control Panel window
        /// </summary>
        /// <param name="hwnd">hwnd to the little window of the control panel.</param>
        static void ShowMiniPreview(IntPtr hwnd)
        {
            Form miniprev = new miniControlPanelForm(hwnd);

            // Set miniprev's window style to WS_CHILD, so that it window is 
            // destroyed when parent window is destroyed. Start by getting
            // the value which represents the current window style, and modifying
            // that value to include WS_CHILD.
            // TODO: should probably also be clearing WS_POPUP
            IntPtr ip = new IntPtr();
            int index = (int)NativeMethods.WindowLongFlags.GWL_STYLE | 0x40000000;
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): About to call GetWindowLongPtr:");
            ip = NativeMethods.GetWindowLongPtr(miniprev.Handle, index);
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): GetWindowLongPtr returned IntPtr: " + ip.ToString() + ", GetLastError() returned: " + error.ToString());

            // Now use that value to set the new window style.
            object ohRef = new object();
            HandleRef hRef = new HandleRef(ohRef, miniprev.Handle);
            IntPtr ip2 = new IntPtr();
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): About to call SetWindowLongPtr:");
            index = (int)NativeMethods.WindowLongFlags.GWL_STYLE;
            ip2 = NativeMethods.SetWindowLongPtr(hRef, index, ip);
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): SetWindowLongPtr returned IntPtr: " + ip2.ToString() + ", GetLastError() returned: " + error.ToString());

            // Now make the passed hWnd miniprev's parent window.
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Calling SetParent to set new Parent for our form:");
            NativeMethods.SetLastErrorEx(0, 0);
            IntPtr newOldParent = NativeMethods.SetParent(miniprev.Handle, hwnd);
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): SetParent() returned IntPtr = " + newOldParent.ToString() + ", GetLastError() returned: " + error.ToString());

            // Set miniprev window size to the size of our window's new parent.
            // First, get that size.
            System.Drawing.Rectangle ParentRect = new System.Drawing.Rectangle();
            NativeMethods.SetLastErrorEx(0, 0);
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Calling GetClientRect to get a new rect for our form:");
            bool fSuccecss = NativeMethods.GetClientRect(hwnd, ref ParentRect);
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): GetClientRect() returned bool = " + fSuccecss + ", GetLastError() returned: " + error.ToString());

            // Set our size to new rect and location at (0, 0)
            Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Setting Size and Position:");
            miniprev.Size = ParentRect.Size;
            miniprev.Location = new System.Drawing.Point(0, 0);


            miniprev.Show();
        }



        // ------------ Unhandled Exception Handlers Below Here ------------ //


        /// <summary>
        /// Handles any un-handled Thread Exceptions that bubble up as far as the EntryPoint.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs ea)
        {
            Exception e = ea.Exception;

            string caption = "Hey, contact Scot, and tell him...";
            string instructions = "We've encountered an unexpected 'thread exception'. If you click OK we will try to continue, but we may crash. If you click 'Cancel', we will terminate the program." + Environment.NewLine + Environment.NewLine;
            string strSender = sender.ToString() + Environment.NewLine;
            string details = "Exception Message: " + e.Message + Environment.NewLine + Environment.NewLine;
            string details2 = "Stack Trace: " + Environment.NewLine;
            string stackTrace = "";
            try
            {
                stackTrace = e.ToString().Replace(";", Environment.NewLine);
            }
            catch { }
            string body = instructions + strSender + details + details2 + stackTrace;

            DialogResult dr = MessageBox.Show(body, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            if (dr == DialogResult.Cancel)
            {
                Application.Exit();
            }
        }


        /// <summary>
        /// Handles any un-handled UI Exceptions that bubble up as far as the EntryPoint.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ea)
        {
            Exception e = (Exception)ea.ExceptionObject;

            string caption = "Hey, contact Scot, and tell him...";
            string instructions = "";
            if (ea.IsTerminating)
            {
                instructions = "We've encountered an unexpected 'UI Exception'. After you click 'OK', the program will terminate." + Environment.NewLine + Environment.NewLine;
            }
            else
            {
                instructions = "We've encountered an unexpected 'UI Exception'. If you click OK we will try to continue, but we may crash. If you click 'Cancel', we will terminate the program." + Environment.NewLine + Environment.NewLine;
            }

            string strSender = sender.ToString() + Environment.NewLine;
            string details = "Exception Message: " + e.Message + Environment.NewLine + Environment.NewLine;
            string details2 = "Stack Trace: " + Environment.NewLine;
            string stackTrace = "";
            try
            {
                stackTrace = e.ToString().Replace(";", Environment.NewLine);
            }
            catch { }
            string body = instructions + strSender + details + details2 + stackTrace;
            DialogResult dr;

            if (ea.IsTerminating)
            {
                dr = MessageBox.Show(body, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                dr = MessageBox.Show(body, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            }

            dr = MessageBox.Show(body, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

            if (ea.IsTerminating)
            {
                // do nothing
            }
            else
            {

                if (dr == DialogResult.Cancel)
                {
                    Application.Exit();
                }
            }

        }

    }
}
