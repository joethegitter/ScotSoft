Read Me

Last updated Aug 30th.

Here is how our overall architecture now works
----------------------------------------------

(This portion explains how we use two executables (one stub .scr, one .exe). To see explanation of the architecture of our main executable, jump down to Architecture of Our Main Executable.)

1. We have a very small stub screen saver (PattySvrX.scr). This goes in the System32 directory. 

2. We now have an executable (PattySaver.exe).  This also goes into the System32 directory. For desktop/app use, point a shortcut at PattSaver.exe in the System32 directory.

3. The stub is tiny, and only does the following:
   a. is launched by Windows, and receives the command line arguments that Windows uses.
   b. converts those parameters into BETTER parameters
   c. launches our exe with those new parameters. Those parameters are now:

   /scr /<mode> [-windowHandle] [/dgbwin]

   Where:
     /scr is required and tells us we were launched by the stub;
     <mode> is one of: /screensaver, /dt_configure, /cp_configure, /cp_minipreview;
     [-windowHandle] is only required for /cp_configure or /cp_minipreview
     [/dbgwin] tells the executable to open our debugOutputWindow on a timer after launch

   Making /dbgwin happen actually requires renaming PattSvrX.scr to PattySvrX.dgbwin.scr. I can explain how to use it later if you want to use it.

4. The executable can now handle the following parameters:
   
   When launched by the screen saver stub, we handle ONLY exactly what is described above.

   When launched by double clicking or from a shortcut, we handle:

   Modes:
   /c or /dt_configure              - open Settings Dlg on desktop
   /cp_configure -windowHandle      - open Settings Dlg owned by specified window handle
   /cp_minipreview -windowHandle    - open miniPreviewForm owned by specified window handle (for control panel only)
   /s or /screensaver               - open maximized/topmost/slideshow running

   If none of the above modes are specified, we open "windowed" - last remembered window size and position, slideshow off. So, a shortcut with no mode parameters would open this way.

   /startbuffer                     - tells us to put all debugOutput into a string buffer, so that when we open
                                      our debugOutput window, we can see all the debug output from the moment of launch.
                                      /starbuffer is assumed if we receive [/dbgwin].

5. Do NOT put /scr on a command line from a shortcut!  Things explode if you do that.

6. We now have a fully functioning debugOutputWindow that can be used from retail builds, debug builds not running in the debugger, etc.

You can open this window from the Screen Saver window by hitting F9. Hitting F9 again hides/shows it. When you close the debugOutputWindow, it is destroyed, and hitting F9 again recreates it.

If you want to see debug output when running the miniPreview form in the control panel, change the name of PattSvrX.scr to PattySvrX.dbgwin.scr. When the control panel opens, wait.  The debugOutputWindow will pop up after about 10 seconds.


Architecture of our Main Executable
-----------------------------------

1. The EntryPoint class (entirely in EntryPoint.cs) is the entry point for our executable. It parses the command line, sets up various states and modalities, and then opens one of our three main forms.

2. The ScreenSaverForm class is our main window. It is where we show the images that make up our viewer/slideshow. This class spans many .cs file. It is primarily in ScreenSaverForm.cs, but extends across Input.cs, Slideshow.cs, FontData.cs, and possibly others. Most of the other classes and files exist to support the functionality of the ScreenSaverForm class (for example, the FileInfoSource class).

3. The SettingsForm class is the Settings Dialog.

4. The miniControlPanelForm class is the tiny little window we draw in when showing our screen saver in the ControlPanel.

5. There are additional forms: HelpAboutForm, ScrollingTextWindow form. HelpAboutForm is our HelpAbout dialog.  ScrollingTextWindow form is our debugOutputWindow.



