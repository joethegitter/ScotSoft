using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;
using Microsoft.VisualBasic.ApplicationServices;

namespace SingleInstanceScreenSaver
{
    static class EntryPoint
    {
        // tells our exe to pop up debugOutputWindow on timer after launch
        public const string FILE_DBGWIN = ".popdbgwin";
        public const string POPDBGWIN = @"/popdbgwin";
        // tells our exe to start recording debug output in string buffer from the moment of launch 
        public const string FILE_STARTBUFFER = ".startbuffer";
        public const string STARTBUFFER = @"/startbuffer";

        // command line strings for launch modes
        public const string FROMSTUB = @"/scr";                     // tells us that our exe was launched from our .scr stub
        public const string M_CP_CONFIGURE = @"/cp_configure";      // open settings dlg in control panel
        public const string M_CP_MINIPREVIEW = @"/cp_minipreview";  // open miniPreview form in control panel
        public const string M_DT_CONFIGURE = @"/dt_configure";      // open settings dlg on desktop
        public const string M_SCREENSAVER = @"/screensaver";        // open screenSaverForm
        public const string M_NO_MODE = "no_mode";                  // open screenSaverForm in windowed mode

        public static List<string> scrArgs = new List<string>() { M_CP_CONFIGURE, M_CP_MINIPREVIEW, M_DT_CONFIGURE, M_SCREENSAVER };

        public const string OLDCONFIGURE = @"/c";                   //  same as M_DT_CONFIGURE - but we will ignore any window handles
        public const string OLDSCREENSAVER = @"/s";                 //  same M_SCREENSAVER 

        public static List<string> oldArgs = new List<string>() { OLDCONFIGURE, OLDSCREENSAVER };

        // debug output controllers
        static bool fDebugOutput = true;                                    // controls whether any debug output is emitted
        static bool fDebugOutputAtTraceLevel = true;                        // impacts the granularity of debug output
        static bool fDebugTrace = fDebugOutput && fDebugOutputAtTraceLevel; // controls the granularity of debug output

        // options
        public static bool fRunFromScreenSaverStub = false;
        public static bool fMaintainBuffer = false;
        public static bool fPopUpDebugOutputWindowOnTimer = false;

