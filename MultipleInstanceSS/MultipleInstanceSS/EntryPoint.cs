using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using ScotSoft.PattySaver.DebugUtils;
using ScotSoft;
using ScotSoft.PattySaver;

namespace JKSoft
{
    static class EntryPoint
    {
        // command line strings and file extensions
        public const string VSHOSTED = ".vshost";                   // string appended to our exe basename by VS when hosted
        public const string FROMSTUB = @"/scr";                     // tells us that our exe was launched from our .scr stub
        public const string POPDBGWIN = @"/popdbgwin";                    // pop up debugOutputWindow on timer after launch
        public const string STARTBUFFER = @"/startbuffer";          // place debug output in string buffer from the moment of launch

        // command line strings for launch modes
        public const string M_CP_CONFIGURE = @"/cp_configure";      // open settings dlg in control panel
        public const string M_CP_MINIPREVIEW = @"/cp_minipreview";  // open miniPreview form in control panel
        public const string M_DT_CONFIGURE = @"/dt_configure";      // open settings dlg on desktop
        public const string M_SCREENSAVER = @"/screensaver";        // open screenSaverForm
        public const string M_NO_MODE = "no_mode";                  // open screenSaverForm in windowed mode

        public static List<string> scrArgs = new List<string>() { M_CP_CONFIGURE, M_CP_MINIPREVIEW, M_DT_CONFIGURE, M_SCREENSAVER };

        public const string OLDCONFIGURE = @"/c";                   //  same as M_DT_CONFIGURE - but we will ignore any window handles
        public const string OLDSCREENSAVER = @"/s";                 //  same M_SCREENSAVER 

        public static List<string> oldArgs = new List<string>() { OLDCONFIGURE, OLDSCREENSAVER };

