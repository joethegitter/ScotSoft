using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Diagnostics;

using ScotSoft;
using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;
using ScotSoft.PattySaver.LaunchManager;

namespace ScotSoft.PattySaver
{
    public partial class ScreenSaverForm : Form
    {
        public class FontData
        {
            bool fDebugOutput = true;
            bool fDebugAtTraceLevel = false;
            bool fDebugTrace = false;   // do not modify here, it will be recalculated in constructor

            const float RealMaxFontSize = 124F;
            const float RealMinFontSize = 6F;
            const int RealMaxContrastLevel = 12;
            const int RealMinContrastLevel = 0;

            private string _fontName = "Segoe UI";
            private float _fontSize = 9;
            private FontStyle _fontStyle = FontStyle.Regular;
            private string _fontColorName = "Aqua";
            private TextRenderingHint _textRenderingHint = TextRenderingHint.AntiAliasGridFit;
            private int _contrastLevel = 0;
            private float _maxFontSize = 124F;
            private float _minFontSize = 6F;
            private bool _shadowing = true;
            private bool _allowNonAntiAliased = false;

            private List<String> _colorNameList = new List<String>();
            private int _colorNameListIndex = 0;
            private List<TextRenderingHint> _textRenderingHintList = new List<TextRenderingHint>();
            private int _textRenderingHintsIndex = 0;

            public FontData()
            {
                fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

                // Initialize the indexables
                InitializeIndexableValues();

                // Set the intial value of _textRenderingHint
                if (FontSmoothingIsEnabled())
                {
                    // If we can, set Cleartype
                    if (ClearTypeIsEnabled())
                    {
                        _textRenderingHintList.Add(System.Drawing.Text.TextRenderingHint.ClearTypeGridFit);
                        _textRenderingHintsIndex = 2;
                        _textRenderingHint = _textRenderingHintList[_textRenderingHintsIndex];
                    }
                    else
                    {
                        // If we cannot have Cleartype, try AntiAliasGridfit
                        _textRenderingHintsIndex = 1;
                        _textRenderingHint = _textRenderingHintList[_textRenderingHintsIndex];
                    }
                }
                else
                {
                    // TODO: hack.  Do the work to handle a variable length list, based on whether font smoothing is enabled and _allowNonAliased
                    throw new InvalidOperationException("Windows says that FontSmoothing has been disabled, and this program needs it to be enabled.");
                    //_textRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    // _allowNonAntiAliased = true;
                }
            }

            public string FontName
            {
                get
                {
                    return _fontName;
                }
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("FontName", "FontName cannot be null.");
                    }
                    _fontName = value;
                }
            }

            public float FontSize
            {
                get
                {
                    return _fontSize;
                }

                set
                {
                    if (value < MinFontSize || value > MaxFontSize)
                    {
                        throw new ArgumentOutOfRangeException("FontSize", "FontSize be larger than MaxFontSize or smaller than MinFontSize.");
                    }
                    _fontSize = value;
                }
            }

            public FontStyle FontStyle
            {
                get
                {
                    return _fontStyle;
                }

                set
                {
                    _fontStyle = value;
                }
            }

            public String FontColorName
            {
                get
                {
                    return _fontColorName;
                }

                set
                {
                    _fontColorName = value;
                }
            }

            public bool AllowNonAntiAliasedFonts
            {
                get
                {
                    return _allowNonAntiAliased;
                }

                private set
                {
                    if (value == false)
                    {
                        if (!FontSmoothingIsEnabled())
                        {
                            // do nothing, we can't allow this. Caller needs to test value after setting it.
                        }
                        else
                        {
                            _allowNonAntiAliased = false;
                        }
                    }
                    else
                    {
                        _allowNonAntiAliased = value;
                    }
                }
            }

            public TextRenderingHint TextRenderingHint
            {
                get
                {
                    return _textRenderingHint;
                }
            }

