using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.LaunchManager;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver.LaunchManager
{
    class Modes
    {
        // All debug logging in this method should use Logging.LogLine, instead of System.Diagnostics.Debug.XXXX.
        // EntryPoint and LaunchManager need to support artificial debug output when in the control panel.

        static bool fDebugOutput = true;
        static bool fDebugOutputAtTraceLevel = false;
        static bool fDebugTrace = fDebugOutput && fDebugOutputAtTraceLevel;

        #region Public Fields and Enums

        // public static Form refToFullScreen;

        public static string nv = "<NoValueSet>";
        public static string dbgFirstArgVal = nv;               // unaltered value of the first command line arg
        public static string dbgSecondArgVal = nv;              // unaltered value of the second command line arg
        public static string dbgFirstArgBase = nv;              // inferred/transformed value of the first intended value
        public static long dbgFirstArgSubArg = (long)(-1);      // inferred/transformed value of the second intended value
        public static string dbgSettingsValAddition = nv;       // information added by the Settings Screen code
        public static string dbgScreenSaverValAddition = nv;    // information added by the Settings Screen code

        public static bool fVSHOSTED = false;
        public static bool fPopUpDebugOutputWindowOnTimer = false;
        public static bool fOpenInScreenSaverMode = true;
        public static bool fNoArgMode = true;
        public static bool UnofficialArgOverrideWindowed = false;
        public static bool fMaintainBuffer = false;

        public static LaunchModality LaunchMode = LaunchModality.Undecided;

        public enum LaunchModality
        {
            Configure = 10, Configure_WithWindowHandle = 11, Mini_Preview = 20, FullScreen = 30,
            /// <summary>
            /// Lauch mode has not yet been established.
            /// </summary>
            Undecided = 0,
            /// <summary>
            /// App should not launch, see NoLaunchReason.
            /// </summary>
            NOLAUNCH = -1
        }



        #endregion Public Fields and Enums



        #region Public Methods

        /// <summary>
        /// Returns the Launch Modality suggested by the command line args.
        /// </summary>
        /// <param name="hwndTargetWindow">Will be either (-1) or the hwnd of the desired parent window, to be used for various modes.</param>
        /// <param name="argsConsumed">Indicates if the first 0, 1 or 2 of the CmdLine args were part of the mode calcution, and therfore not private args.</param>
        /// <returns>Globals.LaunchModality enum.</returns>

        public static LaunchModality GetLaunchModalityFromCmdLineArgs(string[] incoming, out long hwndTargetWindow, out int argsConsumed)
        {
            // could be "/c:xxxxxxx" or "/c xxxxxxx", so we can't rely on GetCommandLineArgs() to break the cmdline correctly,
            // as it separates values on spaces only

            // if no valid representation in first or first and second arg, launch in Configure mode (same as no args),
            // but don't preclude 'umofficial' args

            // Acceptable cases:
            // 1. Zero arguments
            // 2. A single 'public' argument in the form of "/c", or "/s", or 
            //    "/c:xxxxxxx", "/p:xxxxxx", or "/c xxxxxxxx", or "/p xxxxxxx"; public argument
            //    must be the first argument; and then any number of 'private' arguments.
            //    Private arguments must also be separated by the "/" character. Private arguments 
            //    can have any number of sub-arguments, separated by spaces.
            // 3. Only private arguments.
            // We will only fail to launch if we get a valid two char argument, but a bad following window handle (xxxxxx).

            // In the case that the unofficial args contain "/window", we ignore all other data and force fullscreen.
            // That is handled outside of this method.


            Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): Entered.");

            hwndTargetWindow = (long)(-1);

            if (!(incoming.Length > 0))
            {
                Logging.LogLineIf(fDebugTrace, "   GetLaunchModalityFromCmdLineArgs(): No args, so Configure Mode.");
                argsConsumed = 0;
                Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): exiting.");
                return Modes.LaunchModality.Configure;                  // no args = Configure
            }

            dbgFirstArgVal = incoming[0];                                       // store away for debugging output

            string baseArg; long subArg; bool HasSubArg = false; bool SubArgParsesToLong = false;

            if (incoming.Length == 1)
            {
                if (IsValidSingleOrDoubleArg(incoming[0], out baseArg, out subArg, out HasSubArg, out SubArgParsesToLong))
                {
                    dbgFirstArgBase = baseArg;                                  // store away for debugging output
                    if (SubArgParsesToLong) dbgFirstArgSubArg = subArg;         // store away for debugging output

                    argsConsumed = 1;
                    if (SubArgParsesToLong) hwndTargetWindow = subArg;
                    Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): exiting to GetModality().");
                    return GetModality(baseArg, subArg, HasSubArg, SubArgParsesToLong);
                }
                else
                {
                    argsConsumed = 0;
                    Logging.LogLineIf(fDebugTrace, "   GetLaunchModalityFromCmdLineArgs(): IsValidSingleOrDoubleArg() returned false, so Configure Mode.");
                    Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): exiting.");
                    return LaunchModality.Configure;
                }
            }

            if (incoming.Length > 1)
            {
                bool hasSubArg;
                bool subArgParsesToLong;

                dbgSecondArgVal = incoming[1];                                  // store away for debugging output
                if (IsValidBaseArg(incoming[0], out baseArg))                   // first arg is valid
                {
                    argsConsumed = 1;
                    if (IsValidSubArg(incoming[1], baseArg, out subArg, out hasSubArg, out subArgParsesToLong))  // second arg is valid
                    {
                        dbgFirstArgBase = baseArg;                              // store away for debugging output
                        dbgFirstArgSubArg = subArg;                             // store away for debugging output

                        argsConsumed = 2;
                        HasSubArg = hasSubArg;
                        SubArgParsesToLong = subArgParsesToLong;
                        hwndTargetWindow = subArg;
                    }
                    else
                    {
                        HasSubArg = hasSubArg;
                        SubArgParsesToLong = subArgParsesToLong;
                    }
                    Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): exiting to GetModality().");
                    return GetModality(baseArg, subArg, HasSubArg, SubArgParsesToLong); // first arg was valid, second may not have been, let GetModality sort it out
                }
                else  // first arg not valid, so abandon all hope
                {
                    argsConsumed = 0;
                    Logging.LogLineIf(fDebugOutput, "   GetLaunchModalityFromCmdLineArgs(): IsValidBaseArg() returned false, so Configure Mode.");
                    Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): exiting.");
                    return LaunchModality.Configure;
                }
            }

            // how do we get here without having returned?
            argsConsumed = 0;
            Logging.LogLineIf(fDebugOutput, "GetLaunchModalityFromCmdLineArgs(): Falling through to Configure, HOW THE HELL DID WE GET HERE??");
            Logging.LogLineIf(fDebugTrace, "GetLaunchModalityFromCmdLineArgs(): exiting.");
            return LaunchModality.Configure;
        }

        private static LaunchModality GetModality(string baseArg, long subArg, bool HasSubArg, bool SubArgParsesToLong)
        {
            // coming in, baseArg will be valid two char
            // subArg will be valid if SubArgParsesToLong = True, invalid if False

            // Logic is:
            // c is okay by with or without handle
            // p always needs a handle
            // s never gets a handle

            // If p: if not (HasSubArg && SubArgParsesToLong), return NOLAUNCH w Log
            // If p: if HasSubArg and SubArgParsesToLong, return Preview
            // If s: If HasSubArg, return Full Screen  and log
            // If s: If not HasSubArg, return Full Screen
            // If c: not HasSubArg return Configure
            // If c: HasSubArg and not SubArgParsesToLong, return Configure (bad hwnd, just launch configure) (log)
            // If c: HasSubArg and SubArgParsesToLong, return Configure_WithHandle

            Logging.LogLineIf(fDebugTrace, "GetModality(): entered.");

            // save printable params for output
            string args = "baseArg: " + baseArg + ", subArg: " + subArg + ", HasSubArg: " + HasSubArg + ", SubArgParsesToLong: " + SubArgParsesToLong;

            Modes.LaunchModality retVal = LaunchModality.Undecided;

            if (baseArg == @"/p")
            {
                if (HasSubArg && SubArgParsesToLong)
                {
                    Logging.LogLineIf(fDebugTrace, "   GetModality(): returning Preview based on args: " + args);
                    retVal = LaunchModality.Mini_Preview;
                }
                else
                {
#if DEBUG
                    if (Logging.CannotLog())
                    {
                        MessageBox.Show("CommandLine: " + Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                            "Falling through to NOLAUNCH: Preview requested with no or bad hwnd: " + args, Application.ProductName,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
#endif
                    Logging.LogLineIf(fDebugOutput, "  * GetModality(): Falling through to NOLAUNCH: Preview requested with no or bad hwnd: " + args);
                    retVal = LaunchModality.NOLAUNCH;
                }
            }

            if (baseArg == @"/s")
            {
                if (HasSubArg)
                {
#if DEBUG
                    if (Logging.CannotLog())
                    {
                        MessageBox.Show("CommandLine: " + Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                            "Full Screen requested with hwnd: " + args, Application.ProductName,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
#endif
                    Logging.LogLineIf(fDebugTrace, "   GetModality(): FullScreen requested with hwnd: " + args);
                    retVal = LaunchModality.FullScreen;
                }
                else
                {
                    Logging.LogLineIf(fDebugTrace, "   GetModality(): returning FullScreen based on args: " + args);
                    retVal = LaunchModality.FullScreen;
                }
            }

            if (baseArg == @"/c")
            {
                if (!HasSubArg)
                {
                    Logging.LogLineIf(fDebugTrace, "   GetModality(): returning Configure based on args: " + args);
                    retVal = LaunchModality.Configure;
                }

                if (HasSubArg && SubArgParsesToLong)
                {
                    Logging.LogLineIf(fDebugTrace, "   GetModality(): returning Configure_WithHandle based on args: " + args);
                    retVal = LaunchModality.Configure_WithWindowHandle;
                }

                if (HasSubArg && !SubArgParsesToLong)
                {
#if DEBUG
                    if (Logging.CannotLog())
                    {
                        MessageBox.Show("CommandLine: " + Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                            " Falling through to Configure: Configure requested with bad hwnd: " + args, Application.ProductName,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
#endif
                    Logging.LogLineIf(fDebugOutput, "  * GetModality(): Falling through to Configure: Configure requested with bad hwnd: " + args);
                    retVal = LaunchModality.Configure;
                }
            }

            if (retVal == LaunchModality.Undecided)
            {
#if DEBUG
                if (Logging.CannotLog())
                {
                    MessageBox.Show("CommandLine: " + Environment.CommandLine + Environment.NewLine + Environment.NewLine +
                        "GetModality: Falling through to NOLAUNCH, dropped out of Switch with retVal unchanged: " + args, Application.ProductName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
#endif
                Logging.LogLineIf(fDebugOutput, "  * GetModality(): Falling through to NOLAUNCH, hitting DEFAULT CASE: " + args);
            }

            Logging.LogLineIf(fDebugTrace, "GetModality(): exiting.");
            return retVal;
        }

        /// <summary>
        /// Determines if a string is correctly formatted as either a single 'public' screen saver launch argument, or as a single argument plus sub-argument.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="baseArg">The detected two character base parameter will be stored here.</param>
        /// <param name="subArg">The detected (long) sub-argument will be stored here, or if SubArgParsesToLong = false, then (-1).</param>
        /// <param name="HasSubArg">If true, a valid single argument was detected, and a valid or non-valid sub-argument was detected.</param>
        /// <param name="SubArgParsesToLong">If true, a valid single argument was detected, and a valid sub-argument was detected.</param>
        /// <returns>True if either a valid single or valid single plus valid/invalid sub-argument was detected.</returns>

        private static bool IsValidSingleOrDoubleArg(string arg, out string baseArg, out long subArg, out bool HasSubArg, out bool SubArgParsesToLong)
        {
            // valid single: single = /x, where x is c or p or s.
            // valid double: valid single + ":xxxxxxx" where xxxxxx is a string that parses to a (long) value.

            baseArg = arg.ToLower().Trim();

            if (String.IsNullOrEmpty(baseArg))
            {
                subArg = (-1);
                HasSubArg = false;
                SubArgParsesToLong = false;
                return false;                                   // too empty
            }

            baseArg = baseArg.Substring(0, 2);

            if (!(IsValidBaseArg(baseArg, out baseArg)))     // cleaned up first arg placed into out var baseArg
            {
                subArg = (-1);
                HasSubArg = false;
                SubArgParsesToLong = false;
                return false;                                   // first two chars not valid, so bail 
            }

            // we're done with baseArg now, time to work on the other side
            if (arg.Length > 3)
            {
                if (arg.Substring(2, 1) == ":")              // test 3rd char for colon
                {
                    HasSubArg = true;

                    string testLong = arg.Substring(3);

                    if (long.TryParse(testLong, out subArg))    // puts the long value in subArg for return
                    {
                        SubArgParsesToLong = true;
                    }
                    else
                    {
                        SubArgParsesToLong = false;
                        subArg = (-1);
                    }
                    return true;                                // return true here no matter the value of sub-arg, let GetModality sort it out
                }
                else // no colon, so garbage value
                {
                    baseArg = "";
                    subArg = (-1);
                    HasSubArg = false;
                    SubArgParsesToLong = false;
                    return false;                               // not enough colon
                }
            }

            subArg = (-1);
            HasSubArg = false;
            SubArgParsesToLong = false;
            return true;                                        // arg was only two chars long, and was valid
        }

        private static bool IsValidSubArg(string arg, string subArgString, out long subArg, out bool HasSubArg, out bool SubArgParsesToLong)
        {

            subArgString = arg.ToLower().Trim();

            if (String.IsNullOrEmpty(subArgString))
            {
                subArg = (-1);
                HasSubArg = false;
                SubArgParsesToLong = false;
                return false;                                   // too empty
            }

            if (subArgString.Substring(0, 1) == @"/")
            {
                subArg = (-1);
                HasSubArg = false;
                SubArgParsesToLong = false;
                return false;                                    // it's a new baseArg, not a subArg
            }

            if (long.TryParse(subArgString, out subArg))    // puts the long value in subArg for return
            {
                HasSubArg = true;
                SubArgParsesToLong = true;
                return true;
            }
            else
            {
                HasSubArg = true;
                SubArgParsesToLong = false;
                return true;
            }
        }

        private static bool IsValidBaseArg(string arg, out string baseArg)
        {
            baseArg = arg.ToLower().Trim();

            if (String.IsNullOrEmpty(baseArg))
            {
                return false;                                   // too empty
            }

            if (baseArg.Length < 2)
            {
                baseArg = "";
                return false;                                    // too short
            }

            baseArg = baseArg.Substring(0, 2);

            if ((baseArg != @"/c") &&
                (baseArg != @"/p") &&
                (baseArg != @"/s"))
            {
                baseArg = "";
                return false;                                   // too 'not the right letters'   
            }
            else
            {
                return true;
            }
        }

        #endregion Methods


    }

}
