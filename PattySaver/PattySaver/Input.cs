using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Drawing.Text;
using System.Diagnostics;

using ScotSoft.PattySaver;
using JoeKCo.Utilities;
using JoeKCo.Utilities.Debug;

namespace ScotSoft.PattySaver
{
    public partial class ScreenSaverForm : Form
    {
        bool fInputDebugOutput = true;
        bool fInputDebugOutputAtTraceLevel = false;

        // do not change fInputDebugTrace here, it is calculated in each input method
        bool fInputDebugTrace = true;

        #region Context Menu Event Handlers

        private void contextMenuMain_Opening(object sender, CancelEventArgs e)
        {
            // Get the various modes we are in, and change them as necessary for the length of 
            // time the menu is opened.  Watch for timers going off.

            if (ourSlideshow.IsRunning)
            {
                fWasInSlideshowModeWhenMenuOpened = true;
                ourSlideshow.Exit();        // we'll restore it in contextMenuMain_Closed
            }

            // Get the various modes we are in, and enable/disable items as appropriate
            tsmPauseResume.Enabled = !fInETFMode;
            tsmNextPhoto.Enabled = !fInETFMode;
            tsmPreviousPhoto.Enabled = !fInETFMode;

            tsmExploreFolder.Enabled = !fInETFMode && !fShowingEmbeddedFileImage;
            tsmNextInFolder.Enabled = fInETFMode;
            tsmPreviousInFolder.Enabled = fInETFMode;
            tsmExitExploreFolder.Enabled = fInETFMode;

            tsmBlacklist.Enabled = !fShowingEmbeddedFileImage;

            // Tell the handler not to cancel the event
            e.Cancel = false;
        }

        private void contextMenuMain_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            // Get the various modes we are in, and change them as necessary for the length of 
            // time the menu is opened.

            if (fWasInSlideshowModeWhenMenuOpened)
            {
                fWasInSlideshowModeWhenMenuOpened = false;
                ourSlideshow.Start();
            }
        }

        private void tsmHelpAbout_Click(object sender, EventArgs e)
        {
            DoHelpAboutDialog();
        }

        private void tsmPauseResume_Click(object sender, EventArgs e)
        {
            ourSlideshow.Toggle();
        }

        private void tsmNextPhoto_Click(object sender, EventArgs e)
        {
            DoPreviousOrNext(false);
        }

        private void tsmPreviuosPhoto_Click(object sender, EventArgs e)
        {
            DoPreviousOrNext(true);
        }

        private void tsmExploreFolder_Click(object sender, EventArgs e)
        {
            EnterExploreFolderMode();
        }

        private void tsmNextInFolder_Click(object sender, EventArgs e)
        {
            // TODO: there's an issue with wrapping WasInSlideshowMode in both MenuOpen/Close event AND in the 
            // DoArrowKeyX methods. Only the outermost handler will actually see the correct state, the 
            // othe handlers will see the state after the outermost has handled it. 

            DoArrowKeyUp();
        }

        private void tsmPreviousInFolder_Click(object sender, EventArgs e)
        {
            DoArrowKeyDown();
        }

        private void tsmExitExploreFolder_Click(object sender, EventArgs e)
        {
            ExitExploreFolderMode(false);
        }

        private void tsmBlacklist_Click(object sender, EventArgs e)
        {
            DoBlacklistCurrentFile();
        }

        private void tsmSettings_Click(object sender, EventArgs e)
        {
            DoSettingsDialog();
        }

        private void tsmFullscreen_Click(object sender, EventArgs e)
        {
            ToggleScreenSaverWindowStyle();
        }

        private void tsmMetadataShowHide_Click(object sender, EventArgs e)
        {
            ToggleShowMetadata();
        }

        private void tsmMetaDataChangeFont_Click(object sender, EventArgs e)
        {
            DoFontDialog();
        }

        private void tsmMetaDataChangeColor_Click(object sender, EventArgs e)
        {
            DoColorDialog();
        }

