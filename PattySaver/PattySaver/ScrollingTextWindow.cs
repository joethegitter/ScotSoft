using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver
{
    /// <summary>
    /// Defines behavior of a hideable/showable display.
    /// </summary>
    public interface IDisplayable
    {
        void ShowDisplay();

        void HideDisplay();

        void ToggleDisplayVisibility();

        bool DisplayIsVisible { get; }
    }

    /// <summary>
    /// Defines behavior of a text container to which text can be added.
    /// </summary>
    public interface IAppendableText
    {
        void AppendText(string SomeText);

        void ClearText();

        void CopyTextToClipboard();

        int TextLength { get; }
    }

    public partial class ScrollingTextWindow : Form, IDebugOutputConsumer, IAppendableText, IDisplayable
    {

        #region Interface Implementations

        public void ShowDisplay()
        {
            _IsBoxVisible = true;
            if (MyOwner != null)
            {
                this.Show(MyOwner);
            }
            else
            {
                this.Show();
            }
        }

        public void HideDisplay()
        {
            _IsBoxVisible = false;
            this.Hide();
        }

        public bool DisplayIsVisible
        {
            get
            {
                return _IsBoxVisible;
            }
        }

        public void ToggleDisplayVisibility()
        {
            if (DisplayIsVisible)
            {
                HideDisplay();
            }
            else
            {
                ShowDisplay();
            }
        }

        public void AppendText(string SomeText)
        {
            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "AppendText(): Entered.");

            if (SomeText == null)
            {
                System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "   AppendText(): SomeText == null, returning immediately.");
                System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "AppendText(): exiting.");
                return;
            }

            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "   AppendText(): SomeText.Length = " + SomeText.Length);

            // test to see if incoming text plus existing text is too long for our comfort
            int MaxLengthAllowed = Int32.MaxValue / 3;

            int existingLength = TextLength;
            long total = TextLength + SomeText.Length;
            if (total > MaxLengthAllowed)
            {
                // clear the text box, then compact memory
                ClearText();
                GC.Collect();

                // Notify
                string notification = "Text was cleared to avoid overrun. Existing/Incoming/Total Lengths: " + existingLength.ToString() +
                    " + " + SomeText.Length.ToString() + " = " + total.ToString() + " > " + MaxLengthAllowed.ToString();

                System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "   AppendText(): " + notification);

                // insert notification into text box
                AppendText("<< " + notification + " >>");
            }

            theTextBox.AppendText(SomeText);

            if (theTextBox.Text.Length > 0)
            {
                theTextBox.Select(theTextBox.Text.Length - 1, 0);
            }

            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "AppendText(): Exiting.");
        }

        public void ClearText()
        {
            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "Clear(): Entered.");

            theTextBox.Clear();

            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "Clear(): Exiting.");

        }

        public int TextLength
        {
            get
            {
                return theTextBox.TextLength;
            }
        }

        public void ConsumeDebugOutputBuffer(string SomeText)
        {
            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "ConsumeBuffer(): Entered.");

            if (SomeText == null)
            {
                System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "   ConsumeBuffer(): SomeText == null, returning immediately.");
                return;
            }
            else
            {
                AppendText(SomeText);
            }
            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "ConsumeBuffer(): Exiting.");
        }

        public void ConsumeDebugOutput(string SomeText)
        {
            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "ConsumeDebugOutput(): Entered.");

            if (SomeText == null)
            {
                System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "   ConsumeDebugOutput(): SomeText == null, returning immediately.");
                return;
            }
            else
            {
                AppendText(SomeText);
            }

            System.Diagnostics.Debug.WriteLineIf(fDebugTrace, "ConsumeDebugOutput(): Exiting.");
        }

        #endregion Interface Implementations


        #region Fields

        // Debug Output
        bool fDebugTrace = false;   // do not modify here, this is recalculated at Constructor

        bool fDebugOutput = true;
        bool fOutputAtTraceLevel = true;
        
        // Public Fields
        public ScrollingTextWindow Myself;     // Public reference to "this"
        public Form MyOwner;                    // Public owner to this.Owner

        // Internal states
        internal bool fConstructorIsRunning = false;
        internal bool fConstructorHasCompleted = false;
        internal bool fFormLoadIsRunning = false;
        internal bool fFormLoadHasCompleted = false;

        // Implementation support
        bool _IsBoxVisible = false;

        #endregion Fields


        #region Constructors

        /// <summary>
        /// Creates the object. If toBeOwner is not null, adds this object to that form's ownedForms list.
        /// </summary>
        /// <param name="toBeOwner">Optional: form which should be made the owner of this object.</param>
        public ScrollingTextWindow(Form toBeOwner = null)
        {
            fConstructorIsRunning = true;           // set flags

            // set debugging levels
            fDebugTrace = fDebugOutput && fOutputAtTraceLevel;

            // boilerplate; ignore, but do not remove, or change order
            InitializeComponent();          

            // set the values we'll need
            Myself = this;
            MyOwner = toBeOwner;
            if (toBeOwner != null)
            {
                this.Owner = toBeOwner;
            }

            fConstructorIsRunning = false;
            fConstructorHasCompleted = true;
        }

        #endregion Constructors


        #region Public Members

        /// <summary>
        /// Override of ShowWithoutActivation property.  In theory, this tells
        /// Windows that when our window gets shown, it should not receive activation.
        /// </summary>
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        public bool CopyTextToClipboardOnClose { get; set; }

        public void CopyTextToClipboard()
        {
            if (theTextBox.Text.Length > 0)
            {
                Clipboard.SetText(theTextBox.Text);
            }
        }

        #endregion Public Members


        #region Form Events

        private void ScrollingTextWindow_Load(object sender, EventArgs e)
        {
            // Register to get debug output sent to us
            Logging.AddConsumer((IDebugOutputConsumer)this);
        }

        private void ScrollingTextWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CopyTextToClipboardOnClose)
            {
                CopyTextToClipboard();
            }

            Logging.RemoveConsumer((IDebugOutputConsumer)this);
        }

        private void ScrollingTextWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            // TODO: hack. Replace with an event fired when the form is closed,
            // and have the creating form get the event.
            if (this.Owner != null)
            {
#if MIPROTOTYPE
                JKSoft.CP_Preview x = this.Owner as JKSoft.CP_Preview;
                if (x != null)
                {
                    x.debugOutputWindow = null;
                }
#elif SCOTSOFTSS
                ScreenSaverForm x = this.Owner as ScreenSaverForm;
                if (x != null)
                {
                    x.debugOutputWindow = null;
                }

#elif SIPROTOTYPE
                SingleInstanceScreenSaver.CP_PreviewForm x = this.Owner as SingleInstanceScreenSaver.CP_PreviewForm;
                if (x != null)
                {
                    x.debugOutputWindow = null;
                }

#endif
            }
        }


        #endregion Form Events


    }
}
