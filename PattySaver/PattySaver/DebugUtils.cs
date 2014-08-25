using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.LaunchManager;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver.DebugUtils
{
    class Logging
    {
        public enum LogDestination
        {
            Default,
            String
        };

        static public LogDestination dest = LogDestination.Default;

        static public LogDestination Destination { get; set; }

        static public string strBuffer = "";

        static private Form dbgWindow = null;

        static private TextBox tx = null;

        static public bool ShowHideDebugWindow(Form callingForm)
        {
            if (!Modes.fAllowUseOfDebugOutputWindow)
            {
                return false;
            }

            if (dbgWindow == null)  // has not been created yet
            {
                if ((callingForm.TopMost == true) && (callingForm.WindowState == FormWindowState.Maximized))
                {
                    // order is important, do not modify
                    callingForm.ShowIcon = true;
                    callingForm.MaximizeBox = true;
                    callingForm.MinimizeBox = true;
                    callingForm.ControlBox = true;
                    callingForm.ShowInTaskbar = true;
                    callingForm.TopMost = false;
                    callingForm.WindowState = FormWindowState.Normal;
                    callingForm.FormBorderStyle = FormBorderStyle.Sizable;
                }

                dbgWindow = new ScrollingTextWindow();
                dbgWindow.Location = new System.Drawing.Point(0, 0);
                dbgWindow.Show(callingForm);
            }
            else if (dbgWindow.Visible == false)
            {
                dbgWindow.Visible = true;
            }
            return true;
        }

        static public void Log(int level, string category, string message, bool AddCrLf = false)
        {
            if (AddCrLf) message = message + Environment.NewLine;
            if (Destination == LogDestination.Default)
            {
                System.Diagnostics.Debugger.Log(level, category, message);
            }
            else
            {
                if (dbgWindow == null)
                {
                    // get the size of the buffer
                    if (strBuffer.Length > (32000)) strBuffer = "";

                    strBuffer = strBuffer + message;
                }
                else
                {
                    if (tx == null)
                    {
                        tx = (TextBox)dbgWindow.Controls[0];
                    }

                    tx.Text = tx.Text + message;
                    if (tx.Text.Length > (32000)) tx.Text = "";

                    tx.Select(tx.Text.Length - 1, 0);
                }
            }
        }

        static public void Log(string message)
        {
            Log(0, null, message);
        }

        static public void LogIf(bool YesNo, string message)
        {
            if (YesNo)
            {
                Log(message);
            }
        }

        static public void LogIf(bool YesNo, int level, string category, string message)
        {
            if (YesNo)
            {
                Log(level, category, message);
            }
        }

        static public void LogLine(int level, string category, string message)
        {
            Log(level, category, message, true);
        }

        static public void LogLine(string message)
        {
            Log(0, null, message, true);
        }

        static public void LogLineIf(bool YesNo, string message)
        {
            if (YesNo)
            {
                LogLine(message);
            }
        }

        static public void LogLineIf(bool YesNo, int level, string category, string message)
        {
            if (YesNo)
            {
                LogLine(level, category, message);
            }
        }

        static public bool CannotLog()
        {
            if (System.Diagnostics.Debugger.IsAttached && System.Diagnostics.Debugger.IsLogging()) return false;
            return true;
        }

    }
}