        private void tsmMetadataMoveOnScreen_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To move the metadata display around the screen, hold down the Control Key and use the Arrow Keys on your keyboard.", ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsmOpenFileExplorer_Click(object sender, EventArgs e)
        {
            OpenFileExplorer(pbMain.ImageLocation);
        }

        private void tsmExit_Click(object sender, EventArgs e)
        {
            DoQuit();
        }

        #endregion Context Menu Event Handlers


        #region Keyboard Event Handlers

        /// <summary>
        /// Event which occurs immediately before the KeyDown event occurs. We may not need this at all.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenSaverForm_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            fInputDebugTrace = fInputDebugOutput && fInputDebugOutputAtTraceLevel;

            Logging.LogLineIf(fInputDebugTrace, "ScreenSaverForm_PreviewKeyDown(): entered.");

            // To test which keys cause which events, un-comment the next line
            //Logging.LogLineIf(fInputDebugTrace, "   PreviewKeyDown(): KeyCode = " + e.KeyCode + ", KeyValue = " + e.KeyValue + 
            //    ", KeyData = " + e.KeyData + ", Modifiers = " + e.Modifiers.ToString() + ", e.IsInputKey = " + e.IsInputKey);

            e.IsInputKey = true;

            // We currently do not monitor this event, as we will only need it if we add any controls to the form
            // which can take user keystroke input.  If that happens, we will need to move the arrow keys into this section.

