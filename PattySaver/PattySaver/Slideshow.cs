using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

using JoeKCo.Utilities.Debug;


namespace ScotSoft.PattySaver
{
    public partial class ScreenSaverForm : Form
    {

        /// <summary>
        /// A class which provides an event and timer based Slideshow metronome.
        /// </summary>
        public class Slideshow
        {

            // TODO: the Enter method should take a delegate and the Tick event handler should invoke that delegate.
            // that way, the Tick method wouldn't have to know about the DoPreviousOrNext() method, and the constructor
            // wouldn't have to take a "form that created me" parameter.

            // TODO: const SlideShowDeferralInterval should be a user settable property
            // TODO: all intervals should be user settable properties, don't let class read from config file


            #region Data

            private bool fDebugOutput = true; // controls whether this object emits debug output
            private bool fDebugOutputTraceLevel = false;

            int _interval = 7000;   // must be greater than SlideShowDeferralBreakWindow
            int _deferralInterval = 7000;   // must be greater than SlideShowDeferralBreakWindow
            int _deferralTimeWindow = 2000;   // must be greater than SlideShowDeferralBreakWindow

            TimeSpan SlideShowDeferralBreakWindow;

            private ScreenSaverForm _formThatCreatedMe;
            private System.Windows.Forms.Timer _slideshowTimer;
            private System.Windows.Forms.Timer _slideshowDeferralTimer;
            private bool _fSlideshowIsRunning = false;
            private bool _fSlideshowIsDeferred = false;
            private object lockSlideshowTimer = new object();
            private object lockSlideshowDeferralTimer = new object();
            private DateTime _lastTimeDeferWasCalled;

            #endregion Data


            #region Public Members

            /// <summary>
            /// Creates the object.
            /// </summary>
            /// <param name="FormThatCreatedMe">The FullScreenForm instance who's DoPreviousOrNext() method will be called when at each Slideshow.Interval.</param>
            public Slideshow(ScreenSaverForm FormThatCreatedMe)
            {
                // store away a reference to the instance of FullScreenForm that created this object
                _formThatCreatedMe = FormThatCreatedMe;

                // set the deferral window
                SlideShowDeferralBreakWindow = new TimeSpan(0, 0, 0, _deferralTimeWindow / 1000);
            }


            /// <summary>
            /// Interval, in milliseconds, at which DoPreviousOrNext() will be called.
            /// </summary>
            public int IntervalInMilliSeconds
            {
                get
                {
                    return _interval;
                }

                set
                {
                    bool wasRunning = IsRunning;
                    if (wasRunning) Exit();
                    _interval = Math.Abs(value);
                    if (wasRunning) Start();
                }
            }

            /// <summary>
            /// Interval, in milliseconds, that calling DoPreviousOrNext() will be delayed when user presses Left Arrow once during Slideshow.
            /// </summary>
            public int DeferralIntervalInMilliseconds
            {
                get
                {
                    return _deferralInterval;
                }

                set
                {
                    bool wasRunning = IsRunning;
                    if (wasRunning) Exit();
                    _deferralInterval = Math.Abs(value);
                    if (wasRunning) Start();
                }
            }

            /// <summary>
            /// How many milliseconds after the first deferral that a second deferral will be accepted. Used to distinguish between "Extend Deferral" and "Do That Twice".
            /// </summary>
            public int DeferralTimeWindowInMilliseconds
            {
                get
                {
                    return _deferralTimeWindow;
                }

                set
                {
                    bool wasRunning = IsRunning;
                    if (wasRunning) Exit();
                    _deferralTimeWindow = Math.Abs(value);
                    if (wasRunning) Start();
                }
            }

