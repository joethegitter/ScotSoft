using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace PattySvrX
{
    // 1. This is a stub .scr file. Windows thinks that it is our screen saver;
    //    in reality, it is just a little stub that launches our executable
    //    with the appropriate arguments.
    // 2. Although it is a WinForms project, it contains no forms, just the
    //    Main() method. (Using a Console project as a stub causes unacceptable
    //    flashing of the console window.)
    // 3. All this stub really does is capture the command line args passed to 
    //    us by Windows, capture the keyboard state, and then launch our exe
    //    with new command line args based on the old args plus keyboard state.

    static class StubScr
    {
        // Use this "Path" constant to have the stub launch the development 
        // version of PattySaver.exe from your development directory.  
        // Otherwise, leave it empty, and the stub will expect to find 
        // PattSaver.exe in the same dir as the stub.

#if USE_JOE_DEV_DIR
        public const string PATH = @"C:\Users\LocallyMe\Source\Repos\ScotSoft\PattySaver\PattySaver\bin\Debug\"; 
#elif USE_SCOT_DEV_DIR
        public const string PATH = @"C:\"; 
#else
        public const string PATH = "";
#endif

        // Filename the stub will launch
        public const string TARGET_BASE = "PattySaver";
        public const string TARGET_EXT = ".exe";
        public const string TARGET = PATH + TARGET_BASE+TARGET_EXT;

        // Filename elements and command line arguments that tell our
        // executable to pop up debugOutputWindow on a timer after launch.
        public const string FILE_DBGWIN = ".popdbgwin";
        public const string POPDBGWIN = @"/popdbgwin";

        // Filename elements and command line arguments that tell our
        // executable to immediately start storing debug output in a 
        // a buffer at launch (as opposed to when we open the 
        // debugOutputWindow).
        public const string FILE_STARTBUFFER = ".startbuffer"; 
        public const string STARTBUFFER = @"/startbuffer";

        // Command line args that the stub will issue to our executable
        public const string FROMSTUB = @"/scr";                     // tells us that our exe was launched from the stub
        public const string M_CP_CONFIGURE = @"/cp_configure";      // open settings dlg in control panel
        public const string M_CP_MINIPREVIEW = @"/cp_minipreview";  // open miniPreview form in control panel
        public const string M_DT_CONFIGURE = @"/dt_configure";      // open settings dlg on desktop
        public const string M_SCREENSAVER = @"/screensaver";        // open screenSaverForm

        /// <summary>
        /// The main entry point for the stub.
        /// </summary>
        [STAThread]
        static void Main(string[] mainArgs)
        {
            string debugOutput = "";
            string scrArgs = "";
            string mode = "";
            bool fHasWindowHandle = false;
            string windowHandle = "";

            // The incoming command line for this stub will ONLY ever be (EB = Expected Behavior):
            //  /S                 - EB: run screensaver in fullscreen               - so we pass /screensaver
            //  /P windowHandle    - EB: put mini preview window in control panel    - so we pass /cp_minipreview -windowHandle  
            //  no args            - EB: show configure dlg on desktop               - so we pass /dt_configure
            //  /C                 - EB: show configure dlg on desktop               - so we pass /dt_configure
            //  /C:windowHandle    - EB: show configure dlg owned by control panel   - so we pass /cp_configure -windowHandle

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
                if (fShift) postArgs += " " + POPDBGWIN;
                if (fControl) postArgs += " " + STARTBUFFER;
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

            if (fAlt || fAlways)
            {
                DialogResult dr = MessageBox.Show("Incoming cmdLine: " + System.Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                    "Outgoing cmdLine: " + TARGET + " " + scrArgs + Environment.NewLine + Environment.NewLine +
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

            // In the M_CP_CONFIGURE case, we need to wait for the process to exit.
            // Otherwise, Windows will immediately launch our Preview window again,
            // and it won't update when the Configure dialog closes.
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
    }
}