            public bool SetTextRenderingHint(TextRenderingHint textRenderingHint)
            {
                if (textRenderingHint == System.Drawing.Text.TextRenderingHint.AntiAlias ||
                    textRenderingHint == System.Drawing.Text.TextRenderingHint.AntiAliasGridFit ||
                    textRenderingHint == System.Drawing.Text.TextRenderingHint.ClearTypeGridFit)
                {
                    if (!_allowNonAntiAliased)
                    {
                        return false;
                    }

                    if (!FontSmoothingIsEnabled())
                    {
                        return false;
                    }

                    if (textRenderingHint == System.Drawing.Text.TextRenderingHint.ClearTypeGridFit)
                    {
                        if (!ClearTypeIsEnabled())
                        {
                            return false;
                        }
                    }

                    // no problem accepting anti-aliased font, let's do it
                    _textRenderingHint = textRenderingHint;
                    return true;
                }
                else    // non-antialiasing setting
                {
                    _textRenderingHint = textRenderingHint;
                    return true;
                }
            }

            public int ContrastLevel
            {
                get
                {
                    return _contrastLevel;
                }

                set
                {
                    if (value < RealMinContrastLevel || value > RealMaxContrastLevel)
                    {
                        throw new ArgumentOutOfRangeException("ContrastLevel", "Cannot be less than zero or greater than 12.");
                    }

                    _contrastLevel = value;
                }
            }

            public bool Shadowing
            {
                get
                {
                    return _shadowing;
                }
                set
                {
                    _shadowing = value;
                }
            }

            public float MaxFontSize
            {
                get
                {
                    return _maxFontSize;
                }

                set
                {
                    if (value < RealMinFontSize || value > RealMaxFontSize)
                    {
                        throw new ArgumentOutOfRangeException("MaxFontSize", "Cannot be less than " + RealMinFontSize + " or greater than " + RealMaxFontSize + ".");
                    }

                    if (value < _minFontSize)
                    {
                        throw new ArgumentOutOfRangeException("MaxFontSize", "Cannot be less than MinFontSize.");
                    }

                    _maxFontSize = value;
                }
            }

            public float MinFontSize
            {
                get
                {
                    return _minFontSize;
                }

                set
                {
                    if (value < RealMinFontSize || value > RealMaxFontSize)
                    {
                        throw new ArgumentOutOfRangeException("MaxFontSize", "Cannot be less than " + RealMinFontSize + " or greater than " + RealMaxFontSize + ".");
                    }

                    if (value > _maxFontSize)
                    {
                        throw new ArgumentOutOfRangeException("MaxFontSize", "Cannot be less than MaxFontSize.");
                    }

                    _minFontSize = value;
                }
            }

            public bool SetPropertiesFromFontDlg(FontDialog AFontDialog)
            {
                if (AFontDialog.Font.Size > MaxFontSize || AFontDialog.Font.Size < MinFontSize)
                {
                    return false;
                }

                _fontName = AFontDialog.Font.Name;
                _fontSize = AFontDialog.Font.Size;
                _fontStyle = AFontDialog.Font.Style;
                _fontColorName = AFontDialog.Color.Name;
                return true;
            }

            public void SetColorFromColorDlg(ColorDialog AColorDialog)
            {
                _fontColorName = AColorDialog.Color.Name;
            }

            public void IncrementOrDecrementFontColorName(bool decrement, bool AllowCycling = true)
            {
                int newIndex = _colorNameListIndex;

                if (decrement)  // Up or down?
                {
                    newIndex = _colorNameListIndex - 1;
                }
                else
                {
                    newIndex = _colorNameListIndex + 1;
                }

                if (newIndex < 0)
                {
                    if (AllowCycling)
                    {
                        // go back to the top
                        _colorNameListIndex = _colorNameList.Count - 1;
                    }
                    else
                    {
                        // stop at zero
                        _colorNameListIndex = 0;
                    }
                }
                else if (newIndex > _colorNameList.Count - 1)
                {
                    if (AllowCycling)
                    {
                        // go back to the bottom
                        _colorNameListIndex = 0;
                    }
                    else
                    {
                        // stop at top
                        _colorNameListIndex = _colorNameList.Count - 1;
                    }
                }
                else
                {
                    _colorNameListIndex = newIndex;
                }

                Logging.LogLineIf(fDebugTrace, "Metadata now being drawn in color: " + _colorNameList[_colorNameListIndex].ToString());
                this._fontColorName = _colorNameList[_colorNameListIndex];
                // pbMainPhoto.Invalidate();
            }