        public enum LaunchModality
        {
            DT_Configure = 10, CP_Configure = 11, CP_MiniPreview = 20, ScreenSaver = 30, ScreenSaverWindowed = 40,
            /// <summary>
            /// Lauch mode has not yet been established.
            /// </summary>
            Undecided = 0,
            /// <summary>
            /// App should not launch, see NoLaunchReason.
            /// </summary>
            NOLAUNCH = -1
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] mainArgs)
        {
            string debugOutput = "";
            string modalityArgs = "";
            string modeBase = "";
            bool fHasWindowHandle = false;
            string windowHandle = "";

            // The incoming command line for this stub will ONLY ever be (EB = Expected Behavior):
            //  /S                 - EB: run screensaver in fullscreen               - so we set /screensaver
            //  /P windowHandle    - EB: put mini preview window in control panel    - so we set /cp_minipreview -windowHandle  
            //  no args            - EB: show configure dlg on desktop               - so we set /dt_configure
            //  /C                 - EB: show configure dlg on desktop               - so we set /dt_configure
            //  /C:windowHandle    - EB: show configure dlg owned by control panel   - so we set /cp_configure -windowHandle

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Capture the state of the Shift key and Control Key and ALT keys at .scr launch.
            // Note that by using this method, key is only true if it's the ONLY modifier key held down.
            bool fShift = false;
            bool fControl = false;
            bool fAlt = false;
            if (Control.ModifierKeys == Keys.Alt) fAlt = true;
            if (Control.ModifierKeys == Keys.Shift) fShift = true;
            if (Control.ModifierKeys == Keys.Control) fControl = true;

            // first check if the filename has been changed, in order to force post arguments
            string postArgs = "";
            if (Environment.GetCommandLineArgs()[0].ToLowerInvariant().Contains(FILE_DBGWIN.ToLowerInvariant()))
            {
                postArgs += " " + POPDBGWIN;
            }

            // only one of the two filename-based postArgs is allowed, and POPDBGWIN takes precedence. So if postArgs is still empty...
            if (postArgs == "")
            {
                if (Environment.GetCommandLineArgs()[0].ToLowerInvariant().Contains(FILE_STARTBUFFER.ToLowerInvariant()))
                {
                    postArgs += " " + STARTBUFFER;
                }
            }

            // if filename was not modified, check the keys held down at .scr launch
            if (postArgs == "")
            {
                // these are exclusive
                if (fShift) postArgs += " " + POPDBGWIN;
                if (fControl) postArgs += " " + STARTBUFFER;
            }

            // now examine incoming args and build outgoing args
            if (mainArgs.Length < 1)
            {
                // no args
                modeBase = M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 2)
            {
                // can only be:
                //  '/S' or 
                //  '/C' or 
                //  '/C:windowHandle'

                // these are exclusive, only one will ever be true
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/s") modeBase = M_SCREENSAVER;
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/c") modeBase = M_DT_CONFIGURE;

                if (mainArgs[0].ToLowerInvariant().Trim().StartsWith(@"/c:"))
                {
                    // get the chars after /c: for the windowHandle
                    modeBase = M_CP_CONFIGURE;
                    fHasWindowHandle = true;
                    windowHandle = mainArgs[0].Substring(3);
                }

            }
            else if (mainArgs.Length < 3)
            {
                // can only be '/P windowHandle'
                modeBase = M_CP_MINIPREVIEW;
                fHasWindowHandle = true;
                windowHandle = mainArgs[1];
            }
            else
            {
                throw new ArgumentException("CommandLine had more than 2 arguments, could not parse.");
            }

            modalityArgs = FROMSTUB + " " + modeBase;

            if (fHasWindowHandle)
            {
                modalityArgs = modalityArgs + " -" + windowHandle;
            }

            // Add postArg to modalityArgs
            modalityArgs += postArgs;

            // Decide whether to put up message box showing command line args.
            // Change fAlways to true if you want message box to pop up always.
            bool fAlways = false;

            if (fAlt || fAlways)
            {
                DialogResult dr = MessageBox.Show("Incoming cmdLine: " + System.Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                    "Outgoing modalityArgs: " + modalityArgs + Environment.NewLine + Environment.NewLine +
                    "Click OK to launch, Cancel to abort."
                    + Environment.NewLine + Environment.NewLine + debugOutput
                    , Application.ProductName,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                // if user clicks Cancel, don't launch the exe
                if (dr == DialogResult.Cancel)
                {
                    return;
                }
            }

            // Split the string into an array of strings
            string[] Args = modalityArgs.Split(' ');

            // Check Args for debug output options
            foreach (string arg in Args)
            {
                // Determine if we were run from the screen saver stub
                if (arg.ToLowerInvariant().Trim() == FROMSTUB)
                {
                    fRunFromScreenSaverStub = true;
                }

                // Determine if we need to maintain the debugOutput buffer
                if (arg.ToLowerInvariant().Trim() == STARTBUFFER)
                {
                    fMaintainBuffer = true;
                }

                // Determine if we need to put up the debug output window after a timer goes off
                // (for cases where there is no user interaction available, ie miniControlPanelForm)
                if (arg.ToLowerInvariant().Trim() == POPDBGWIN)
                {
                    fPopUpDebugOutputWindowOnTimer = true;
                    fMaintainBuffer = true;
                }
            }

            // If there is a debugger available and logging, add it to the list of LogDestinations
            if (!Logging.CannotLog()) Logging.AddLogDestination(Logging.LogDestination.Default);

            // If necessary, add the buffer to the logging destination list
            if (fMaintainBuffer || fPopUpDebugOutputWindowOnTimer) Logging.AddLogDestination(Logging.LogDestination.Buffer);

            // Start logging
            Logging.LogLineIf(fDebugTrace, "Main(): entered.");
            Logging.LogIf(fDebugTrace, "   Main(): Log Destination(s): ");
            foreach (Logging.LogDestination dest in Logging.LogDestinations)
            {
                Logging.LogIf(fDebugTrace, dest.ToString() + "; ");
            }
            Logging.LogIf(fDebugTrace, Environment.NewLine);
            Logging.LogLineIf(fDebugOutput, "   Main(): CommandLine was: " + Environment.CommandLine);

            // Validate Args
            Tuple<LaunchModality, IntPtr> modeTuple;
            string failReason;

            Logging.LogLineIf(fDebugOutput, "   Main(): calling ValidateArgs()...");
            if (ValidateArgs(Args, out modeTuple, out failReason))
            {
                Logging.LogLineIf(fDebugOutput, "   Main(): calling ProcessCommandLineAndLaunch()...");
                ProcessCommandLineAndLaunch(modeTuple);
            }
            else
            {
                throw new ArgumentException("Command Line Failure: " + failReason);
            }

            Logging.LogLineIf(fDebugTrace, "Main(): exiting.");
        }

        /// <summary>
        /// Parses command line options, sets modes, and calls the Launch Code 
        /// </summary>
        /// <param name="theArgs"></param>
        static void ProcessCommandLineAndLaunch(Tuple<LaunchModality,IntPtr> modeTuple)
        {
            LaunchModality LaunchMode = modeTuple.Item1;
            IntPtr hWnd = modeTuple.Item2;

            // Based on Launch Mode, show the correct window in the correct place
            if (LaunchMode == LaunchModality.DT_Configure)
            {
                // ShowSettings();
            }
            else if (LaunchMode == LaunchModality.CP_Configure)
            {
                
                // ShowSettings(hWnd);
            }
            else if (LaunchMode == LaunchModality.ScreenSaver)
            {
                // fOpenInScreenSaverMode = true;
                // ShowScreenSaver();
            }
            else if (LaunchMode == LaunchModality.ScreenSaverWindowed)
            {
                //fOpenInScreenSaverMode = false;
                //ShowScreenSaver();
            }
            else if (LaunchMode == LaunchModality.CP_MiniPreview)
            {
                // Create and Run the CP_Background Form
                CreateAndRunCPBackgroundForm(hWnd);
                // ShowMiniPreview(hWnd);
            }
            else if (LaunchMode == LaunchModality.NOLAUNCH)
            {
                MessageBox.Show("Command: " + Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                    "Sorry, an error prevented this screen saver from running.", Application.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                // since Application.Run was never called, exe will now terminate... // TODO: JOE: verify fallthough behavior
            }
            else if (LaunchMode == LaunchModality.Undecided)
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
                //ShowSettings();

                //Logging.LogLineIf(fDebugOutput, " ** Launch(): we fell through to Mode.Undecided, calling Application.Run().");
                //Application.Run();

                Logging.LogLineIf(fDebugTrace, "Launch(): exiting for realsies.");
            }
        } // ProcessCommandLineAndLaunch

        static bool ValidateArgs(string[] theArgs, out Tuple<LaunchModality, IntPtr> modeTuple, out string failReason)
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

            // Values to return in case of failure
            modeTuple = new Tuple<LaunchModality, IntPtr>(LaunchModality.Undecided, IntPtr.Zero);
            failReason = "Failed to Initialize failReason!";

            IntPtr hWnd = IntPtr.Zero;
            string launchString = M_SCREENSAVER;

            // First, handle only the very rigorous "we were launched from the scr stub" case.
            // Note "/scr" was already detected, and noted in Modes.fRunFromScreenSaverStub
            int lastProcessedIndex = -1;
            // Logic:
            // A. If /scr exists, it must be first
            // B. If /scr, only one /scr arg allowed
            // C. If /scr, one /scr arg required
            // D. If /scr, validate that windowHandles parse to IntPtr 
            // E. If /scr, non-scr args not allowed except /popdbgwin or /startbuffer

            // first argument must be /scr
            if ((theArgs.Length > 0) && (theArgs[0].ToLowerInvariant() != @"/scr"))
            {
                failReason = @"CommandLine: /scr can only be the first argument.";
                return false;
            }
            lastProcessedIndex = 0;

            // second arg must be one of four valid /scr-related arguments
            if ((theArgs.Length > 1) && !scrArgs.Contains(theArgs[1].ToLowerInvariant()))
            {
                failReason = @"CommandLine: /scr can only be followed by a valid /scr-related argument.";
                return false;
            }
            lastProcessedIndex = 1;

            // if second arg starts with cp_ it must be followed with a valid window handle
            if (theArgs[1].ToLowerInvariant() == M_CP_CONFIGURE || theArgs[1].ToLowerInvariant() == M_CP_MINIPREVIEW)
            {
                if ((theArgs.Length > 2) && theArgs[2].ToLowerInvariant().StartsWith("-"))
                {
                    string subArg = theArgs[2].ToLowerInvariant();
                    string longCandidate = subArg.Substring(1);
                    if (!String.IsNullOrEmpty(longCandidate))
                    {
                        long val;
                        bool parsedOK = long.TryParse(longCandidate, out val);
                        if (parsedOK)
                        {
                            hWnd = new IntPtr(val);
                        }
                        else  // bad parse
                        {
                            failReason = @"CommandLine: invalid window handle passed: " + longCandidate;
                            return false;
                        }
                    }
                    else   // null or empty
                    {
                        failReason = @"CommandLine: null or emptry window handle passed.";
                        return false;
                    }
                }
                else  // missing required sub argument
                {
                    failReason = @"CommandLine: /cp_ argument missing required sub-argument.";
                    return false;
                }
                lastProcessedIndex = 2;
            }

            // at this point, lastProcessedIndex is either 1 or 2. The only valid arguments past here are 
            // either /dgbwin or /startbuffer, which will already have been detected, but not validated
            // in position. They are only allowed in index 2 or 3.

            // validate StartBuffer
            bool DidProcess = false;
            if (fMaintainBuffer)  // this was detected earlier
            {
                if ((theArgs[lastProcessedIndex + 1].ToLowerInvariant() != POPDBGWIN) &&
                    (theArgs[lastProcessedIndex + 1].ToLowerInvariant() != STARTBUFFER))
                {
                    string invalid = POPDBGWIN + " or " + STARTBUFFER;
                    failReason = @"CommandLine:" + invalid + " detected but not at valid index.";
                    return false;
                }
                DidProcess = true; ;
            }

            // validate POPDBGWIN
            if (fPopUpDebugOutputWindowOnTimer)  // this was detected earlier
            {
                if (theArgs[lastProcessedIndex + 1].ToLowerInvariant() != POPDBGWIN)
                {
                    failReason = @"CommandLine:" + POPDBGWIN + " detected but not at valid index." ;
                    return false;
                }
                DidProcess = true; ;
            }

            if (DidProcess) lastProcessedIndex++;

            // starting at lastProcessedIndex, there should be NO arguments
            if ((theArgs.Length - 1) > lastProcessedIndex)
            {
                failReason = @"CommandLine: too many arguments past /scr.";
                return false;
            }

            // by this point, our mode is in mainArgs[1] and hWnd is either IntPtr.Zero or a numerically validated hWnd.
            launchString = theArgs[1].ToLowerInvariant();

            // Now map launchMode string to LaunchMode enum
            LaunchModality LaunchMode = LaunchModality.Undecided;

            if (launchString == M_NO_MODE) LaunchMode = LaunchModality.ScreenSaverWindowed;
            if (launchString == M_CP_CONFIGURE) LaunchMode = LaunchModality.CP_Configure;
            if (launchString == M_CP_MINIPREVIEW) LaunchMode = LaunchModality.CP_MiniPreview;
            if (launchString == M_DT_CONFIGURE) LaunchMode = LaunchModality.DT_Configure;
            if (launchString == M_SCREENSAVER) LaunchMode = LaunchModality.ScreenSaver;

            modeTuple = new Tuple<LaunchModality, IntPtr>(LaunchMode, hWnd);
            failReason = "NO_FAILURE";
            return true;
        }

