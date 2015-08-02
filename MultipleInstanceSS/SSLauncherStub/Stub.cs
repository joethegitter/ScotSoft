using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JKSoft
{
    static class Stub
    {
        // Filename to launch
        public const string PATH = @"C:\Users\LocallyMe\Source\Repos\ScotSoft\MultipleInstanceSS\MultipleInstanceSS\bin\Debug\";
        public const string TARGET_BASE = "MultipleInstanceSS";
        public const string TARGET_EXT = ".exe";
        public const string TARGET = PATH + TARGET_BASE + TARGET_EXT;

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
        static void Main(string[] mainArgs)
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            // Application.Run(new Form1());

            string debugOutput = "";
            string scrArgs = "";
            string mode = "";
            bool fWindowHandle = false;
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

            // now examine incoming args and build outgoing args
            if (mainArgs.Length < 1)
            {
                // no args
                mode = M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 2)
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
                    fWindowHandle = true;
                    windowHandle = mainArgs[0].Substring(3);
                }

            }
            else if (mainArgs.Length < 3)
            {
                // can only be /P windowHandle
                mode = M_CP_MINIPREVIEW;
                fWindowHandle = true;
                windowHandle = mainArgs[1];
            }
            else
            {
                throw new ArgumentException("CommandLine had more than 2 arguments, could not parse.");
            }

            scrArgs = FROMSTUB + " " + mode;

            if (fWindowHandle)
            {
                scrArgs = scrArgs + " -" + windowHandle;
            }

            // Decide whether to put up message box showing command line args
            // Change fAlways to true if you want message box to pop up always
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

            if (mode == M_CP_MINIPREVIEW)
            {
                procPreview = System.Diagnostics.Process.Start(TARGET, scrArgs);
                return;
            }
            else if (mode == M_CP_CONFIGURE)
            {
                procConfigure = System.Diagnostics.Process.Start(TARGET, scrArgs);
                procConfigure.WaitForExit();
                return;
            }
            else
            {
                return;
            }
        }
    }
}