        // debug output controllers
        public static bool fDebugOutput = true;                                    // controls whether any debug output is emitted
        public static bool fDebugOutputAtTraceLevel = true;                        // impacts the granularity of debug output
        public static bool fDebugTrace = fDebugOutput && fDebugOutputAtTraceLevel; // controls the granularity of debug output
        public static bool fShowDebugOutputWindow = true;
        public static bool fDebugOutputWindowIsShown = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] mainArgs)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logging.AddLogDestination(Logging.LogDestination.Default);
            Logging.AddLogDestination(Logging.LogDestination.Buffer);

            Logging.LogLineIf(fDebugTrace, "Main() entered.");
            Logging.LogLineIf(fDebugOutput, "   Main(): CommandLine was: " + Environment.CommandLine);
            Logging.LogLineIf(fDebugOutput, "   Main(): calling ParseCommandLineAndLaunch()...");

            ParseCommandLineAndLaunch(mainArgs);

            Logging.LogLineIf(fDebugTrace, "Main() exiting.");
        }

        static void ParseCommandLineAndLaunch(string[] mainArgs)
        {
            Logging.LogLineIf(fDebugTrace, "ParseCommandLineAndLaunch() entered.");

            IntPtr hWnd = IntPtr.Zero;
            string launchString = M_SCREENSAVER;
            int lastProcessedIndex = -1;

            // first argument must be /scr
            if ((mainArgs.Length > 0) && (mainArgs[0].ToLowerInvariant() != @"/scr"))
            {
                throw new ArgumentException(@"CommandLine: /scr can only be the first argument." +
                Environment.NewLine + Environment.CommandLine);
            }
            lastProcessedIndex = 0;

            // second arg must be one of four valid /scr-related arguments
            if ((mainArgs.Length > 1) && !scrArgs.Contains(mainArgs[1].ToLowerInvariant()))
            {
                throw new ArgumentException(@"CommandLine: /scr can only be followed by a valid /scr-related argument." +
                Environment.NewLine + Environment.CommandLine);
            }
            lastProcessedIndex = 1;

            // if second arg starts with cp_ it must be followed with a valid window handle
            if (mainArgs[1].ToLowerInvariant() == M_CP_CONFIGURE || mainArgs[1].ToLowerInvariant() == M_CP_MINIPREVIEW)
            {
                if ((mainArgs.Length > 2) && mainArgs[2].ToLowerInvariant().StartsWith("-"))
                {
                    string subArg = mainArgs[2].ToLowerInvariant();
                    string longCandidate = subArg.Substring(1);
                    if (!String.IsNullOrEmpty(longCandidate))
                    {
                        long val;
                        bool worked = long.TryParse(longCandidate, out val);
                        if (worked)
                        {
                            Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): passed hWnd was " + DecNHex(val));

                            hWnd = new IntPtr(val);
                        }
                        else  // bad parse
                        {
                            throw new ArgumentException(@"CommandLine: invalid window handle passed: " + longCandidate +
                                Environment.NewLine + Environment.CommandLine);
                        }
                    }
                    else   // null or empty
                    {
                        throw new ArgumentException(@"CommandLine: invalid window handle passed." +
                            Environment.NewLine + Environment.CommandLine);
                    }
                }
                else  // missing required sub argument
                {
                    throw new ArgumentException(@"CommandLine: /cp_ argument missing required subargument." +
                        Environment.NewLine + Environment.CommandLine);
                }
                lastProcessedIndex = 2;
            }

            // by this point, our mode is in mainArgs[1] and hWnd is either IntPtr.Zero or a numerically validated hWnd.
            launchString = mainArgs[1].ToLowerInvariant();

            // launch
            if (launchString == M_CP_MINIPREVIEW)
            {
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): target form is CPPreview.");
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): calling new CP_Preview("+ DecNHex(hWnd) +")...");
                CP_Preview preview = new CP_Preview(hWnd);
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Constructor returned. Window handle is: " + DecNHex(preview.Handle));

                int error = 0;

                // Determine what the initial parent of our form is.
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Getting initial value of parent: Calling GetParent(" + DecNHex(preview.Handle) + ")...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr originalParent = NativeMethods.GetParent(preview.Handle);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetParent() returned IntPtr = " + DecNHex(originalParent));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Set the passed hWnd to be the parent of the form window.
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Changing parent to passed hWnd: Calling SetParent(" + DecNHex(preview.Handle) + ", " + DecNHex(hWnd) + ")...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr newParent = NativeMethods.SetParent(preview.Handle, hWnd);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): SetParent() returned IntPtr = " + DecNHex(newParent));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Verify if the form now has the expected new parent.
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Verifying new parent: Calling GetParent(" + DecNHex(preview.Handle) + ")...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr verifyParent = NativeMethods.GetParent(preview.Handle);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetParent() returned IntPtr = " + DecNHex(verifyParent));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Set the size of the form to the size of the passed hWnd
                System.Drawing.Rectangle ParentRect = new System.Drawing.Rectangle();
                NativeMethods.SetLastErrorEx(0, 0);
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Calling GetClientRect(" + DecNHex(hWnd) +")...");
                bool fSuccess = NativeMethods.GetClientRect(hWnd, ref ParentRect);
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetClientRect() returned bool = " + fSuccess + ", rect = " + ParentRect.ToString());
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // Set our size to new rect and location at (0, 0)
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Setting Size and Position with C# code:");
                preview.Size = ParentRect.Size;
                preview.Location = new System.Drawing.Point(0, 0);


                //// Do the Win32 API magic
                //// Get initial value of WindowLong
                //IntPtr WindowLong1 = new IntPtr();
                //int index = (int)NativeMethods.WindowLongFlags.GWL_STYLE;
                //Logging.LogLineIf(fDebugTrace, " ");
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Getting initial value of window style: calling GetWindowLongPtr(" + DecNHex(preview.Handle) + ", " + DecNHex(index) + ")...");
                //NativeMethods.SetLastErrorEx(0, 0);
                //WindowLong1 = NativeMethods.GetWindowLongPtr(preview.Handle, index);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetWindowLongPtr() returned long pointer: " + DecNHex(WindowLong1));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                //// Get modified value of WindowLong
                //IntPtr WindowLong2 = new IntPtr();
                //index = (int)NativeMethods.WindowLongFlags.GWL_STYLE | 0x40000000;
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Getting modified window style: calling GetWindowLongPtr(" + DecNHex(preview.Handle) + ", " + DecNHex(index) + ")...");
                //NativeMethods.SetLastErrorEx(0, 0);
                //WindowLong2 = NativeMethods.GetWindowLongPtr(preview.Handle, index);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetWindowLongPtr() returned long pointer: " + DecNHex(WindowLong2));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                //// Use modified value of WindowLong to set the new window style.
                //object ohRef = new object();
                //HandleRef hRef = new HandleRef(ohRef, preview.Handle);
                //IntPtr ip2 = new IntPtr();

                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Setting new window style: calling SetWindowLongPtr(" + DecNHex(hRef) + ", " + DecNHex(index) + ", " + DecNHex(WindowLong1) + ")");
                //NativeMethods.SetLastErrorEx(0, 0);
                //index = (int)NativeMethods.WindowLongFlags.GWL_STYLE;
                //ip2 = NativeMethods.SetWindowLongPtr(hRef, index, WindowLong1);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): SetWindowLongPtr() returned IntPtr: " + DecNHex(ip2) + ", which should be: " + DecNHex(WindowLong1));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                //// For kicks, let's learn the current Parent of our Window.
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Who is the original parent? Calling GetParent(" + DecNHex(preview.Handle) + ")");
                //NativeMethods.SetLastErrorEx(0, 0);
                //IntPtr originalParent = NativeMethods.GetParent(preview.Handle);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetParent() returned IntPtr = " + DecNHex(originalParent));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                //// Now make the passed hWnd miniprev's parent window.
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Changing parent to passed hWnd: Calling SetParent(" + DecNHex(preview.Handle) + ", " + DecNHex(hWnd) + ")");
                //NativeMethods.SetLastErrorEx(0, 0);
                //IntPtr newParent = NativeMethods.SetParent(preview.Handle, hWnd);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): SetParent() returned IntPtr = " + DecNHex(newParent));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                //// For kicks, let's verify the new parent.
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): New parent should be " + DecNHex(newParent) + ": Calling GetParent(" + DecNHex(preview.Handle) + ")");
                //NativeMethods.SetLastErrorEx(0, 0);
                //IntPtr currentParent = NativeMethods.GetParent(preview.Handle);
                //error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetParent() returned IntPtr = " + DecNHex(currentParent));
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");


                //// Set miniprev window size to the size of our window's new parent.
                //// First, get that size.
                //System.Drawing.Rectangle ParentRect = new System.Drawing.Rectangle();
                //NativeMethods.SetLastErrorEx(0, 0);
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Calling GetClientRect to get a new rect for our form:");
                //bool fSuccess = NativeMethods.GetClientRect(hWnd, ref ParentRect);
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetClientRect() returned bool = " + fSuccess);
                //Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                //Logging.LogLineIf(fDebugTrace, " ");

                //// Set our size to new rect and location at (0, 0)
                //Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Setting Size and Position with C# code:");
                //preview.Size = ParentRect.Size;
                //preview.Location = new System.Drawing.Point(0, 0);

                //// Show with owner = passed hwnd
                //NativeMethods.WindowWrapper ww = new NativeMethods.WindowWrapper(hwnd);
                //miniprev.Show(ww);

                // Show the form
                Logging.LogLineIf(fDebugTrace, " ");
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Calling preview.Show()...");
                preview.Show();
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Show() has returned.");


                // Run the app
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Calling Application.Run():");
                Application.Run(preview);
            }
            else if (launchString == M_CP_CONFIGURE)
            {
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): target form is CP_Configuration.");
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): calling new CP_Configuration()...");
                CP_Configuration configDlg = new CP_Configuration();
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Constructor returned. configDlg window handle is: " + DecNHex(configDlg.Handle));

                int error = 0;

                // Get the root owner of the passed hWnd
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): Getting Root Ancestor: calling GetAncestor(hWnd, GetRoot)...");
                NativeMethods.SetLastErrorEx(0, 0);
                IntPtr passedWndRoot = NativeMethods.GetAncestor(hWnd, NativeMethods.GetAncestorFlags.GetRoot);
                error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Logging.LogLineIf(fDebugTrace, "  ParseCommandLineAndLaunch(): GetAncestor() returned IntPtr: " + EntryPoint.DecNHex(passedWndRoot));
                Logging.LogLineIf(fDebugTrace, "      GetLastError() returned: " + error.ToString());
                Logging.LogLineIf(fDebugTrace, " ");

                // and show ourselves modal to that window
                NativeMethods.WindowWrapper ww = new NativeMethods.WindowWrapper(passedWndRoot);
                DialogResult dr = configDlg.ShowDialog(ww);
            }

            Logging.LogLineIf(fDebugTrace, "ParseCommandLineAndLaunch() exiting.");
        }

        public static string DecNHex(int val)
        {
            return val.ToString("x") + "/" + val.ToString();
        }

        public static string DecNHex(long val)
        {
            return val.ToString("x") + "/" + val.ToString();
        }

        public static string DecNHex(IntPtr val)
        {
            return val.ToString("x") + "/" + val.ToString();
        }

        public static string DecNHex(HandleRef val)
        {
            return val.Handle.ToString("x") +  "/" + val.Handle.ToString();
        }



    }
}