            public void IncrementOrDecrementFontSize(bool decrement)
            {
                float newFontSize = _fontSize;

                if (decrement)  // Up or down?
                {
                    newFontSize = (float)((int)(newFontSize - 1));
                }
                else
                {
                    newFontSize = (float)((int)(newFontSize + 1));
                }

                if (newFontSize < _minFontSize)
                {
                    _fontSize = _minFontSize;
                }
                else if (newFontSize > _maxFontSize)
                {
                    _fontSize = _maxFontSize;
                }
                else
                {
                    _fontSize = newFontSize;
                }
                Logging.LogLineIf(fDebugTrace, "Metadata now being drawn at size: " + _fontSize.ToString());
            }

            public void IncrementTextRenderingHint()
            {
                //dataFont.SetTextRenderingHint
                if (_textRenderingHintsIndex == _textRenderingHintList.Count - 1)
                {
                    _textRenderingHintsIndex = 0;
                }
                else
                {
                    _textRenderingHintsIndex++;
                }
                Logging.LogLineIf(fDebugTrace, "Metadata now being drawn with Hinting value: " + _textRenderingHintList[_textRenderingHintsIndex].ToString());
            }

            public void IncrementContrastLevel()
            {
                if (_contrastLevel == 0)
                {
                    _contrastLevel = 3;
                }
                else if (_contrastLevel == 3)
                {
                    _contrastLevel = 6;
                }
                else if (_contrastLevel == 6)
                {
                    _contrastLevel = 9;
                }
                else if (_contrastLevel == 9)
                {
                    _contrastLevel = 12;
                }
                else if (_contrastLevel == 12)
                {
                    _contrastLevel = 0;
                }
                Logging.LogLineIf(fDebugTrace, "Metadata now being drawn in with a Contrast of : " + _contrastLevel.ToString());
            }

            private bool FontSmoothingIsEnabled()
            {
                try
                {
                    return SystemInformation.IsFontSmoothingEnabled;
                }
                catch
                {
                    return false;
                }
            }

            private bool ClearTypeIsEnabled()
            {
                try
                {
                    return SystemInformation.FontSmoothingType == 2;
                }
                catch
                {
                    return false;
                }
            }

            private bool InitializeIndexableValues()
            {
                try
                {
                    // fill the list of colors with all known, non-system colors
                    _colorNameList.Clear();
                    foreach (KnownColor kc in (KnownColor[])Enum.GetValues(typeof(KnownColor)))
                    {
                        if (!Color.FromKnownColor(kc).IsSystemColor)
                        {
                            _colorNameList.Add(Color.FromKnownColor(kc).Name);
                        }
                    }

                    //Logging.LogLineIf("Count of colors in list: " + colors.Count + ", and here they are:");

                    //for (int i = 0; i < colors.Count; i++)
                    //{
                    //    Logging.LogLineIf(colors[i].ToString() + "  " + i);
                    //}

                    _textRenderingHintList.Clear();
                    _textRenderingHintList.Add(TextRenderingHint.AntiAlias);
                    _textRenderingHintList.Add(TextRenderingHint.AntiAliasGridFit);

                    return true;
                }
                catch (Exception ex)
                {
                    Logging.LogLineIf(fDebugOutput, "InitializeIndexableValues(): Exception thrown, returning false. Exception: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
