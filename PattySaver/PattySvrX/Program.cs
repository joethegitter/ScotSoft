using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;

using ScotSoft.PattySaver;

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

        // SHARED CONSTANTS - UPDATE IN ALL FILES
        const string CP_MINIPREVIEW_TITLEBARBASE = "MiniPreviewInControlPanelForm - ProcessID = ";

        // Filename to launch
        public const string TARGET_BASE = "PattySaver";
        public const string TARGET_EXT = ".exe";
        public const string TARGET = TARGET_BASE+TARGET_EXT;

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
            string debugOutput = "";
            
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
            string scrArgs = "";
            if (mainArgs.Length < 1)
            {
                // no args
                scrArgs = FROMSTUB + " " + M_DT_CONFIGURE;
            }
            else if (mainArgs.Length < 2)
            {
                // can only be:
                //  /S or 
                //  /C or 
                //  /C:windowHandle

                // these are exclusive, only one will ever be true
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/s") scrArgs = FROMSTUB + " " + M_SCREENSAVER;
                if (mainArgs[0].ToLowerInvariant().Trim() == @"/c") scrArgs = FROMSTUB + " " + M_DT_CONFIGURE;
                if (mainArgs[0].ToLowerInvariant().Trim().StartsWith(@"/c:"))
                {
                    // get the chars after /c: for the windowHandle
                    scrArgs = FROMSTUB + " " + M_CP_CONFIGURE + " -" + mainArgs[0].Substring(3);
                }

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

            // testing some stuff
            // Determine if there is a MiniPrev instance running already
             //System.Diagnostics.Process[] proc = System.Diagnostics.Process.GetProcessesByName(TARGET_BASE);
            System.Diagnostics.Process p = System.Diagnostics.Process.GetProcessById(4480);

            string pid = p.Id.ToString();
            debugOutput += "For process id: " + pid + Environment.NewLine;
            debugOutput += "   p.ProcessName = " + p.ProcessName + Environment.NewLine;
            debugOutput += "   p.MainWindowHandle = " + p.MainWindowHandle.ToString() + Environment.NewLine;
            debugOutput += "   p.MainWindowTitle = " + p.MainWindowTitle + Environment.NewLine;
            debugOutput += "   p.MainModule.ModuleName = " + p.MainModule.ModuleName + Environment.NewLine;
            debugOutput += "   p.Responding = " + p.Responding.ToString() + Environment.NewLine;
            debugOutput += Environment.NewLine;


            //if (proc.Length > 0)
            //{
            //    int index = 0;
            //    bool miniPrevRunning = false;
            //    foreach (System.Diagnostics.Process p in proc)
            //    {
            //        index++;

            //        // get the id of that process
            //        string pid = p.Id.ToString();
            //        debugOutput += "For process id: " + pid + Environment.NewLine;
            //        debugOutput += "   p.ProcessName = " + p.ProcessName + Environment.NewLine;
            //        debugOutput += "   p.MainWindowHandle = " + p.MainWindowHandle.ToString() + Environment.NewLine;
            //        debugOutput += "   p.MainWindowTitle = " + p.MainWindowTitle + Environment.NewLine;
            //        debugOutput += "   p.MainModule.ModuleName = " + p.MainModule.ModuleName + Environment.NewLine;
            //        debugOutput += "   p.Responding = " + p.Responding.ToString() + Environment.NewLine;
            //        debugOutput += Environment.NewLine;

            //        if (index > 5) break;



            //        //// build the target titlebar text
            //        //string windowtext = CP_MINIPREVIEW_TITLEBARBASE + pid;
            //        //// find a window that has the targeted title bar text
            //        //NativeMethods.SetLastErrorEx(0, 0);
            //        //IntPtr miniPrevWindow = NativeMethods.FindWindowByCaption(IntPtr.Zero, windowtext);
            //        //int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

            //    }
            //}


            // Decide whether to put up message box showing command line args
            // Change fAlways to true if you want message box to pop up always
            bool fAlways = true;

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
                    return 0;
                }
            }

            //// if we're about to send M_CP_MINIPREVIEW, bail if we've already got one going
            //if (scrArgs.Contains(M_CP_MINIPREVIEW))
            //{
            //    System.Diagnostics.Process[] proc = System.Diagnostics.Process.GetProcessesByName(TARGET_BASE);
            //    if (proc.Length > 0)
            //    {
            //        return 1;
            //    }
            //}      
            

            //// if we receive the M
            //if (scrArgs.Contains(M_CP_CONFIGURE))
            //{
            //}

            // if we were launched from the Settings button, the Control Panel is going to suspend
            // our process (or at least hide our window, which is somehow impacting this).  That means
            // that if launch right away, our process won't get the nextInstance message.


            // Launch our exe with the args we just built.
            System.Diagnostics.Process.Start(TARGET, scrArgs);

            // TODO: note that we assume exe is in same dir as .scr. Add code to look
            // in Pictures Directory, or to check a Registry Entry

            // Application.Run();
            // return value to force exit
            return 0;
        }
    }
}
