using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

using System.Windows;
using System.Windows.Input;




namespace ScrSvStb
{
    class Program
    {

        // Name of the stub
        public static string NAME = "Screen Saver Launcher";

        // Use this "Path" constant to tell the Screen Saver Stub where to 
        // find the executable to launch. By default we use the directory 
        // that ScrSvStb lives in
        // public static string PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string PATH = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

        // Filename the stub will launch
        public const string TARGET_BASE = "PattySaver";
        public const string TARGET_EXT = ".exe";
        public static string TARGET = PATH + @"\" + TARGET_BASE + TARGET_EXT;

        // Filename elements, command line args and keystates that tell our
        // executable to pop up debugOutputWindow on a timer after launch.
        public const string FILE_DBGWIN = ".popdbgwin";
        public const string POPDBGWIN = @"/popdbgwin";
        public static bool fShiftKeyDown = false;

        // Filename elements, command line args and keystates that tell our
        // executable to immediately start storing debug output in a 
        // a buffer at launch (as opposed to when we open the 
        // debugOutputWindow).
        public const string FILE_STARTBUFFER = ".startbuffer";
        public const string STARTBUFFER = @"/startbuffer";
        public static bool fControlKeyDown = false;

        // Command line args that the stub will issue to our application
        public const string FROMSTUB = @"/scr";                     // tells us that our exe was launched from the stub
        public const string M_CP_CONFIGURE = @"/cp_configure";      // open settings dlg in control panel
        public const string M_CP_MINIPREVIEW = @"/cp_minipreview";  // open miniPreview form in control panel
        public const string M_DT_CONFIGURE = @"/dt_configure";      // open settings dlg on desktop
        public const string M_SCREENSAVER = @"/screensaver";        // open screenSaverForm

        // Keystate that tells us to always show launch args
        public static bool fAltKeyDown = false;

        // Import the Win32 MessageBox
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern MessageBoxResult MessageBox(IntPtr hWnd, String text, String caption, MessageBoxOptions options);

