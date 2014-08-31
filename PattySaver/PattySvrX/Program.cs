using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PattySvrX
{
    // This is a stub .scr file. Although it pretends to be our screensaver, all 
    // it really does is launch our actual .exe with commandline instructions on what to do.
    static class Program
    {
        public const string DBGWIN = ".dbgwin";   // appended by user to enable debug output window in non-hosted build

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] mainArgs)
        {
            // The command line for this stub will ONLY ever be (EB = Expected Behavior):
            // - /S                 - EB: run screensaver in fullscreen               - so we pass /screensaver
            // - /P windowHandle    - EB: put mini preview window in control panel    - so we pass /cp_minipreview -windowHandle  
            // - no args            - EB: show configure dlg on desktop               - so we pass /dt_configure
            // - /C                 - EB: show configure dlg on desktop               - so we pass /dt_configure
            // - /C:windowHandle    - EB: show configure dlg owned by control panel   - so we pass /cp_configure -windowHandle

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // If name of .scr has .dbgwin in it, pass along request to pop up 
            // debugoutputwindow on timer
            string postArg = "";
            if (Environment.GetCommandLineArgs()[0].ToLowerInvariant().Contains(DBGWIN.ToLowerInvariant()))
            {
                postArg = @" /dbgwin";
            }

            string scrArgs = "";
            if (mainArgs.Length < 1)
            {
                // no args
                scrArgs = @"/scr /dt_configure";
            }
            else if (mainArgs.Length < 2)
            {
                // can only be /S or /C or /C:windowHandle

                if (mainArgs[0].ToLowerInvariant().Trim().StartsWith(@"/c:"))
                {
                    // windowHandle = the chars after /c:
                    scrArgs = @"/scr /cp_configure -" + mainArgs[0].Substring(3);
                }
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/s") scrArgs = @"/scr /screensaver";
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/c") scrArgs = @"/scr /dt_configure";
            }
            else if (mainArgs.Length < 3)
            {
                // can only be /P windowHandle
                scrArgs = @"/scr /cp_minipreview -" + mainArgs[1];
            }
            else
            {
                throw new ArgumentException("CommandLine had more than 2 arguments, could not parse.");
            }

#if DEBUG
            // Uncomment the following lines and in DEBUG builds 
            // we'll put up this dialog every time we launch, showing incoming command line and what this stub
            // will send the exe.

            //MessageBox.Show("CommandLine: " + System.Environment.CommandLine + Environment.NewLine + Environment.NewLine +
            //    "What we will pass along = " + scrArgs, Application.ProductName,
            //    MessageBoxButtons.OK, MessageBoxIcon.Information);
            //return 0;
#endif
            // Add postArg to scrArgs
            scrArgs += postArg;

            // Launch our exe with the args we just built.
            System.Diagnostics.Process.Start("PattySaver.exe", scrArgs);

            // TODO: note that we assume exe is in same dir as .scr. Add code to look
            // in Pictures Directory, or to check a Registry Entry

            // Application.Run();
            return 0;
        }
    }
}