            /// <summary>
            /// Indicates if the Slideshow is currently in process.
            /// </summary>
            public bool IsRunning
            {
                get
                {
                    if (_fSlideshowIsRunning || _fSlideshowIsDeferred)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }


            /// <summary>
            /// Starts the Slideshow.
            /// </summary>
            public void Start()
            {
                bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

                Logging.LogLineIf(fDebugTrace, "Slideshow.Start(): entered.");

                lock (lockSlideshowTimer)
                {
                    _fSlideshowIsRunning = true;
                    _slideshowTimer = new Timer();                                               // create a new timer
                    _slideshowTimer.Tick += new EventHandler(SlideshowTimerTick);                // bind the timer.tick event to a handler method
                    _slideshowTimer.Interval = _interval;                                        // set interval from settings
                    _slideshowTimer.Start();
                }
                Logging.LogLineIf(fDebugTrace, "Slideshow.Start(): exiting.");
            }

            /// <summary>
            /// Delays the Slideshow by the DeferralIntervalInMilliseconds number of milliseconds.
            /// </summary>
            /// <returns>False if the deferral request occured within the DeferralWindowInMilliseconds timeframe, so that the user may be allowed to go backwards.</returns>
            public bool Defer()
            {
                bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

                Logging.LogLineIf(fDebugTrace, "Slideshow.Defer(): entered (outside locks).");

                lock (lockSlideshowTimer)
                {
                    lock (lockSlideshowDeferralTimer)
                    {
                        Logging.LogLineIf(fDebugTrace, "   Slideshow.Defer(): inside locks.");

                        if (!_fSlideshowIsRunning)
                        {
                            Logging.LogLineIf(fDebugOutput, "   Slideshow.Defer(): called when !_fSlideshowIsRunning. Doing nothing.");
                            Logging.LogLineIf(fDebugTrace, "Slideshow.Defer(): exiting.");
                            return true;
                        }
                        else
                        {
                            // if we're already in deferral, extend it
                            if (_fSlideshowIsDeferred)
                            {
                                DateTime now = DateTime.Now;
                                TimeSpan delta = now - _lastTimeDeferWasCalled;
                                if (delta < SlideShowDeferralBreakWindow)
                                {
                                    Logging.LogLineIf(fDebugOutput, "   Slideshow.Defer(): not extending defer, timespan was too small: " + delta);
                                    Logging.LogLineIf(fDebugTrace, "Slideshow.Defer(): exiting.");
                                    return false;
                                }
                                else
                                {
                                    ExtendDeferral();
                                    Logging.LogLineIf(fDebugOutput, "   Slideshow.Defer(): extending deferral.");
                                    Logging.LogLineIf(fDebugTrace, "   Slideshow.Defer(): updating _lastTimeDeferWasCalled.");
                                    _lastTimeDeferWasCalled = now;
                                    Logging.LogLineIf(fDebugTrace, "Slideshow.Defer(): exiting.");
                                    return true;
                                }
                            }

                            // note: do NOT set _fSlideShowIsRunning to false here. It's still running, it's just deferred.
                            Logging.LogLineIf(fDebugTrace, "   Slideshow.Defer(): starting deferral.");
                            _slideshowTimer.Stop();
                            _slideshowDeferralTimer = new Timer();
                            _slideshowDeferralTimer.Tick += new EventHandler(SlideshowDeferralTick);
                            _slideshowDeferralTimer.Interval = _deferralInterval;
                            _fSlideshowIsDeferred = true;
                            Logging.LogLineIf(fDebugTrace, "   Slideshow.Defer(): updating _lastTimeDeferWasCalled.");
                            _lastTimeDeferWasCalled = DateTime.Now;
                            _slideshowDeferralTimer.Start();
                            Logging.LogLineIf(fDebugTrace, "Slideshow.Defer(): exiting.");
                            return true;
                        }
                    }
                }
            }

            /// <summary>
            /// Extends an existing Deferral (actually, resets it).
            /// </summary>
            public void ExtendDeferral()
            {
                // When we're already counting down to resume SlideshowMode, and the user hits
                // GetNextImage or GetPreviousImage manually again, we want to extend the deferment time
                lock (lockSlideshowDeferralTimer)
                {
                    if (_slideshowDeferralTimer != null)
                    {
                        _slideshowDeferralTimer.Stop();
                        _slideshowDeferralTimer.Start();     // this starts it over, with original timer interval
                    }
                }
            }

            /// <summary>
            /// Stops the Slideshow if it is running, and starts it if it is not.
            /// </summary>
            public void Toggle()
            {
                if (IsRunning)
                {
                    Exit();
                }
                else
                {
                    Start();
                }
            }

            /// <summary>
            /// Stops the Slideshow.
            /// </summary>
            public void Exit()
            {
                bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

                Logging.LogLineIf(fDebugTrace, "Slideshow.Exit(): entered (outside locks).");

                lock (lockSlideshowTimer)
                {
                    lock (lockSlideshowDeferralTimer)
                    {
                        Logging.LogLineIf(fDebugTrace, "   Slideshow.Exit(): inside locks.");

                        if (_fSlideshowIsDeferred)
                        {
                            if (_slideshowDeferralTimer != null)
                            {
                                _slideshowDeferralTimer.Stop();
                                _slideshowDeferralTimer.Tick -= new EventHandler(SlideshowDeferralTick);
                                _slideshowDeferralTimer.Dispose();
                                _slideshowDeferralTimer = null;
                                _fSlideshowIsDeferred = false;
                            }
                        }

                        if (_fSlideshowIsRunning)
                        {
                            if (_slideshowTimer != null)
                            {
                                _slideshowTimer.Stop();
                                _slideshowTimer.Tick -= new EventHandler(SlideshowTimerTick);     // unbind the timer.tick event from the method
                                _slideshowTimer.Dispose();                                       // dispose of the timer object
                                _slideshowTimer = null;
                                _fSlideshowIsRunning = false;
                            }
                        }
                    }
                }
                Logging.LogLineIf(fDebugTrace, "Slideshow.Exit(): exiting.");
            }

            #endregion Public Members


            #region Private Members

            private void SlideshowDeferralTick(object obj, EventArgs e)
            {
                bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

                Logging.LogLineIf(fDebugTrace, "Slideshow.SlideshowDeferralTick(): entering (outside locks).");

                // when the timer goes off, we kill the defer timer
                lock (lockSlideshowTimer)
                {
                    lock (lockSlideshowDeferralTimer)
                    {
                        //Logging.LogLineIf(fDbgOutput, "   Slideshow.SlideshowDeferralTick(): inside locks.");

                        if (_slideshowDeferralTimer != null)
                        {
                            _slideshowDeferralTimer.Stop();
                            _slideshowDeferralTimer.Tick -= new EventHandler(SlideshowDeferralTick);     // unbind the timer.tick event from the method
                            _slideshowDeferralTimer.Dispose();                                       // dispose of the timer object
                            _slideshowDeferralTimer = null;
                            _fSlideshowIsDeferred = false;
                        }
                        else
                        {
                            Logging.LogLineIf(fDebugOutput, "   Slideshow.SlideshowDeferralTick(): called when _slideshowTimer == null.");
                        }
                    }

                    // check to see if slideshow is still supposed to be running
                    if (_fSlideshowIsRunning)
                    {
                        if (_slideshowTimer != null)
                        {
                            _slideshowTimer.Start();
                        }
                        else
                        {
                            Logging.LogLineIf(fDebugOutput, "   Slideshow.SlideshowDeferralTick(): _slideshowTimer == null but _fSlideshowIsRunning is true!");
                        }
                    }
                    else
                    {
                        Logging.LogLineIf(fDebugOutput, "   Slideshow.SlideshowDeferralTick(): called when _fSlideshowIsRunning = false.");
                    }
                }
                Logging.LogLineIf(fDebugTrace, "Slideshow.SlideshowDeferralTick(): exiting.");
            }

            private void SlideshowTimerTick(object obj, EventArgs e)
            {
                bool fDebugTrace = fDebugOutput && fDebugOutputTraceLevel;

                Logging.LogLineIf(fDebugTrace, "Slideshow.SlideshowTimerTick(): entering (outside locks).");

                lock (lockSlideshowTimer)
                {
                    lock (lockSlideshowDeferralTimer)
                    {
                        Logging.LogLineIf(fDebugTrace, "   Slideshow.SlideshowTimerTick(): inside locks.");

                        if (_fSlideshowIsRunning && !_fSlideshowIsDeferred)   // these could have changed between Start and Exit
                        {
                            // on each Tick, we draw the next image
                            _formThatCreatedMe.DoPreviousOrNext(false);
                        }
                        else
                        {
                            Logging.LogLineIf(fDebugOutput, "   Slideshow.SlideshowTimerTick(): tick went off but (_fSlideshowIsRunning && !_fSlideshowIsDeferred) failed.");
                        }
                    }
                }
                Logging.LogLineIf(fDebugTrace, "Slideshow.SlideshowTimerTick(): exiting.");
            }

            #endregion Private Members

        }
    }
}
