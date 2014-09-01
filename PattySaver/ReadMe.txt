Read Me

Last updated Aug 30th.

Here is how our overall architecture now works
----------------------------------------------

(This portion explains how we use two executables (one stub .scr, one .exe). 
To see explanation of the architecture of our main executable, jump down to 
Architecture of Our Main Executable.)

1. We have a very small stub screen saver (PattySaver.scr). This goes in the 
   System32 directory. This stub is built from the PattSvrX project, and
   the file is renamed in a postbuild event.

2. We now have an executable (PattySaver.exe). This also goes into the System32 
   directory. For desktop/app use, point a shortcut at PattSaver.exe in the 
   System32 directory. This exe is built from the PattySaver project.

3. The stub is tiny, and only does the following:
   a. is launched by Windows, and receives command line arguments Windows uses
   b. converts those parameters into BETTER parameters
   c. launches our exe with those new parameters. Those parameters are now:

   /scr /<mode> [-windowHandle] [[/popdgbwin] | [/startbuffer]]

   Where:
     /scr is required and tells the exe it was launched by the stub;
     <mode> is one of: 
        /screensaver    - open screensaver maximized/topmost/slideshow on
        /dt_configure   - open Settings dlg to the desktop
        /cp_configure   - open Settings dlg with specified control panel hWnd
        /cp_minipreview - open minipreview with specified control panel hWnd

     [-windowHandle] is only required for /cp_configure or /cp_minipreview

     [/popdbgwin] is optional and tells the executable to open our 
               debugOutputWindow on a timer after launch. 

     [/startbuffer] is optional and tells the executable to begin storing 
                    debug output in a string buffer immediately after
                    launch, so that it is available for viewing in the
                    debugOutputWindow later.

Notes: /popdbgwin and /startbuffer will never appear in the same command
line.  If /popdbgwin is passed, /startbuffer is assumed, but not vice versa.

/popdbgwin will ONLY appear in the command line output if 
(a) user held down shift key when .scr was launched or
(b) user renamed .scr file to PattySaver.dbgwin.scr.

/startbuffer will ONLY appear in the command line if 
(a) user held down CONTROL key when .scr was launched or
(b) user renamed .scr file to PattySaver.startbuffer.scr.

This file rename does not interfere with any functionality.

For debug purposes, if you hold down ALT when the .scr launches,
the .scr will put up a message box showing the command line it 
received, and the command line it is passing to the exe.

4. The executable can now handle the following parameters:
   
   When launched by the screen saver stub, we handle ONLY exactly what is 
   described above.

   When launched by double clicking or from a shortcut, we handle:

   Modes:
   /c or /dt_configure              - same as above
   /cp_configure -windowHandle      - same as above, will use windowHandle
   /cp_minipreview -windowHandle    - same as above, will use windowHandle
   /s or /screensaver               - open maximized/topmost/slideshow running

   If none of the above modes are specified, our executable opens "windowed" - 
   to the last remembered window size and position, slideshow off. So, a 
   shortcut with no mode parameters would open windowed.

   Unofficial args:
   /popdbgwin     - tells us to pop up the debugOutputWindow on a timer
                    after the exe is launched. This only works for the 
                    miniControlPanelForm, which has no way for the user to
                    summon the window interactively.

   /startbuffer     - tells us to put all debugOutput into a string buffer, 
                    so that when we open our debugOutput window, we can see 
                    all the debug output back to the moment of launch. If 
                    /dbgwin is received, /startbuffer is forced to true.

5. We now have a fully functioning debugOutputWindow that can be used from 
   retail builds, debug builds not running in the debugger, etc.

   You can open this window from the Screen Saver window by hitting F9. 
   Hitting F9 again hides/shows it. When you close the debugOutputWindow, 
   it is destroyed, and hitting F9 again recreates it.



Architecture of our Main Executable
-----------------------------------

1. The EntryPoint class (entirely in EntryPoint.cs) is the entry point for 
   our executable. It parses the command line, sets up various states and 
   modalities, and then opens one of our three main forms.

2. The ScreenSaverForm class is our main window. It is where we show the 
   images that make up our viewer/slideshow. This class spans multiple .cs 
   files. It is primarily in ScreenSaverForm.cs, but extends across Input.cs,
   Slideshow.cs, FontData.cs, and possibly others. Most of the other classes 
   and files exist to support the functionality of the ScreenSaverForm class 
   (for example, the FileInfoSource class).

3. The SettingsForm class is the Settings Dialog.

4. The miniControlPanelForm class is the tiny little window we draw in when 
   showing our screen saver in the ControlPanel.

5. There are additional forms: HelpAboutForm, ScrollingTextWindow form. 
   HelpAboutForm is our HelpAbout dialog. ScrollingTextWindow form is our 
   debugOutputWindow.



