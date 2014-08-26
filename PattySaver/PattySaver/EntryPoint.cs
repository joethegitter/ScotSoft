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
        static bool fDebugOutput = true;
        static bool fDebugOutputAtTraceLevel = true;
        static bool fDebugTrace = fDebugOutput && fDebugOutputAtTraceLevel;  

        /// <summary>
        /// The main entry point for the application/screen saver.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();                       // boilerplate, ignore
            Application.SetCompatibleTextRenderingDefault(false);   // boilerplate, ignore

            // Provide exception handlers for exceptions that bubble up this high without being caught
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // if there is no debugger and/or logger available, send Logging.LogLineIf() output
            // to a string buffer and/or scrolling text window so we can see it.
            if (Logging.CannotLog())
            {
                Logging.Destination = Logging.LogDestination.String;
            }
            else
            {
                Logging.Destination = Logging.LogDestination.Default;
            }

            Logging.LogLineIf(fDebugTrace, "Main(): entered.");
            Logging.LogLineIf(fDebugTrace, "   Main(): Debug Output Destination is: " + Logging.Destination.ToString());

            // store the command line
            string cmdLine = System.Environment.CommandLine;
            Logging.LogLineIf(fDebugOutput, "   Main(): CommandLine was: " + cmdLine);

            // determine if we are running hosted by Visual Studio
            Modes.fVSHOSTED = cmdLine.ToLowerInvariant().Contains(".vshost");

            // determine if we are running a file specifying to use the debug output UI
            Modes.fAllowUseOfDebugOutputWindow = cmdLine.ToLowerInvariant().Contains("_dbgwin");

            if (Modes.fVSHOSTED)  // we're a process launched by Visual Studio 
            {
                Logging.LogLineIf(fDebugOutput, "   Main(): we are hosted by Visual Studio.");

                if (args.Length < 1)                                // There were no args: open in non-maximized state to help debugging
                {
                    Logging.LogLineIf(fDebugOutput, @"   Main(): No cmdline args detected, so we'll open in normal mode (!ScreenSaverMode).");
                    LaunchManager.Modes.fNoArgMode = true;
                    LaunchManager.Modes.fOpenInScreenSaverMode = false;
                    ShowScreenSaver();
                    Application.Run();
                }
                else
                {
                    // When VSHOSTED, we initially only examine the first argument to determine if it is official.
                    // For official args, we only look at the first two chars. If we determine that there is an arg
                    // present, but it's not official, then we use HandleUnofficialArgs(), which will re-examine ALL
                    // args found after any (or no) official args.

                    // Get the first two 2 chars of first command line argument, ignore anything past
                    string arg = args[0].ToLowerInvariant().Trim().Substring(0, 2);
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
                            Logging.LogLineIf(fDebugOutput, @"   Main(): No 'Official' args recognized, but args were passed. We'll open in NoArgs mode, and try to execute the 'non-official' args.");
                            LaunchManager.Modes.fNoArgMode = true;
                            HandleUnofficialArguments(0);
                            ShowScreenSaver();
                            Application.Run();
                            break;
                    }
                }
                Logging.LogLineIf(fDebugTrace, "Main(): exiting.");
                return;         
            }
            else
            {
                // We're not being hosted by Visual Studio, so we need to handle command line arguments
                // as if Windows has launched us.  Specifically, we can no longer ignore 
                // arguments with window handles, and we need to treat "no args" as "Show Settings Dialog" 
                // (as per Screen Saver behavior).

                int publicArgsConsumed = 0;
                long toBeHwnd = (long)(-1);
                Modes.LaunchModality mode = Modes.LaunchModality.Undecided;

                // Determine which mode we should launch in from 'official' arguments
                mode = Modes.GetLaunchModalityFromCmdLineArgs(args, out toBeHwnd, out publicArgsConsumed);

                // Handle any 'unofficial' arguments
                HandleUnofficialArguments(publicArgsConsumed);

                // Now launch us
                Launch(mode, toBeHwnd);
            }

            //Logging.LogLineIf("Main(): execution returned to non-VSHOSTED section after Application.Run(). Calling Application.Exit().");
            //Application.Exit();
            Logging.LogLineIf(fDebugTrace, "Main(): exiting.");
            return;         
        }


        /// <summary>
        /// Display the Full Screen form on each of the computer's monitors.
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
        /// Display the Settings form.
        /// </summary>
        static void ShowSettings()
        {
            Form settings = new Settings();
            settings.Show();
        }

        /// <summary>
        /// Display the Settings form.
        /// </summary>
        static void ShowSettings(IntPtr hwnd)
        {
            Form settings = new Settings(hwnd);
            settings.Show();
        }

        /// <summary>
        /// Display something in the mini-preview of the control panel
        /// </summary>
        /// <param name="hwnd">hwnd to the little window of the control panel.</param>
        static void ShowMiniPreview(IntPtr hwnd)
        {
            Form miniprev = new miniControlPanelForm(hwnd);
            miniprev.Show();
        }


        /// <summary>
        /// Handles any arguments on the command line that are not 'official' screen saver arguments.
        /// </summary>
        /// <param name="countOfArgsConsumed">Number of arguments at beginning of command line to ignore, as they were used as 'official' args.</param>
        /// <remarks>For each unoficial argument understood, various state variables will be set, for later consumption.</remarks>
        private static void HandleUnofficialArguments(int countOfArgsConsumed)
        {
            // TODO: rewrite to actually parse the args from System.Environment.CommandLine.  We're being incredibly lazy here.
            // Remember to check to see if countOfargs consumed = or > actual count of args, etc

            if (Modes.fVSHOSTED)
            {
                // currently there are no unofficial args which are meaningful to VSHOSTED mode
                Logging.LogLineIf(fDebugOutput, "HandleUnofficialArguments(): Unofficial arg detected, but unofficial args are not valid for VSHOSTED mode. No action will be taken. Command Line was: " +
                    Environment.NewLine + System.Environment.CommandLine);
                return;
            }

            if (System.Environment.CommandLine.Contains(@"/window"))
            {
                Modes.UnofficialArgOverrideWindowed = true;
                Modes.fOpenInScreenSaverMode = false;
            }
        }


        /// <summary>
        /// Handles "real" launch (as opposed to as an VSHOSTED).
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
                Logging.LogLineIf(fDebugTrace, "Launch(): calling Application.Run().");
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
                Logging.LogLineIf(fDebugTrace, "Launch(): calling Application.Run().");
                Application.Run();
            }
            else if (LaunchMode == Modes.LaunchModality.Mini_Preview)
            {
                IntPtr previewWndHandle = new IntPtr(toBeHwnd);
                ShowMiniPreview(previewWndHandle);
                Logging.LogLineIf(fDebugTrace, "Launch(): calling Application.Run().");
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

                Logging.LogLineIf(fDebugTrace, "Launch(): we fell through to Mode.Undecided, calling Application.Run().");

                Application.Run();
                Logging.LogLineIf(fDebugTrace, "Launch(): exiting for real.");
            }
        }

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
