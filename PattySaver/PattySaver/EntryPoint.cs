using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.LaunchManager;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver
{
    static class EntryPoint
    {
        public const string VSHOSTED = ".vshost"; // string appended to our exe basename by VS when hosted
        public const string DBGWIN = ".dbgwin";   // appended by user to enable debug output window in non-hosted build

        static bool fDebugOutput = true;                                    // controls whether any debug output is emitted
        static bool fDebugOutputAtTraceLevel = true;                        // impacts the granularity of debug output
        static bool fDebugTrace = fDebugOutput && fDebugOutputAtTraceLevel; // controls the granularity of debug output


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

            // Scan command line for debug logging and debug hosting options and set them
            SetDebugOutputAndHostOptions();

            // Start logging
            Logging.LogLineIf(fDebugTrace, "Main(): entered.");
            Logging.LogIf(fDebugTrace, "   Main(): Log Destinations are set to: ");
            foreach (Logging.LogDestination dest in Logging.LogDestinations)
            {
                Logging.LogIf(fDebugTrace, dest.ToString() + "  ");
            }
            Logging.LogIf(fDebugTrace, Environment.NewLine);
            Logging.LogLineIf(fDebugOutput, "   Main(): CommandLine was: " + Environment.CommandLine);

            // Set launch modes based on hosting options and command line arguments
            if (Modes.fVSHOSTED)  // we're a process launched by Visual Studio 
            {
                Logging.LogLineIf(fDebugOutput, "   Main(): process is hosted by Visual Studio.");
                LaunchVSHosted(mainArgs);
            }
            else
            {
                LaunchUnhosted(mainArgs);
            }

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

            // Determine early if we need to maintain the debugOutput buffer
            if (cmdLine.ToLowerInvariant().Contains("/usebuffer")) Modes.fMaintainBuffer = true;

            // Determine if we need to put up the debug output window after a timer goes off
            // (for cases where there is no user interaction available, ie miniControlPanelForm)
            Modes.fPopUpDebugOutputWindowOnTimer = EnvArgs[0].ToLowerInvariant().Contains(DBGWIN.ToLowerInvariant());
            if (Modes.fPopUpDebugOutputWindowOnTimer) Modes.fMaintainBuffer = true;

            // If there is a debugger available and logging, add it to the list of LogDestinations
            if (!Logging.CannotLog()) Logging.AddLogDestination(Logging.LogDestination.Default);

            //// If necessary, add the buffer to the logging destination list
            //if (Modes.fMaintainBuffer) Logging.AddLogDestination(Logging.LogDestination.Buffer);

            // Because we cannot have command line arguments in .SCR mode, 
            // Always add the buffer to destination list, until we figure that out
            Logging.AddLogDestination(Logging.LogDestination.Buffer);
        }


        /// <summary>
        /// Parses command line options, sets modes, and runs the correct form, when process is hosted by Visual Studio,
        /// </summary>
        /// <param name="mainArgs"></param>
        static void LaunchVSHosted(string[] mainArgs)
        {
            if (mainArgs.Length < 1)                                // There were no args: open in non-maximized state to help debugging
            {
                Logging.LogLineIf(fDebugOutput, @"   Main(): No cmdline args detected, so we'll open in normal window style (not ScreenSaverWindowStyle).");
                LaunchManager.Modes.fNoArgMode = true;
                LaunchManager.Modes.fOpenInScreenSaverMode = false;
                ShowScreenSaver();
                Application.Run();
            }
            else  // examine the args
            {
                // When VSHOSTED, we initially only examine the first argument to determine if it is official.
                // For official args, we only look at the first two chars. If we determine that there is an arg
                // present, but it's not official, then we use HandleUnofficialArgs(), which will re-examine ALL
                // args found after any (or no) official args.

                // Get the first two 2 chars of first command line argument, ignore anything past
                string arg = mainArgs[0].ToLowerInvariant().Trim().Substring(0, 2);
                switch (arg)
                {
                    case "/c":
                        // Show the options dialog. First check to see if there are additional, 'private' arguments.
                        Logging.LogLineIf(fDebugOutput, @"   Main(): /c detected, so we'll be opening only the Settings dialog, and then quitting.");
                        LaunchManager.Modes.fNoArgMode = false;
                        HandleUnofficialArguments(1);
                        ShowSettings();
                        Application.Run();
                        break;

                    case "/p":
                        // In VSHOSTED mode, don't do anything for preview.
                        Logging.LogLineIf(fDebugOutput, @"   Main(): /p detected; we don't support that in VSHOSTED mode, so we'll quit.");
                        LaunchManager.Modes.fNoArgMode = false;
                        Application.Exit();
                        break;

                    case "/s":
                        // Open in FULLSCREEN, maximized, topmopst mode.  Not recommended for debugging.
                        Logging.LogLineIf(fDebugOutput, @"   Main(): /s detected, so we'll open in ScreenSaverMode.  Not good for debugging.");
                        LaunchManager.Modes.fNoArgMode = false;
                        HandleUnofficialArguments(1);
                        ShowScreenSaver();
                        Application.Run();
                        break;

                    default:
                        // There were no "official" args, so this must be an unofficial arg, and possibly one of many
                        // We'll open in NoArg mode for debugging
                        Logging.LogLineIf(fDebugOutput, @"   Main(): No 'Official' args recognized, but args passed. Open in normal window style, try to execute the 'non-official' args.");
                        LaunchManager.Modes.fNoArgMode = true;
                        LaunchManager.Modes.fOpenInScreenSaverMode = false;
                        HandleUnofficialArguments(0);
                        ShowScreenSaver();
                        Application.Run();
                        break;
                }
            }
        }


        /// <summary>
        /// Parses command line options, sets modes, and calls the Launch Code when process is NOT hosted by Visual Studio,
        /// </summary>
        /// <param name="mainArgs"></param>
        static void LaunchUnhosted(string[] mainArgs)
        {
            int publicArgsConsumed = 0;
            long toBeHwnd = (long)(-1);
            Modes.LaunchModality mode = Modes.LaunchModality.Undecided;

            // Determine which mode we should launch in from 'official' arguments
            mode = Modes.GetLaunchModalityFromCmdLineArgs(mainArgs, out toBeHwnd, out publicArgsConsumed);

            // Handle any 'unofficial' arguments
            HandleUnofficialArguments(publicArgsConsumed);

            // Now launch us
            Launch(mode, toBeHwnd);
        }


        /// <summary>
        /// Handles any arguments on the command line that are not 'official' screen saver arguments.
        /// </summary>
        /// <param name="countOfOfficialArgsConsumed">Number of arguments at beginning of command line to ignore, as they were used as 'official' args.</param>
        /// <remarks>For each unoficial argument understood, various state variables will be set, for later consumption.</remarks>
        private static void HandleUnofficialArguments(int countOfOfficialArgsConsumed)
        {
            // TODO: Turns out that a .SCR can't receive any Unofficial command line arguments.

            // TODO: rewrite to actually parse the args from System.Environment.CommandLine.  We're being incredibly lazy here.

            // Remember to check to see if countOfOfficialArgsConsumed consumed = or > actual count of args, etc

            // Remember that /usebuffer will have been consumed already, so disregard it

            if (System.Environment.CommandLine.Contains(@"/window"))
            {
                Modes.UnofficialArgOverrideWindowed = true;
                Modes.fOpenInScreenSaverMode = false;
            }
        }


        /// <summary>
        /// Handles "real" launch (as opposed to VSHOSTED mode).
        /// </summary>
        /// <param name="LaunchMode"></param>
        /// <param name="toBeHwnd"></param>
        static void Launch(Modes.LaunchModality LaunchMode, long toBeHwnd)
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

            // this overrides all other launch modes
            if (Modes.UnofficialArgOverrideWindowed)
            {
                LaunchMode = Modes.LaunchModality.FullScreen;
            }

            // Based on Launch Mode, show the correct window in the correct place
            if (LaunchMode == Modes.LaunchModality.Configure)
            {
                ShowSettings();
                Logging.LogLineIf(fDebugTrace, "   Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.Configure_WithWindowHandle)
            {
                IntPtr cpWndHandle = new IntPtr(toBeHwnd);
                ShowSettings(cpWndHandle);
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.FullScreen)
            {
                ShowScreenSaver();
                Logging.LogLineIf(fDebugTrace, "   Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.Mini_Preview)
            {
                IntPtr previewWndHandle = new IntPtr(toBeHwnd);
                ShowMiniPreview(previewWndHandle);
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
            settings.Show();
        }


        /// <summary>
        /// Show the little miniControlPanelForm in the Control Panel window
        /// </summary>
        /// <param name="hwnd">hwnd to the little window of the control panel.</param>
        static void ShowMiniPreview(IntPtr hwnd)
        {
            Form miniprev = new miniControlPanelForm(hwnd);
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