        static void CreateAndRunCPBackgroundForm(IntPtr hWndForPreview)
        {
            CP_SI_Controller controller = new CP_SI_Controller(hWndForPreview);
            SingleInstanceApplication.Run(controller);
        }

        /// <summary>
        /// Show the little miniControlPanelForm in the Control Panel window
        /// </summary>
        /// <param name="hWnd">hwnd to the little window of the control panel.</param>
        public static void ShowMiniPreview(IntPtr hWnd)
        {
            Logging.LogLineIf(fDebugTrace, "ShowMiniPreview(): Entered.");
            Logging.LogLineIf(fDebugTrace, "   ShowMiniPreview(): hWnd = " + EntryPoint.DecNHex(hWnd));

            if (NativeMethods.IsWindow(hWnd))
            {
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): calling miniControlPanelForm constructor with argument: " + DecNHex(hWnd) + " ...");
                CP_PreviewForm preview = new CP_PreviewForm(hWnd);
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Constructor returned. Window handle is: " + DecNHex(preview.Handle));

                int error = 0;

                //// Determine who the initial parent of our form is.
                //Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Getting initial parent of form: Calling GetParent(" + DecNHex(preview.Handle) + ")...");
                //NativeMethods.SetLastErrorEx(0, 0);
                //IntPtr originalParent = NativeMethods.GetParent(preview.Handle);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): GetParent() returned IntPtr = " + DecNHex(originalParent));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                // Set the passed hWnd to be the parent of the form window.
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Changing parent of form to passed hWnd: Calling SetParent(" + DecNHex(preview.Handle) + ", " + DecNHex(hWnd) + ")...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr newParent = NativeMethods.SetParent(preview.Handle, hWnd);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): SetParent() returned IntPtr = " + DecNHex(newParent));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Verify that the form now has the expected new parent.
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Verifying new parent: Calling GetParent(" + DecNHex(preview.Handle) + ")...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr verifyParent = NativeMethods.GetParent(preview.Handle);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): GetParent() returned IntPtr = " + DecNHex(verifyParent));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Set the size of the form to the size of the parent window (using the passed hWnd)
                System.Drawing.Rectangle ParentRect = new System.Drawing.Rectangle();
                NativeMethods.SetLastErrorEx(0, 0);
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Calling GetClientRect(" + DecNHex(hWnd) + ")...");
                bool fSuccess = NativeMethods.GetClientRect(hWnd, ref ParentRect);
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): GetClientRect() returned bool = " + fSuccess + ", rect = " + ParentRect.ToString());
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Set our size to new rect and location at (0, 0)
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Setting Size and Position with C# code:");
                preview.Size = ParentRect.Size;
                preview.Location = new System.Drawing.Point(0, 0);

                // Show the form
                Logging.LogLineIf(fDebugTrace, " ");
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Calling preview.Show()...");
                preview.Show();
                Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): Show() has returned.");

                // and run it
                //Logging.LogLineIf(fDebugTrace, "  ShowMiniPreview(): calling Application.Run(preview)).");
                //SingleInstanceApplication.Run(preview);
            }
            else
            {
                Logging.LogLineIf(fDebugOutput, "  ShowMiniPreview(): Invalid hWnd passed: " + hWnd.ToString());
                throw new ArgumentException("Invalid hWnd passed to ShowMiniPreview(): " + hWnd.ToString());
            }

            Logging.LogLineIf(fDebugTrace, "ShowMiniPreview(): Exiting.");
        }

        /// <summary>
        /// Displays the Settings form when requested by the Control Panel.
        /// </summary>
        static void ShowSettings(IntPtr hWnd)
        {
            Logging.LogLineIf(fDebugTrace, "ShowSettings(): Entered.");

            if (NativeMethods.IsWindow(hWnd))
            {
                CP_Configure settings = new CP_Configure(hWnd);

                // ScrollingTextWindow debugOutputWindow = new ScrollingTextWindow(settings);

                int error = 0;

                // Get the root owner window of the passed hWnd
                Logging.LogLineIf(fDebugTrace, "  ShowSettings(): Getting Root Ancestor: calling GetAncestor(hWnd, GetRoot)...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr passedWndRoot = NativeMethods.GetAncestor(hWnd, NativeMethods.GetAncestorFlags.GetRoot);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ShowSettings(): GetAncestor() returned IntPtr: " + EntryPoint.DecNHex(passedWndRoot));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // and show ourselves modal to that window
                NativeMethods.WindowWrapper ww = new NativeMethods.WindowWrapper(passedWndRoot);
                Logging.LogLineIf(fDebugTrace, "  ShowSettings(): calling ShowDialog().");
                settings.ShowDialog(ww);

                // and run it
                //Logging.LogLineIf(fDebugTrace, "  ShowSettings(): calling Application.Run(settings)).");
                //Application.Run(settings);
            }
            else
            {
                Logging.LogLineIf(fDebugOutput, "  ShowSettings(): Invalid hWnd passed: " + hWnd.ToString());
                throw new ArgumentException("Invalid hWnd passed to ShowMiniPreview(): " + hWnd.ToString());
            }

            Logging.LogLineIf(fDebugTrace, "ShowSettings(): Exiting.");
        }



        public static string DecNHex(int val)
        {
            return val.ToString("x") + "/" + val.ToString();
        }

        public static string DecNHex(long val)
        {
            return val.ToString("x") + "/" + val.ToString();
        }

        public static string DecNHex(IntPtr val)
        {
            return val.ToString("x") + "/" + val.ToString();
        }

        public static string DecNHex(HandleRef val)
        {
            return val.Handle.ToString("x") + "/" + val.Handle.ToString();
        }
    } // class EntryPoint

    public sealed class SingleInstanceApplication : WindowsFormsApplicationBase
    {
        private static SingleInstanceApplication _application;

        private SingleInstanceApplication()
        {
            base.IsSingleInstance = true;
        }

        public static void Run(Form form)
        {
            _application = new SingleInstanceApplication { MainForm = form };

            _application.StartupNextInstance += NextInstanceHandler;
            _application.Run(Environment.GetCommandLineArgs());
        }

        static void NextInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            Logging.LogLineIf(true, "NextInstanceHandler(): Entered.");


            Logging.LogLineIf(true, "NextInstanceHandler(): Exiting.");

            //// Do whatever you want to do when the user launches subsequent instances
            //// like when the user tries to restart the application again, the main window is activated again.
            //_application.MainWindow.WindowState = FormWindowState.Maximized;
        }
    }
}