        static void Main(string[] mainArgs)
        {
            string debugOutput = "";
            string scrArgs = "";
            string mode = "";
            bool fHasWindowHandle = false;
            string windowHandle = "";

            // The incoming command line for this stub will ONLY ever be the following,
            // where EB = the Behavior that Windows Expects
            //  /S                 - EB: run screensaver in fullscreen               - so we pass /screensaver
            //  /P windowHandle    - EB: put mini preview window in control panel    - so we pass /cp_minipreview -windowHandle  
            //  no args            - EB: show configure dlg on desktop               - so we pass /dt_configure
            //  /C                 - EB: show configure dlg on desktop               - so we pass /dt_configure
            //  /C:windowHandle    - EB: show configure dlg owned by control panel   - so we pass /cp_configure -windowHandle

            // Capture the state of the Shift key and Control Key and ALT keys at .scr launch.
            fAltKeyDown = KeyboardInfo.GetKeyState(WindowsVirtualKey.VK_MENU).IsPressed;
            fShiftKeyDown = KeyboardInfo.GetKeyState(WindowsVirtualKey.VK_SHIFT).IsPressed;
            fControlKeyDown = KeyboardInfo.GetKeyState(WindowsVirtualKey.VK_CONTROL).IsPressed;

            // RARE, but first priority: user can modify filename to get certain behaviors.
            // Check if the filename has been changed, in order to force post arguments.
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

            // If filename was not modified, check which keys were held down at .scr launch
            if (postArgs == "")
            {
                // these are exclusive
                if (fShiftKeyDown) postArgs += " " + POPDBGWIN;
                if (fControlKeyDown) postArgs += " " + STARTBUFFER;
            }

            // Examine incoming args and build outgoing args.
            if (mainArgs.Length < 1) // no args
            {
                // no args
                mode = M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 2) // 1 arg
            {
                // can only be:
                //  /S or 
                //  /C or 
                //  /C:windowHandle

                // these are exclusive, only one will ever be true
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/s") mode = M_SCREENSAVER;
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/c") mode = M_DT_CONFIGURE;

                if (mainArgs[0].ToLowerInvariant().Trim().StartsWith(@"/c:"))
                {
                    // get the chars after /c: for the windowHandle
                    mode = M_CP_CONFIGURE;
                    fHasWindowHandle = true;
                    windowHandle = mainArgs[0].Substring(3);
                }

            }
            else if (mainArgs.Length < 3) // 2 args
            {
                // can only be /P windowHandle
                mode = M_CP_MINIPREVIEW;
                fHasWindowHandle = true;
                windowHandle = mainArgs[1];
            }
            else
            {
                throw new ArgumentException("CommandLine had more than 2 arguments, could not parse.");
            }

            // Finish outgoing command line
            scrArgs = FROMSTUB + " " + mode;
            if (fHasWindowHandle)
            {
                scrArgs = scrArgs + " -" + windowHandle;
            }
            scrArgs += postArgs;

            // Decide whether to put up message box showing command line args.
            // Change fAlways to true if you want message box to pop up always.
            bool fAlways = false;

            if (fAltKeyDown || fAlways)
            {
                MessageBoxResult dr =  MessageBox(IntPtr.Zero, 
                    "Incoming cmdLine: " + System.Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                    "Outgoing cmdLine: " + TARGET + " " + scrArgs + Environment.NewLine + Environment.NewLine +
                    "Click OK to launch, Cancel to abort."
                    + Environment.NewLine + Environment.NewLine + debugOutput,
                    NAME, MessageBoxOptions.OkCancel);

                // if user clicks Cancel, don't launch the exe
                if (dr == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // Is the application there?
            if (!File.Exists(TARGET))  // 
            {
                MessageBoxResult dr = MessageBox(IntPtr.Zero, "File not found: " + TARGET, NAME, MessageBoxOptions.OkOnly);
                return;
            }

            // In the M_CP_CONFIGURE case, we need to wait for the process to exit
            // before we let the stub terminate. If we do not, Windows will 
            // immediately launch our Preview window again while the Settings dialog
            // is still up, and the Preview won't update when the Settings dialog closes.
            System.Diagnostics.Process proc = null;
            if (mode == M_CP_CONFIGURE)
            {
                proc = System.Diagnostics.Process.Start(TARGET, scrArgs);
                proc.WaitForExit();
                return;
            }
            else  // in all other cases, fire and forget
            {
                proc = System.Diagnostics.Process.Start(TARGET, scrArgs);
                return;
            }
        }

        ///<summary>
        /// Flags that define appearance and behaviour of a standard message box displayed by a call to the MessageBox function.
        /// </summary>    
        [Flags]
        public enum MessageBoxOptions : uint
        {
            OkOnly = 0x000000,
            OkCancel = 0x000001,
            AbortRetryIgnore = 0x000002,
            YesNoCancel = 0x000003,
            YesNo = 0x000004,
            RetryCancel = 0x000005,
            CancelTryContinue = 0x000006,
            IconHand = 0x000010,
            IconQuestion = 0x000020,
            IconExclamation = 0x000030,
            IconAsterisk = 0x000040,
            UserIcon = 0x000080,
            IconWarning = IconExclamation,
            IconError = IconHand,
            IconInformation = IconAsterisk,
            IconStop = IconHand,
            DefButton1 = 0x000000,
            DefButton2 = 0x000100,
            DefButton3 = 0x000200,
            DefButton4 = 0x000300,
            ApplicationModal = 0x000000,
            SystemModal = 0x001000,
            TaskModal = 0x002000,
            Help = 0x004000,
            NoFocus = 0x008000,
            SetForeground = 0x010000,
            DefaultDesktopOnly = 0x020000,
            Topmost = 0x040000,
            Right = 0x080000,
            RTLReading = 0x100000
        }

        /// <summary>
        /// Represents possible values returned by the MessageBox function.
        /// </summary>
        public enum MessageBoxResult : uint
        {
            Ok = 1,
            Cancel,
            Abort,
            Retry,
            Ignore,
            Yes,
            No,
            Close,
            Help,
            TryAgain,
            Continue,
            Timeout = 32000
        }
    }
}
