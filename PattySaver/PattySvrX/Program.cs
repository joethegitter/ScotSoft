using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PattySvrX
{
    // This is a stub .scr file. Although it pretends to be our screensaver, all 
    // it does is launch our actual .exe, passing along the arguments that 
    // Windows fed to it.
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string xargs = @"/x " + System.Environment.CommandLine;
            System.Diagnostics.Process.Start("PattySaver.exe", xargs);
            // Application.Run(new Form1());
            return 0;
        }
    }
}