            Logging.LogLineIf(fInputDebugTrace, "ScreenSaverForm_PreviewKeyDown(): exiting.");

        }

        /// <summary>
        /// Event which occurs when a key goes down. Most of our keyboard handling will occur here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenSaverForm_KeyDown(object sender, KeyEventArgs e)
        {
            fInputDebugTrace = fInputDebugOutput && fInputDebugOutputAtTraceLevel;

            Logging.LogLineIf(fInputDebugTrace, "ScreenSaverForm_KeyDown(): entered.");

            // To test which keys cause which events, un-comment the following lines
            //Logging.LogLineIf(fInputDebugTrace, "   KeyDown(): KeyCode = " + e.KeyCode + ", KeyValue = " + e.KeyValue +
            //    ", KeyData = " + e.KeyData + ", Modifiers = " + e.Modifiers.ToString() + ", Handled = " + e.Handled);

            switch (e.KeyCode)
            {

                // Special Keys

                case Keys.ControlKey:
                    // set a formwide variable, reset it on KeyUp event for ControlKey
                    fCtrlDown = true;
                    break;

                case Keys.ShiftKey:
                    // set a formwide variable, reset it on KeyUp event for ShiftKey
                    fShiftDown = true;
                    break;

                case Keys.Space:
                    ourSlideshow.Toggle();
                    break;

                case Keys.Escape:
                    DoQuit();
                    break;

                case Keys.Right:
                    DoArrowKeyRight();
                    break;

                case Keys.Left:
                    DoArrowKeyLeft();
                    break;

                case Keys.Up:
                    DoArrowKeyUp();
                    break;

                case Keys.Down:
                    DoArrowKeyDown();
                    break;

                case Keys.PageUp:
                    break;

                case Keys.PageDown:
                    break;

                case Keys.Home:
                    break;

                case Keys.End:
                    break;

                case Keys.Insert:
                    break;

                case Keys.Delete:
                    DoBlacklistCurrentFile();
                    break;

                case Keys.LWin:
                    break;

                case Keys.RWin:
                    break;

                case Keys.Back:
                    ToggleShowMetadata();
                    break;

                case Keys.Enter:
                    break;

                case Keys.F1:
                    DoHelpAboutDialog();
                    break;

                case Keys.F2:
                    DoSettingsDialog();
                    break;

                case Keys.F3:
                    DoFontDialog();
                    break;

                case Keys.F4:
                    // DoColorDialog();
                    break;

                case Keys.F9:
                    if (debugOutputWindow != null)
                    {
                        debugOutputWindow.ToggleDisplayVisibility();
                    }
                    else
                    {
                        debugOutputWindow = new ScrollingTextWindow(this);
                        debugOutputWindow.ShowDisplay();
                    }
                    break;

                case Keys.F11:
                    ToggleScreenSaverWindowStyle();
                    break;

                case Keys.D0: // zero
                    if (e.Control)
                    {
                        DoRestoreTextValuesToLoadedValues();
                    }
                    break;

                case Keys.M:
                    {
                        // MessingAround();
                    }
                    break;

                case Keys.N:
                    break;

                // "Mouse" Keys --- Yes, I am as shocked as you are

                case Keys.LButton:
                    break;

                case Keys.RButton:
                    break;

                case Keys.MButton:
                    break;


                // Alphabetic Keys

                case Keys.S:
                    if (e.Control)
                    {
                        metaFontData.Shadowing = !metaFontData.Shadowing;
                        pbMain.Invalidate();
                    }
                    break;

                case Keys.B:
                    // Break into the debugger
                    if (e.Control && e.Shift)
                    {
                        if (!System.Diagnostics.Debugger.IsAttached)
                        {
                            return;
                        }

                        if (fScreenSaverWindowStyle)
                        {
                            ExitScreenSaverWindowStyle();
                        }

                        //if (this.WindowState != FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
                        //if (this.FormBorderStyle != System.Windows.Forms.FormBorderStyle.Sizable) this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                        //if (this.TopMost != false) this.TopMost = false;

                        System.Diagnostics.Debugger.Break();
                    }
                    break;


                default:
                    break;
            }
            Logging.LogLineIf(fInputDebugTrace, "ScreenSaverForm_KeyDown(): exiting.");

        }

        /// <summary>
        /// Event which occurs when a keypress comes back up. We use this very little.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenSaverForm_KeyUp(object sender, KeyEventArgs e)
        {
            fInputDebugTrace = fInputDebugOutput && fInputDebugOutputAtTraceLevel;

            Logging.LogLineIf(fInputDebugTrace, "ScreenSaverForm_KeyUp(): entered.");

            //// To test which keys cause which events, un-comment the next line
            //Logging.LogLineIf(fInputDebugTrace, "   KeyUp(): KeyCode = " + e.KeyCode + ", KeyValue = " + e.KeyValue +
            //    ", KeyData = " + e.KeyData + ", Modifiers = " + e.Modifiers.ToString() + ", Handled = " + e.Handled + ", SupressKeyPress = "  + e.SuppressKeyPress);


            switch (e.KeyCode)
            {
                case Keys.ControlKey:
                    fCtrlDown = false;
                    break;

                case Keys.ShiftKey:
                    fShiftDown = false;
                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// Code that runs with every Key Press (different than KeyDown or KeyUp).
        /// </summary>
        private void ScreenSaverForm_KeyPress(object sender, KeyPressEventArgs e)
        {

            // Logging.LogLineIf("Form_KeyPress(): Key = " + e.KeyChar);
        }

        #endregion Keyboard Event Handlers


        #region Mouse Event Handlers

        /// <summary>
        /// Event sent when user scrolls mousewheel up or down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">e.Delta occurs for each detent.</param>
        void ScreenSaverForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (fCtrlDown)
            {
                if (fShiftDown)
                {
                    DoUpdateFontColor(e.Delta);
                }
                else
                {
                    DoUpdateFontSize(e.Delta);
                }
 
                pbMain.Invalidate();

            }
        }

        /// <summary>
        /// Because the picture box covers the whole form, the form doesn't receive "click" messages.  So we handle all click messages here, on the pictureBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbMain_Click(object sender, EventArgs e)
        {
            if (fCtrlDown)
            {
                if (fShiftDown)
                {
                    metaFontData.IncrementTextRenderingHint();
                }
                else
                {
                    metaFontData.IncrementContrastLevel();
                }

                pbMain.Invalidate();
            }
        }

        #endregion Mouse Event Handlers

    }
}
