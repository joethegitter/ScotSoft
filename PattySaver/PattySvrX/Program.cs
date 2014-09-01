using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PattySvrX
{
    // 1. This is a stub .scr file. It launches our real exe.
    // 2. Although it is a WinForms project, it contains no forms, just the
    //    Main() method. (Using a Console project as a stub causes unacceptable
    //    flashing of the console window.)
    // 3. All this stub really does is capture the command line args passed to 
    //    us by Windoww, capture the keyboard state, and then launch our exe
    //    with new command line args based on the old args plus keyboard state.

    static class Program
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


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] mainArgs)
        {
            // The incoming command line for this stub will ONLY ever be (EB = Expected Behavior):
            // - /S                 - EB: run screensaver in fullscreen               - so we pass /screensaver
            // - /P windowHandle    - EB: put mini preview window in control panel    - so we pass /cp_minipreview -windowHandle  
            // - no args            - EB: show configure dlg on desktop               - so we pass /dt_configure
            // - /C                 - EB: show configure dlg on desktop               - so we pass /dt_configure
            // - /C:windowHandle    - EB: show configure dlg owned by control panel   - so we pass /cp_configure -windowHandle

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // capture the state of the Shift key and Control Key
            // holding down both keys at once leaves both of these false
            bool fShift = false;
            bool fControl = false;
            bool fAlt = false;
            if (Control.ModifierKeys == Keys.Alt) fAlt = true;
            if (Control.ModifierKeys == Keys.Shift) fShift = true;
            if (Control.ModifierKeys == Keys.Control) fControl = true;

            // first check the filename to see if we need to add post arguments
            string postArgs = "";
            if (Environment.GetCommandLineArgs()[0].ToLowerInvariant().Contains(FILE_DBGWIN.ToLowerInvariant()))
            {
                postArgs += " " + POPDBGWIN;
            }

            // only one is allowed, and POPDBGWIN takes precedence
            if (postArgs == "")
            {
                if (Environment.GetCommandLineArgs()[0].ToLowerInvariant().Contains(FILE_STARTBUFFER.ToLowerInvariant()))
                {
                    postArgs += " " + STARTBUFFER;
                }
            }

            // now check the keys held down at .scr launch
            if (postArgs == "")
            {
                // these are exclusive
                if (fShift) postArgs += " " + POPDBGWIN;
                if (fControl) postArgs += " " + STARTBUFFER;
            }

            // now examine incoming args to build outgoing args
            string scrArgs = "";
            if (mainArgs.Length < 1)
            {
                // no args
                scrArgs = FROMSTUB + " " + M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 2)
            {
                // can only be /S or /C or /C:windowHandle

                if (mainArgs[0].ToLowerInvariant().Trim().StartsWith(@"/c:"))
                {
                    // windowHandle = the chars after /c:
                     scrArgs = FROMSTUB + " " + M_CP_CONFIGURE + " -" + mainArgs[0].Substring(3);
                }
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/s") scrArgs = FROMSTUB + " " + M_SCREENSAVER;
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/c") scrArgs = FROMSTUB + " " + M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 3)
            {
                // can only be /P windowHandle
                scrArgs = FROMSTUB + " " + M_CP_MINIPREVIEW + " -" + mainArgs[1];
            }
            else
            {
                throw new ArgumentException("CommandLine had more than 2 arguments, could not parse.");
            }

            // Add postArg to scrArgs
            scrArgs += postArgs;

            // Decide whether to put up message box showing command line args
            // Change fAlways to true if you want message box to pop up always
            bool fAlways = false;

            if (fAlt || fAlways)
            {
                DialogResult dr = MessageBox.Show("CommandLine: " + System.Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                    "What we will pass along = " + scrArgs, Application.ProductName,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                // if user clicks Cancel, don't launch the exe
                if (dr == DialogResult.Cancel)
                {
                    return 0;
                }
            }

            // Launch our exe with the args we just built.
            System.Diagnostics.Process.Start("PattySaver.exe", scrArgs);

            // TODO: note that we assume exe is in same dir as .scr. Add code to look
            // in Pictures Directory, or to check a Registry Entry

            // Application.Run();
            // return value to force exit
            return 0;
        }
    }
}
