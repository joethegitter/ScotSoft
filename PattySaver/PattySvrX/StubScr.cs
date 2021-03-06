﻿using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;

// BUILD NOTE - do not change the build option to "Prefer 32 bit", as this will
//    cause Windows to reject the stub when it is launched with a "This 
//    application could not be started" error. Windows will not allow apps
//    in the System32 directory to launch if that build option is chosen.

namespace PattySvrX
{

    // 1. This is a stub .scr file. Windows thinks that it is our screen saver;
    //    in reality, it is just a little stub that launches our application
    //    with the appropriate screen-saver related arguments.
    // 2. Although this stub project is a WinForms project, it contains no 
    //    forms, just the Main() method. We use a WinForms project instead of
    //    a Console project because the Console project will flash up a console
    //    window at startup.
    // 3. All this stub really does is capture the command line args passed to 
    //    us by Windows, capture the keyboard state, capture the stub filename,
    //    and then launch our application with improved command line args 
    //    based on all of that captured data.

    static class StubScr
    {

        // Use this "Path" constant to have the stub launch the development 
        // version of our application from your development directory.  
        // Otherwise, leave it empty, and the stub will expect to find 
        // our application in the same directory as the stub.

#if LAUNCH_APP_FROM_DEV_PATH
        public const string PATH = @"C:\Users\LocallyMe\Source\Repos\ScotSoft\PattySaver\PattySaver\bin\Debug\";
#else
        public const string PATH = Application.StartupPath;
#endif

        // Filename of application the stub will launch
        public const string TARGET_BASE = "PattySaver";
        public const string TARGET_EXT = ".exe";
        public const string TARGET = PATH + @"\" + TARGET_BASE + TARGET_EXT;

        // Filename elements, command line args and keystates that tell our
        // application to pop up debugOutputWindow on a timer after launch.
        public const string FILE_DBGWIN = ".popdbgwin";
        public const string POPDBGWIN = @"/popdbgwin";
        public static bool fShiftKeyOnly = false;

        // Filename elements, command line args and keystates that tell our
        // application to immediately start storing debug output in a 
        // a buffer at launch (we normally only start when the debug window
        // is opened, so we miss startup data)
        public const string FILE_STARTBUFFER = ".startbuffer"; 
        public const string STARTBUFFER = @"/startbuffer";
        public static bool fControlKeyOnly = false;

        // Command line args that the stub will issue to our application:
        public const string FROMSTUB = @"/scr";                     // tells us that our exe was launched from the stub
        public const string M_CP_CONFIGURE = @"/cp_configure";      // open settings dlg in control panel
        public const string M_CP_MINIPREVIEW = @"/cp_minipreview";  // open miniPreview form in control panel
        public const string M_DT_CONFIGURE = @"/dt_configure";      // open settings dlg on desktop
        public const string M_SCREENSAVER = @"/screensaver";        // open screenSaverForm

        // Keystate that tells us to show the args received by the stub plus
        // the launch string the stub will use to launch our application in a
        // message box, before launching our app.
        public static bool fAltKeyOnly = false;

        /// <summary>
        /// The main entry point for the stub.
        /// </summary>
        [STAThread]
        static void Main(string[] mainArgs)
        {
            // WinForms boilerplate, ignore
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string debugOutput = "";
            string scrArgs = "";
            string mode = "";
            bool fHasWindowHandle = false;
            string windowHandle = "";

            // The incoming command line to this stub will ONLY ever be (EB = Expected Behavior):
            //  /S                 - EB: run screensaver in fullscreen               - so we pass /screensaver
            //  /P windowHandle    - EB: put mini preview window in control panel    - so we pass /cp_minipreview -windowHandle  
            //  no args            - EB: show configure dlg on desktop               - so we pass /dt_configure
            //  /C                 - EB: show configure dlg on desktop               - so we pass /dt_configure
            //  /C:windowHandle    - EB: show configure dlg owned by control panel   - so we pass /cp_configure -windowHandle

            // Capture the state of the Shift key and Control Key and ALT keys at .scr launch.
            // Note that in this implementation, we only check to see if each key is the ONLY modifier
            // key being pressed. Combinations of these keys will do nothing.
            if (Control.ModifierKeys == Keys.Alt) fAltKeyOnly = true;
            if (Control.ModifierKeys == Keys.Shift) fShiftKeyOnly = true;
            if (Control.ModifierKeys == Keys.Control) fControlKeyOnly = true;

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
                if (fShiftKeyOnly) postArgs += " " + POPDBGWIN;
                if (fControlKeyOnly) postArgs += " " + STARTBUFFER;
            }

            // Examine incoming args and build outgoing args.
            if (mainArgs.Length < 1) // no args
            {
                mode = M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 2) // 1 arg
            {
                // can only be:
                //  /S or 
                //  /C or 
                //  /C:windowHandle    - note this is a single arg, no space in it

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
            // Change fShowArgsAlways to true if you want message box to pop up always.
            bool fShowArgsAlways = false;

            if (fAltKeyOnly || fShowArgsAlways)
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

            // Is the application where we think it is?
            if (!File.Exists(TARGET))  // 
            {
                DialogResult dr = MessageBox.Show("File not found: " + TARGET,Application.ProductName, MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }

            // In the M_CP_CONFIGURE case: don't let the stub process die until 
            // the instance of our app running the Settings dialog dies. If you 
            // don't wait, then when the Settings dialog is dismissed, the mini
            // preview won't read the Settings changes, and won't update itself.
            System.Diagnostics.Process proc = null;
            if (mode == M_CP_CONFIGURE)
            {
                proc = System.Diagnostics.Process.Start(TARGET, scrArgs);
                proc.WaitForExit();  // don't let stub die until app dies
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
