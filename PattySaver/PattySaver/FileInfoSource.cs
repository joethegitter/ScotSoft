using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Diagnostics;

namespace ScotSoft.PattySaver
{

    // ----------------------   MainFileInfoSource   ---------------------- //

    public class MainFileInfoSource
    {
        #region Fields

        // Fields that appear in both implementations
        //
        // IEqualityComparers that we'll use for finding files in _fileInfoList or in _blacklistedFileInfoList
        private IEqualityComparer<KeyValuePair<DateTime, string>> BlackListFilenameComparer = new BlacklistFullFilenameComparer();
        private IEqualityComparer<FileInfo> FullFilenameComparer = new FileInfoFullNameComparer();
        //
        // Data accessed through public members
        private IList<FileInfo> _fileInfoList = new List<FileInfo>();
        private IList<String> _blacklistedFullFilenames = new List<String>();
        private int _currentIndex;
        private object lockGetFile = new object();
        private object lockIndex = new object();

        // Fields specific to this implementation
        //
        // The Shell object we'll use to build our dictionairy of Shell Folder object
        private Shell32.Shell GlobalShell = new Shell32.Shell();
        //
        // The dictionary of Shell Folder objects we'll build at initialization
        public Dictionary<string, Shell32.Folder> ShellDict = new Dictionary<string, Shell32.Folder>();
        //
        private IList<DirectoryInfo> _directories = new List<DirectoryInfo>();

        int _dbgCountOfDirsSkippedDuringInit = 0;
        int _dbgCountOfFilesSkippedDuringInit = 0;
        int _dbgTotal = 0;

        #endregion Fields


        #region Constructors

        public MainFileInfoSource(List<DirectoryInfo> Directories, List<String> BlacklistFullFilenames = null, bool UseRecursion = false, bool UseShuffle = false)
        {
            if (Directories == null || Directories.Count < 1)
            {
                System.Diagnostics.Debug.WriteLine("MainFileInfoSource.Constructor: " +
                    "One of these happened: Directories == null || Directories.Count < 1" +
                    Environment.NewLine + "Object will not be created.");
#if DEBUG
                System.Diagnostics.Debug.Assert((false), "MainFileInfoSource.Constructor: Object will not be created.",
                    "One of these happened: Directories == null || Directories.Count < 1" +
                    Environment.NewLine + "If you click Continue, an exception will be raised. Program will likely fail.");
#endif
                throw new ArgumentException("Directories == null || Directories.Count < 1");
            }

            initializeFileInfoList(Directories, BlacklistFullFilenames, UseRecursion, UseShuffle);
        }

        #endregion Constructors


        #region Public Methods

        #region PMP Common To Both Implementations

        /// <summary>
        /// The list of Files.
        /// </summary>
        public IReadOnlyList<FileInfo> Files
        {
            get { return (IReadOnlyList<FileInfo>)_fileInfoList; }
        }

        /// <summary>
        /// The list of Files that have been blacklisted.
        /// </summary>
        public IReadOnlyList<String> BlacklistedFullFilenames
        {
            get { return (IReadOnlyList<String>)_blacklistedFullFilenames; }
        }

        /// <summary>
        /// Returns the count of Files in the Files List.
        /// </summary>
        public int Count
        {
            get { return _fileInfoList.Count; }
        }

        /// <summary>
        /// Gets the next FileInfo in the List.
        /// </summary>
        /// <returns>FileInfo if succesful, NULL if not successful.</returns>
        public FileInfo GetNextFile()
        {
            return getNextFile();
        }

        /// <summary>
        /// Gets the previous FileInfo in the List.
        /// </summary>
        /// <returns>FileInfo if succesful, NULL if not successful.</returns>
        public FileInfo GetPreviousFile()
        {
            return getPreviousFile();
        }

        /// <summary>
        /// Gets the current FileInfo.
        /// </summary>
        /// <returns>FileInfo located at CurrentIndex.</returns>
        public FileInfo GetCurrentFile()
        {
            return getFileAt(_currentIndex);
        }

        /// <summary>
        /// Gets the FileInfo located at the specified index.
        /// </summary>
        /// <param name="index">Index of desired FileInfo.</param>
        /// <returns>FileInfo located at index, or NULL if FileInfo cannot be returned.</returns>
        public FileInfo GetFileAt(int index)
        {
            return getFileAt(index);
        }

        /// <summary>
        /// Returns a FileInfo from the Files list matching the specified FullName.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns>If no FileInfo is found matching the FullName, null is returned.</returns>
        public FileInfo GetFileByFullName(string FullName)
        {
            return getFileByFullName(FullName);
        }

        /// <summary>
        /// The current index of the FileInfo list; (-1) if List is empty.
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                lock (lockIndex)
                {
                    if (_fileInfoList.Count < 1)
                    {
                        return (-1);
                    }
                    else
                    {
                        return _currentIndex;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the Index of the specified file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>The index if successful, -1 if the File was null or could not be found.</returns>
        public int IndexOf(FileInfo File)
        {
            return indexOf(File);
        }

        /// <summary>
        /// Removes the specified File from the Files list.
        /// </summary>
        /// <param name="File"></param>
        /// <returns> Returns true if successful, false if the file was null or could not be found.</returns>
        public bool RemoveFile(FileInfo File)
        {
            return removeFile(File);
        }

        /// <summary>
        /// Allows an ExploreThisFolderFileInfoSource object to tell the MainFileInfoSource object that it has blackliste a file.
        /// </summary>
        /// <param name="File">File that should now be blacklisted from the MainFileInfoSource files list.</param>
        /// <returns>True if successful, false if file was null, not found, or already in the MainFiles blacklist.</returns>
        public bool RemoveBlacklistedETFFile(FileInfo File)
        {
            lock (lockGetFile)
            {
                return removeBlacklistedETFFile(File);
            }
        }

        public bool BlacklistCurrentFile(out FileInfo NextOrPreviousFile, bool MovePrevious = false)
        {
            lock (lockGetFile)
            {
                return blacklistCurrentFile(out NextOrPreviousFile, MovePrevious);
            }
        }


        #endregion PMP Common To Both Implementations

        /// <summary>
        /// Rebuilds the object internally, preserving the existing blacklist, and adding any additional blacklist files.
        /// </summary>
        /// <param name="Directories">Required.</param>
        /// <param name="BlacklistFullFilenames">Optional.</param>
        /// <param name="UseRecursion"></param>
        /// <param name="UseShuffle"></param>
        public void Rebuild(List<DirectoryInfo> Directories, List<String> BlacklistFullFilenames = null, bool UseRecursion = false, bool UseShuffle = false)
        {
            lock (lockGetFile)
            {
                lock (lockIndex)
                {
                    if (Directories == null || Directories.Count < 1)
                    {
                        System.Diagnostics.Debug.WriteLine("Rebuild: " +
                            "One of these happened: Directories == null || Directories.Count < 1" +
                            Environment.NewLine + "Rebuild will fail. Exception will be thrown.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "MainFileInfoSource.Constructor: Object will not be created.",
                            "One of these happened: Directories == null || Directories.Count < 1" +
                            Environment.NewLine + "If you click Continue, an exception will be raised. Program will likely fail.");
#endif
                        throw new ArgumentException("Directories == null || Directories.Count < 1");
                    }

                    initializeFileInfoList(Directories, BlacklistFullFilenames, UseRecursion, UseShuffle, true);
                }
            }

        }

        #endregion Public Methods


        #region Private Methods

        #region Implementations of Public Members

        /// <summary>
        /// Returns NULL if List has no files, File at List[0] if an error occurs incrementing/decrementing Index.
        /// </summary>
        /// <returns></returns>
        private FileInfo getNextFile()
        {
            lock (lockGetFile)
            {
                if (_fileInfoList.Count < 1)                        // no elements in list
                {
                    return null;
                }

                decrementCurrentIndex();

                return _fileInfoList[_currentIndex];
            }
        }

        private bool decrementCurrentIndex()
        {
            lock (lockIndex)
            {
                if (_currentIndex == _fileInfoList.Count - 1)       // we're pointing at end of list
                {
                    // we want to cycle, so move index to 0
                    _currentIndex = 0;
                }
                else
                {
                    _currentIndex++;
                }

                // never let _currentIndex be less than zero
                if (_currentIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine("decrementCurrentIndex(): adjusted _currentIndex  < 0.  Avoiding exceptions, setting _currentIndex to 0.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "decrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to 0 if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                // never let _currentIndex be greater than _fileInfoList.Count - 1
                if (_currentIndex > _fileInfoList.Count - 1)
                {
                    System.Diagnostics.Debug.WriteLine("decrementCurrentIndex(): adjusted _currentIndex  < _fileInfoList.MaxIndex.  Avoiding exceptions, setting _currentIndex to_fileInfoList.MaxIndex.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "decrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to _fileInfoList.MaxIndex if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Returns NULL if List has no files, File at List[0] if an error occurs incrementing/decrementing Index.
        /// </summary>
        /// <returns></returns>
        private FileInfo getPreviousFile()
        {
            lock (lockGetFile)
            {
                if (_fileInfoList.Count < 1)                    // no elements in list
                {
                    return null;
                }

                incrementCurrentIndex();

                return _fileInfoList[_currentIndex];
            }
        }

        private bool incrementCurrentIndex()
        {
            lock (lockIndex)
            {
                if (_currentIndex == 0)                         // we're pointing to beginning of list
                {
                    // we want to cycle, so move index to Count - 1
                    _currentIndex = (_fileInfoList.Count - 1);
                }
                else
                {
                    _currentIndex--;
                }

                // never let _currentIndex be less than zero
                if (_currentIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine("incrementCurrentIndex(): adjusted _currentIndex  < 0.  Avoiding exceptions, setting _currentIndex to 0.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "incrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to 0 if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                // never let _currentIndex be greater than _fileInfoList.Count - 1
                if (_currentIndex > _fileInfoList.Count - 1)
                {
                    System.Diagnostics.Debug.WriteLine("incrementCurrentIndex(): adjusted _currentIndex  < _fileInfoList.MaxIndex.  Avoiding exceptions, setting _currentIndex to_fileInfoList.MaxIndex.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "incrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to _fileInfoList.MaxIndex if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Returns NULL if index is out of range.
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        private FileInfo getFileAt(int Index)
        {
            lock (lockGetFile)
            {
                if (_fileInfoList.Count < 1)    // no elements in list
                {
                    return null;
                }

                if (Index < 0)                  // negative index
                {
                    System.Diagnostics.Debug.WriteLine("getFileAt(): called with Index < 0.  Avoiding exceptions, returning NULL FileINfo.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "getFileAt(): called with Index < 0", "Will return NULL if you click Continue.");
#endif
                    return null;
                }

                if (Index > _fileInfoList.Count - 1)
                {
                    System.Diagnostics.Debug.WriteLine("getFileAt(): called with (Index > _fileInfoList.Count - 1).  Avoiding exceptions, returning NULL FileINfo.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "getFileAt(): called with (Index > _fileInfoList.Count - 1)", "Will return NULL if you click Continue.");
#endif
                    return null;
                }
                // no problems so far, just return it
                return _fileInfoList[Index];
            }
        }

        /// <summary>
        /// Returns NULL is file was not found in List.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        private FileInfo getFileByFullName(string FullName)
        {
            lock (lockGetFile)
            {
                IEnumerable<FileInfo> iefis = _fileInfoList.Where(c => c.FullName == FullName);
                List<FileInfo> fis = iefis.ToList();

                if (fis.Count == 1)         // found one file, normal case
                {
                    return fis[0];
                }
                else if (fis.Count == 0)    // we didn't find file, return NULL
                {
                    return null;
                }
                else if (fis.Count > 1)     // found multiple files, which is really weird
                {
                    System.Diagnostics.Debug.WriteLine("getFileByFullName() found multiple files, something is seriously wrong. Filename: " + FullName);
#if DEBUG
                    System.Diagnostics.Debug.Assert((false),
                        "getFileByFullName() found multiple files, something is seriously wrong. Filename: " + FullName,
                        "Will return first file found if you click Continue.");
#endif
                    return fis[0];
                }

                // we should not be able to get here, but compiler can't tell that, so we include a "return NULL" at end.
                System.Diagnostics.Debug.WriteLine("getFileByFullName(): No way! Fell through all conditionals.");
#if DEBUG
                System.Diagnostics.Debug.Assert((false),
                    "getFileByFullName(): No way! Fell through all conditionals." +
                    "Will return NULL if you click Continue.");
#endif
                return null;
            }
        }

        /// <summary>
        /// Returns -1 if the File was not found in the List.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        private int indexOf(FileInfo File)
        {
            lock (lockIndex)
            {
                if (File == null)
                {
                    System.Diagnostics.Debug.WriteLine("indexOf(): was passed a NULL FileInfo, returning (-1).");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false),
                        "indexOf(): was passed a NULL FileInfo. This may not be unexpected. " +
                        "Will return (-1) if you click Continue.");
#endif
                    return (-1);
                }

                return _fileInfoList.IndexOf(File);
            }
        }

        /// <summary>
        /// Returns true if successful, false if File was null or could not be found.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        private bool removeFile(FileInfo File)
        {
            lock (lockIndex)
            {
                lock (lockGetFile)
                {
                    if (File == null)
                    {
                        System.Diagnostics.Debug.WriteLine("removeFile(): was passed a NULL FileInfo, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false),
                            "removeFile(): was passed a NULL FileInfo" +
                            "Will return False if you click Continue.");
#endif
                        return false;
                    }
                    return _fileInfoList.Remove(File);
                }
            }
        }

        private bool removeBlacklistedETFFile(FileInfo File)
        {
            // If no file found, this method returns false, with null in NextOrPreviousFile.
            lock (lockGetFile)
            {
                lock (lockIndex)
                {
                    // Capture _currentIndex
                    int capturedCurrentIndex = _currentIndex;

                    System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): Entering, Main _currentIndex is: " + _currentIndex);

                    // -------  Handle Errors  --------- //

                    if (File == null)
                    {

                        System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): called with (File == null).  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "removeBlacklistedETFFile(): called with (File == null)",
                            "Will return False if you click Continue.");
#endif
                        return false;
                    }


                    if (_blacklistedFullFilenames.Contains(File.FullName))
                    {
                        System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): targeted file is already in _blacklistedFileInfoList.  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "removeBlacklistedETFFile(): targeted file is already in _blacklistedFileInfoList",
                            "Will return False if you click Continue.");
#endif
                        return false;
                    }

                    if (!_fileInfoList.Contains(File))
                    {
                        System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): targeted file not found in _fileInfoList.  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "removeBlacklistedETFFile(): targeted file not found in _fileInfoList",
                            "Will return False if you click Continue.");
#endif
                        return false;
                    }

                    // ------  Now we do work  ---------- //

                    int targetIndex = _fileInfoList.IndexOf(File);
                    System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): File we are attempting to blacklist, " + File.Name + ", is at Main index: " + targetIndex);

                    // LOGIC:
                    // Delete specified file from _fileInfolist. Add filename to blacklist. 
                    // If necessary, adjust _currentIndex accordingly.

                    // Remove the file from the _infoFileList
                    if (removeFile(File))
                    {
                        System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): After removing file, Main _currentIndex is at: " + _currentIndex);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("removeBlacklistedETFFile(): FAILED to REMOVE blacklisted file from _fileInfoList. Avoiding exceptions, returning False and Null.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "removeBlacklistedETFFile(): FAILED to REMOVE blacklisted file from _fileInfoList", "Will return False if you click Continue.");
#endif
                        return false;
                    }

                    // Adjust the index accordingly.
                    if (targetIndex < capturedCurrentIndex)
                    {
                        decrementCurrentIndex();
                    }

                    // Add file to blacklist
                    _blacklistedFullFilenames.Add(File.FullName);

                    return true;
                }
            }
        }

        private bool blacklistCurrentFile(out FileInfo NextOrPreviousFile, bool MovePrevious)
        {
            // Adjusts the _currentIndex to point at the returned file.

            lock (lockGetFile)
            {
                lock (lockIndex)
                {
                    // Capture _currentIndex
                    int capturedIndex = _currentIndex;
                    System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): Entering, _currentIndex is: " + _currentIndex);

                    FileInfo targetFile = GetCurrentFile();

                    // -------  Handle Errors  --------- //

                    if (targetFile == null)
                    {
                        System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): GetCurrentFile() returned null!!  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): GetCurrentFile() returned null!!",
                            "Not unexpected if _fileInfoList was empty. Safe as long as caller handles it.  " +
                            "If you click Continue now, blacklistCurrentFile() will return False and Null, which is again okay if caller handlers it." +
                            Environment.NewLine);
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }

                    if (_blacklistedFullFilenames.Contains(targetFile.FullName))
                    {
                        System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): GetCurrentFile() is already in _blacklistedFileInfoList.  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): GetCurrentFile() is already in _blacklistedFileInfoList",
                            "Will return False if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }


                    if (!_fileInfoList.Contains(targetFile))      // this handles the empty list case, also
                    {
                        System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): GetCurrentFile() not found in _fileInfoList.  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): GetCurrentFile() not found in _fileInfoList",
                            "Will return False if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }


                    // -------  Do The Work  --------- //

                    // LOGIC:
                    // Fetch next/prev file, store it. Delete specified file from _fileInfolist. Add filename to blacklist. 
                    // Send store next/prev file back, so the caller can have it drawn.

                    if (_fileInfoList.Count == 1) // we are trying to remove the only file in the list
                    {
                        NextOrPreviousFile = null;
                        // After file is deleted, test to see if _currentIndex would properly have been set at zero.
                    }
                    else
                    {
                        if (!MovePrevious)
                        {
                            NextOrPreviousFile = getNextFile();
                        }
                        else
                        {
                            NextOrPreviousFile = getPreviousFile();
                        }
                        // this caused the current index to move. Restore it to what it was an entry:
                        _currentIndex = capturedIndex;
                    }

                    // Remove the file from the _infoFileList
                    if (removeFile(targetFile))
                    {
                        System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): After removing GetCurrentFile(), index is at: " + _currentIndex);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): FAILED to REMOVE GetCurrentFile() file from _fileInfoList. Avoiding exceptions, returning False and Null.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): FAILED to REMOVE GetCurrentFile() file from _fileInfoList", "Will return False and Null if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }

                    // Add file to blacklist
                    _blacklistedFullFilenames.Add(targetFile.FullName);

                    // Adjust _currentIndex appropriately.
                    if (NextOrPreviousFile == null)   // we just removed the only file in the list
                    {
                        // I'm curious, would this have been zero anyway?
                        if (_currentIndex != 0)
                        {
                            System.Diagnostics.Debug.WriteLine("blackListFile(): After removing only file in list, index is curiously not ZERO, it is: " + _currentIndex);
                        }
                        _currentIndex = 0;
                    }
                    else
                    {
                        // point _currentIndex to the file we just returned 
                        _currentIndex = indexOf(NextOrPreviousFile);
                    }
                    System.Diagnostics.Debug.WriteLine("blackListFile(): After setting index to ZERO || indexOf(NextOrPreviousFile), index is at: " + _currentIndex);
                    return true;
                }
            }
        }

        #endregion Implementations of Public Members

        /// <summary>
        /// Initializes the Files list.
        /// </summary>
        private void initializeFileInfoList(List<DirectoryInfo> Directories, List<String> BlacklistedFullFilenames, bool UseRecursion, bool UseShuffle, bool Rebuild = false)
        {

            // Called from either the constructor or from Rebuild().
            // If called from Rebuild, the original blacklist is preserved.
            System.Diagnostics.Debug.WriteLine("initializeFileInfoList(): Beginning Filescan, building _fileInfoList.");

            _directories = Directories;

            if (BlacklistedFullFilenames != null)
            {
                if (Rebuild)
                {
                    IEnumerable<string> ies = (IEnumerable<string>)_blacklistedFullFilenames.Union((IEnumerable<string>)BlacklistedFullFilenames);
                    _blacklistedFullFilenames = ies.ToList();
                }
                else
                {
                    _blacklistedFullFilenames = BlacklistedFullFilenames;
                }
            }
            else
            {
                _blacklistedFullFilenames = new List<String>();
            }

            // Start timing for sequentialization
            System.Diagnostics.Stopwatch stpSequential = new System.Diagnostics.Stopwatch();
            stpSequential.Start();

            // clear debug variables
            _dbgCountOfDirsSkippedDuringInit = 0;
            _dbgCountOfFilesSkippedDuringInit = 0;
            _dbgTotal = 0;

            // clear the list
            _fileInfoList.Clear();
            ShellDict.Clear();

            // start a List of Lists that we'll eventually concatenate together
            List<IList<FileInfo>> LoL = new List<IList<FileInfo>>();

            // An IEnumerable customComparer we'll for eliminating duplicates from the FutureFiles list
            IEqualityComparer<FileInfo> FileInfoComparer = new FileInfoFullNameComparer();

            // for each dir in _directories, get it's files list, either shallowly or recursively
            for (int i = 0; i < _directories.Count; i++)
            {
                int _dbgFilesInDirCount = 0;
                IEnumerable<FileInfo> _iEnumerableFileInfo;

                if (UseRecursion)     // get files recursively below each folder in the folder list
                {
                    try
                    {   // Traverse and IsImageFile are Method Extensions from our code, GetDirectoryInfosWithoutThrowing
                        // and GetDirectoryInfosWithoutThrowing are methods from our code.
                        // Shell.Folders are added to dictionary in GetDirectoryInfosWithoutThrowing.
                        _iEnumerableFileInfo = new[] { _directories[i] }.Traverse(dir => GetDirectoryInfosWithoutThrowing(dir)).SelectMany(dir => GetFileInfosWithoutThrowing(dir).IsImageFile());
                        System.Diagnostics.Debug.WriteLine("     Recursion of " + _directories[i] + " and below:" + Environment.NewLine +
                            "     Directories skipped so far (Access Violations, errors): " + _dbgCountOfDirsSkippedDuringInit + Environment.NewLine +
                            "     Individual Files skipped: " + _dbgCountOfFilesSkippedDuringInit + Environment.NewLine +
                            "     Folder Entries in Shell Dictionary: " + ShellDict.Count);
                    }
                    catch (Exception ex)
                    {
                        _dbgCountOfDirsSkippedDuringInit++;
                        System.Diagnostics.Debug.WriteLine(" !!! Exception thrown recursing from directory " + _directories[i] + ", no files will be displayed from that directory or below. Exception: " + ex.Message);
                        System.Diagnostics.Debug.WriteLine("     Recursion of " + _directories[i] + " and below:" + Environment.NewLine +
                            "     Directories skipped so far (Access Violations, errors): " + _dbgCountOfDirsSkippedDuringInit + Environment.NewLine +
                            "     Individual Files skipped: " + _dbgCountOfFilesSkippedDuringInit + Environment.NewLine +
                            "     Folder Entries in Shell Dictionary: " + ShellDict.Count);
                        continue;
                    }
                }
                else    // non-recursive, just get the files directly in each folder in the folder list
                {
                    try
                    {
                        // IsImageFile() is a Method Extension from our code.  GetFiles() is a system call.
                        _iEnumerableFileInfo = _directories[i].GetFiles().IsImageFile();

                        // for each directory, add the Folder object which contains the extended file attributes for all of the files therein
                        Shell32.Folder objFolder;

                        objFolder = GlobalShell.NameSpace(_directories[i].FullName);
                        ShellDict.Add(_directories[i].FullName, objFolder);
                    }
                    catch (Exception ex)
                    {
                        _dbgCountOfDirsSkippedDuringInit++;
                        System.Diagnostics.Debug.WriteLine(" !!! Exception thrown calling GetFiles() from directory " + _directories[i] + ", no files will be displayed from that directory. Exception: " + ex.Message);
                        System.Diagnostics.Debug.WriteLine("     Directories skipped so far (Access Violations, errors): " + _dbgCountOfDirsSkippedDuringInit + Environment.NewLine +
                            "     Individual Files skipped so far: " + _dbgCountOfFilesSkippedDuringInit + Environment.NewLine +
                            "     Entries in Shell Dictionary: " + ShellDict.Count);
                        continue;
                    }
                }

                _dbgFilesInDirCount = 0;

                IList<FileInfo> tempList = new List<FileInfo>();

                try
                {
                    foreach (FileInfo fi in _iEnumerableFileInfo)
                    {
                        _dbgFilesInDirCount++;
                        _dbgTotal++;
                        tempList.Add(fi);       // add the FileInfo to the temp list
                    }
                }
                catch (Exception ex)
                {
                    // don't do anything except report some debug output
                    _dbgCountOfFilesSkippedDuringInit++;
                    System.Diagnostics.Debug.WriteLine(" !!! Exception accessing a file in directory " + _directories[i].FullName + ", file will be skipped. Exception: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("     Directories skipped so far (Access Violations, errors): " + _dbgCountOfDirsSkippedDuringInit + Environment.NewLine +
                        "     Individual Files skipped so far: " + _dbgCountOfFilesSkippedDuringInit + Environment.NewLine +
                        "     Entries in Shell Dictionary: " + ShellDict.Count);
                }

                // Debug output
                if (UseRecursion)
                {
                    System.Diagnostics.Debug.WriteLine("     Top Level Directory[" + i + "]: " + _directories[i].Name + ", Image Files in or below target: " + _dbgFilesInDirCount + ", Total Image Files: " + _dbgTotal);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("     Directory[" + i + "]: " + _directories[i].Name + ", Image Files in target: " + _dbgFilesInDirCount + ", Total Image Files: " + _dbgTotal);
                }

                // Add the tempList to our List of Lists.
                LoL.Add(tempList);
            }

            // Add the temp lists together, removing any duplicate entries (ie, c:\pics and c:\pics\spain handled recursively will create duplicate entries)
            foreach (List<FileInfo> li in LoL)
            {
                _fileInfoList = _fileInfoList.Union(li, FileInfoComparer).ToList();
            }

            System.Diagnostics.Debug.WriteLine("     Count of _fileInfoList after Union of Top Level Directory FileInfos: " + _fileInfoList.Count);

            // Stop timer
            stpSequential.Stop();

            System.Diagnostics.Debug.WriteLine("     Count of entries in ShellDict: " + ShellDict.Count);
            System.Diagnostics.Debug.WriteLine("     Time taken to build _theFiles: " + stpSequential.ElapsedMilliseconds + " milliseconds.");

            // this is where we remove the files that are in the blacklist
            stpSequential.Reset();
            stpSequential.Start();
            _fileInfoList = _fileInfoList.Where(c => !_blacklistedFullFilenames.Contains(c.FullName)).ToList();
            stpSequential.Stop();

            System.Diagnostics.Debug.WriteLine("     Count of _fileInfoList after removing blacklisted files: " + _fileInfoList.Count);
            System.Diagnostics.Debug.WriteLine("     Time taken to remove blacklisted files: " + stpSequential.ElapsedMilliseconds.ToString() + " milliseconds.");

            if (UseShuffle)
            {
                stpSequential.Reset();
                stpSequential.Start();
                ShuffleAFileInfoList(_fileInfoList);
                stpSequential.Stop();

                System.Diagnostics.Debug.WriteLine("     Time taken to Shuffle _fileInfoList: " + stpSequential.ElapsedMilliseconds.ToString() + " milliseconds.");
            }

            System.Diagnostics.Debug.WriteLine("     Total Directories skipped (Access Violations, errors): " + _dbgCountOfDirsSkippedDuringInit + Environment.NewLine +
                                        "     Total Individual Files skipped: " + _dbgCountOfFilesSkippedDuringInit + Environment.NewLine +
                                        "     Total Entries in Shell Dictionary: " + ShellDict.Count);

            // point the index at the last element in the list; getNext will get the zero'th element, and get previous will get MaxElement -1
            if (_fileInfoList.Count > 0)
            {
                _currentIndex = _fileInfoList.Count - 1;
            }
            else
            {
                _currentIndex = 0;
            }
        }

        /// <summary>
        /// Attempts to enumerate a list of DirectoryInfo from a specified Directory, ignoring exceptions. 
        /// </summary>
        /// <param name="dir">DirectoryInfo of directory to enumerate.</param>
        /// <returns>If not successful, returns an empty enumerable (test for Enumerable.Empty).</returns>
        /// </summary>
        private IEnumerable<DirectoryInfo> GetDirectoryInfosWithoutThrowing(DirectoryInfo dir)
        {
            // a logline here is safe, Access Violation exception doesn't occur until GetDirectories
            try
            {
                try
                {
                    // for each directory, add the extended file attributes to our dictionary of extended file attributes
                    Shell32.Folder objFolder;
                    objFolder = GlobalShell.NameSpace(dir.FullName);
                    ShellDict.Add(dir.FullName, objFolder);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception thrown trying to add objFolder to ShellDict: " + ex.Message);
                }

                return dir.GetDirectories();
            }
            catch (Exception ex)
            {
                _dbgCountOfDirsSkippedDuringInit++;
                System.Diagnostics.Debug.WriteLine("Error trying to do GetDirectories(), this directory will be skipped: " + dir.FullName);
                System.Diagnostics.Debug.WriteLine("Exception was: " + ex.Message);
                return Enumerable.Empty<DirectoryInfo>();
            }
        }

        /// <summary>
        /// Attempts to enumerate a list of FileInfo from a specified Directory, and ignores exceptions.
        /// </summary>
        /// <param name="dir">Directory to be enumerated.</param>
        /// <returns>Enumerable list of FileInfo.</returns>
        private IEnumerable<FileInfo> GetFileInfosWithoutThrowing(DirectoryInfo dir)
        {
            try
            {
                return dir.GetFiles();
            }
            catch (Exception)
            {
                _dbgCountOfDirsSkippedDuringInit++;
                // System.Diagnostics.Debug.WriteLine("Exception accessing files in directory " + dir.FullName + ", directory will be skipped. Exception: " + ex.Message);
                return Enumerable.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Shuffles the values in a list of FileInfos.
        /// </summary>
        /// <param name="fileInfos"></param>
        private static void ShuffleAFileInfoList(IList<FileInfo> fileInfos)
        {
            for (var i = 0; i < fileInfos.Count; i++)
                fileInfos.Swap(i, ThreadSafeRandom.ThisThreadsRandom.Next(i, fileInfos.Count));
        }

        /// <summary>
        /// Overkill - provides a ThreadSafe version of "Random".
        /// </summary>
        private static class ThreadSafeRandom
        {
            [ThreadStatic]
            private static Random Local;

            public static Random ThisThreadsRandom
            {
                get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
            }
        }

        #endregion Private Methods


        #region IEqualityComparers

        public class FileInfoFullNameComparer : IEqualityComparer<FileInfo>
        {
            public bool Equals(FileInfo x, FileInfo y)
            {
                return x.FullName.Equals(y.FullName);
            }

            public int GetHashCode(FileInfo obj)
            {
                return obj.FullName.GetHashCode();
            }
        }

        public class BlacklistFullFilenameComparer : IEqualityComparer<KeyValuePair<DateTime, string>>
        {
            public bool Equals(KeyValuePair<DateTime, string> x, KeyValuePair<DateTime, string> y)
            {
                return x.Value.Equals(y.Value);
            }

            public int GetHashCode(KeyValuePair<DateTime, string> obj)
            {
                return obj.Value.GetHashCode();
            }
        }

        #endregion IEqualityComparers

    }



    // ----------------   ExploreThisFolderFileInfoSource   ---------------- //

    public class ExploreThisFolderFileInfoSource
    {
        #region Fields

        // Fields that appear in both implementations
        //
        //// IEqualityComparers that we'll use for finding files in _fileInfoList or in _blacklistedFileInfoList
        //private IEqualityComparer<KeyValuePair<DateTime, string>> BlackListFilenameComparer = new BlacklistFullFilenameComparer();
        //private IEqualityComparer<FileInfo> FullFilenameComparer = new FileInfoFullNameComparer();
        ////
        // Data accessed through public members
        private IList<FileInfo> _fileInfoList = new List<FileInfo>();
        private IList<String> _blacklistedFullFilenames = new List<String>();
        private int _currentIndex;
        private object lockGetFile = new object();
        private object lockIndex = new object();

        // Fields specific to this implementation
        private MainFileInfoSource _initializingMainFileInfoSource;         // the MainFileInfoSource that spawned this object
        private FileInfo _initalizingFileInfo;                              // the file being viewed when this object was spawned
        private bool _initializingFileHasBeenBlacklisted;                   // lets us know if the initializing file was blacklisted during the ETF session
        private DirectoryInfo _directoryInfo;                               // the directory we are going to be exploring today

        #endregion Fields


        #region Constructors

        public ExploreThisFolderFileInfoSource(MainFileInfoSource FromThisMFInfoSource, FileInfo WithThisFile)
        {
            if (FromThisMFInfoSource == null || WithThisFile == null || WithThisFile.Exists == false)
            {
                System.Diagnostics.Debug.WriteLine("ExploreThisFolderFileInfoSource.Constructor: " +
                    "One of these happened: FromThisMFInfoSource == null || WithThisFile == null || WithThisFile.Exists == false" +
                    Environment.NewLine + "ETF Object will not be created.");
#if DEBUG
                System.Diagnostics.Debug.Assert((false), "ExploreThisFolderFileInfoSource.Constructor: Object will not be created.",
                    "One of these happened: FromThisMFInfoSource == null || WithThisFile == null || WithThisFile.Exists == false" +
                    Environment.NewLine + "If you click Continue, an exception will be raised. Program will continue safely, as long as caller handles this exception.");
#endif
                throw new ArgumentException("(FromThisMFInfoSource == null || WithThisFile == null || WithThisFile.Exists == false)");
            }

            _initializingMainFileInfoSource = FromThisMFInfoSource;
            _initalizingFileInfo = WithThisFile;
            _initializingFileHasBeenBlacklisted = false;
            if (!setDirectoryInfo(WithThisFile.Directory))
            {
                throw new ArgumentException("Could not get a DirectoryInfo from passed file: " + WithThisFile.FullName);
            }
        }

        #endregion Constructors


        #region Public Methods and Properties

        #region PMP Common To Both Implementations

        /// <summary>
        /// The list of Files.
        /// </summary>
        public IReadOnlyList<FileInfo> Files
        {
            get { return (IReadOnlyList<FileInfo>)_fileInfoList; }
        }

        /// <summary>
        /// The list of Files that have been blacklisted.
        /// </summary>
        public IReadOnlyList<String> BlacklistedFullFilenames
        {
            get { return (IReadOnlyList<String>)_blacklistedFullFilenames; }
        }

        /// <summary>
        /// Returns the count of Files in the Files List.
        /// </summary>
        public int Count
        {
            get { return _fileInfoList.Count; }
        }

        /// <summary>
        /// Gets the next FileInfo in the List.
        /// </summary>
        /// <returns>FileInfo if succesful, NULL if not successful.</returns>
        public FileInfo GetNextFile()
        {
            return getNextFile();
        }

        /// <summary>
        /// Gets the previous FileInfo in the List.
        /// </summary>
        /// <returns>FileInfo if succesful, NULL if not successful.</returns>
        public FileInfo GetPreviousFile()
        {
            return getPreviousFile();
        }

        /// <summary>
        /// Gets the current FileInfo.
        /// </summary>
        /// <returns>FileInfo located at CurrentIndex.</returns>
        public FileInfo GetCurrentFile()
        {
            return getFileAt(_currentIndex);
        }

        /// <summary>
        /// Gets the FileInfo located at the specified index.
        /// </summary>
        /// <param name="index">Index of desired FileInfo.</param>
        /// <returns>FileInfo located at index, or NULL if FileInfo cannot be returned.</returns>
        public FileInfo GetFileAt(int index)
        {
            return getFileAt(index);
        }

        /// <summary>
        /// Returns a FileInfo from the Files list matching the specified FullName.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns>If no FileInfo is found matching the FullName, null is returned.</returns>
        public FileInfo GetFileByFullName(string FullName)
        {
            return getFileByFullName(FullName);
        }

        /// <summary>
        /// The current index of the FileInfo list; (-1) if List is empty.
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                lock (lockIndex)
                {
                    if (_fileInfoList.Count < 1)
                    {
                        return (-1);
                    }
                    else
                    {
                        return _currentIndex;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the Index of the specified file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>The index if successful, -1 if the File was null or could not be found.</returns>
        public int IndexOf(FileInfo File)
        {
            return indexOf(File);
        }

        /// <summary>
        /// Removes the specified File from the Files list.
        /// </summary>
        /// <param name="File"></param>
        /// <returns> Returns true if successful, false if the file was null or could not be found.</returns>
        public bool RemoveFile(FileInfo File)
        {
            return removeFile(File);
        }

        public bool BlacklistCurrentFile(out FileInfo NextOrPreviousFile, bool MovePrevious = false)
        {
            lock (lockGetFile)
            {
                return blacklistCurrentFile(out NextOrPreviousFile, MovePrevious);
            }
        }



        #endregion PMP Common To Both Implementations

        /// <summary>
        /// Returns the FileInfo that was used to create this ExploreThisFolderFileInfoSource object.
        /// </summary>
        public FileInfo InitalizingFileInfo
        {
            get
            {
                return _initalizingFileInfo;
            }
        }

        /// <summary>
        /// Returns the MainFileInfoSource used to create this ExploreThisFolderFileInfoSource object.
        /// </summary>
        public MainFileInfoSource InitializingMainFileInfoSource
        {
            get
            {
                return _initializingMainFileInfoSource;
            }
        }

        /// <summary>
        /// Tells you whether the file used to create this object was blacklisted during the ETF session.
        /// </summary>
        public bool InitializingFileHasBeenBlacklisted
        {
            get { return _initializingFileHasBeenBlacklisted; }
        }

        /// <summary>
        /// The DirectoryInfo of the Folder being explored by this ExploreThisFolderFileInfoSource object.
        /// </summary>
        public DirectoryInfo DirectoryInfo
        {
            get
            {
                return _directoryInfo;
            }
        }

        #endregion Public Methods and Properties


        #region Private Methods

        #region Implementations of Public Members

        /// <summary>
        /// Returns NULL if List has no files, File at List[0] if an error occurs incrementing/decrementing Index.
        /// </summary>
        /// <returns></returns>
        private FileInfo getNextFile()
        {
            lock (lockGetFile)
            {
                if (_fileInfoList.Count < 1)                        // no elements in list
                {
                    return null;
                }

                decrementCurrentIndex();

                return _fileInfoList[_currentIndex];
            }
        }

        private bool decrementCurrentIndex()
        {
            lock (lockIndex)
            {
                if (_currentIndex == _fileInfoList.Count - 1)       // we're pointing at end of list
                {
                    // we want to cycle, so move index to 0
                    _currentIndex = 0;
                }
                else
                {
                    _currentIndex++;
                }

                // never let _currentIndex be less than zero
                if (_currentIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine("decrementCurrentIndex(): adjusted _currentIndex  < 0.  Avoiding exceptions, setting _currentIndex to 0.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "decrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to 0 if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                // never let _currentIndex be greater than _fileInfoList.Count - 1
                if (_currentIndex > _fileInfoList.Count - 1)
                {
                    System.Diagnostics.Debug.WriteLine("decrementCurrentIndex(): adjusted _currentIndex  < _fileInfoList.MaxIndex.  Avoiding exceptions, setting _currentIndex to_fileInfoList.MaxIndex.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "decrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to _fileInfoList.MaxIndex if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Returns NULL if List has no files, File at List[0] if an error occurs incrementing/decrementing Index.
        /// </summary>
        /// <returns></returns>
        private FileInfo getPreviousFile()
        {
            lock (lockGetFile)
            {
                if (_fileInfoList.Count < 1)                    // no elements in list
                {
                    return null;
                }

                incrementCurrentIndex();

                return _fileInfoList[_currentIndex];
            }
        }

        private bool incrementCurrentIndex()
        {
            lock (lockIndex)
            {
                if (_currentIndex == 0)                         // we're pointing to beginning of list
                {
                    // we want to cycle, so move index to Count - 1
                    _currentIndex = (_fileInfoList.Count - 1);
                }
                else
                {
                    _currentIndex--;
                }

                // never let _currentIndex be less than zero
                if (_currentIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine("incrementCurrentIndex(): adjusted _currentIndex  < 0.  Avoiding exceptions, setting _currentIndex to 0.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "incrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to 0 if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                // never let _currentIndex be greater than _fileInfoList.Count - 1
                if (_currentIndex > _fileInfoList.Count - 1)
                {
                    System.Diagnostics.Debug.WriteLine("incrementCurrentIndex(): adjusted _currentIndex  < _fileInfoList.MaxIndex.  Avoiding exceptions, setting _currentIndex to_fileInfoList.MaxIndex.");
#if DEBUG
                    System.Diagnostics.Debug.Assert((false), "incrementCurrentIndex(): adjusted _currentIndex  < 0", "Will set _currentIndex to _fileInfoList.MaxIndex if you click Continue.");
#endif
                    _currentIndex = 0;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Returns NULL if index is out of range.
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        private FileInfo getFileAt(int Index)
        {
            lock (lockGetFile)
            {
                if (_fileInfoList.Count < 1)    // no elements in list
                {
                    return null;
                }

                if (Index < 0)                  // negative index
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("getFileAt(): called with Index < 0.  Avoiding exceptions, returning NULL FileINfo.");
                    System.Diagnostics.Debug.Assert((false), "getFileAt(): called with Index < 0", "Will return NULL if you click Continue.");
                    return null;
#else
                    return null;
#endif
                }

                if (Index > _fileInfoList.Count - 1)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("getFileAt(): called with (Index > _fileInfoList.Count - 1).  Avoiding exceptions, returning NULL FileINfo.");
                    System.Diagnostics.Debug.Assert((false), "getFileAt(): called with (Index > _fileInfoList.Count - 1)", "Will return NULL if you click Continue.");
                    return null;
#else
                    return null;
#endif
                }
                // no problems so far, just return it
                return _fileInfoList[Index];
            }
        }

        /// <summary>
        /// Returns NULL is file was not found in List.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        private FileInfo getFileByFullName(string FullName)
        {
            lock (lockGetFile)
            {
                IEnumerable<FileInfo> iefis = _fileInfoList.Where(c => c.FullName == FullName);
                List<FileInfo> fis = iefis.ToList();

                if (fis.Count == 1)         // found one file, normal case
                {
                    return fis[0];
                }
                else if (fis.Count == 0)    // we didn't find file, return NULL
                {
                    return null;
                }
                else if (fis.Count > 1)     // found multiple files, which is really weird
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("getFileByFullName() found multiple files, something is seriously wrong. Filename: " + FullName);
                    System.Diagnostics.Debug.Assert((false),
                        "getFileByFullName() found multiple files, something is seriously wrong. Filename: " + FullName,
                        "Will return first file found if you click Continue.");
                    return fis[0];
#else
                    return fis[0];
#endif
                }

                // we should not be able to get here, but compiler can't tell that, so we include a "return NULL" at end.
#if DEBUG
                System.Diagnostics.Debug.WriteLine("getFileByFullName(): No way! Fell through all conditionals.");
                System.Diagnostics.Debug.Assert((false),
                    "getFileByFullName(): No way! Fell through all conditionals." +
                    "Will return NULL if you click Continue.");
                return null;
#else
                return null;
#endif
            }
        }

        /// <summary>
        /// Returns -1 if the File was not found in the List.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        private int indexOf(FileInfo File)
        {
            lock (lockIndex)
            {
                if (File == null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("indexOf(): was passed a NULL FileInfo, returning (-1).");
                    System.Diagnostics.Debug.Assert((false),
                        "indexOf(): was passed a NULL FileInfo" +
                        "Will return (-1) if you click Continue.");
                    return (-1);
#else
                    return (-1);
#endif
                }

                return _fileInfoList.IndexOf(File);
            }
        }

        /// <summary>
        /// Returns true if successful, false if File was null or could not be found.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        private bool removeFile(FileInfo File)
        {
            lock (lockIndex)
            {
                lock (lockGetFile)
                {
                    if (File == null)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("removeFile(): was passed a NULL FileInfo, returning False.");
                        System.Diagnostics.Debug.Assert((false),
                            "removeFile(): was passed a NULL FileInfo" +
                            "Will return False if you click Continue.");
                        return false;
#else
                    return false;
#endif
                    }

                    if (indexOf(File) < _currentIndex)
                    {
                        if (_currentIndex == _fileInfoList.Count - 1)
                        {
                            _currentIndex = 0;
                        }
                        else
                        {
                            _currentIndex++;
                        }
                    }
                    return _fileInfoList.Remove(File);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NextOrPreviousFile"></param>
        /// <param name="MovePrevious"></param>
        /// <returns></returns>
        private bool blacklistCurrentFile(out FileInfo NextOrPreviousFile, bool MovePrevious)
        {
            // Adjusts the _currentIndex to point at the returned file.

            lock (lockGetFile)
            {
                lock (lockIndex)
                {
                    // Capture _currentIndex
                    int capturedIndex = _currentIndex;
                    System.Diagnostics.Debug.WriteLine("blacklistCurrentFile(): Entering method, _currentIndex is: " + _currentIndex);

                    FileInfo targetFile = GetCurrentFile();

                    // -------  Handle Errors  --------- //

                    if (targetFile == null)
                    {
                        System.Diagnostics.Debug.WriteLine("   * blacklistCurrentFile(): GetCurrentFile() returned null!!  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): GetCurrentFile() returned null!!",
                            "Will return False and Null if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }

                    if (_blacklistedFullFilenames.Contains(targetFile.FullName))
                    {
                        System.Diagnostics.Debug.WriteLine("   * blacklistCurrentFile(): GetCurrentFile() is already in _blacklistedFileInfoList.  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): GetCurrentFile() is already in _blacklistedFileInfoList",
                            "Will return False and Null if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }

                    if (!_fileInfoList.Contains(targetFile))      // this handles the empty list case, also
                    {
                        System.Diagnostics.Debug.WriteLine("   * blacklistCurrentFile(): GetCurrentFile() not found in _fileInfoList.  Avoiding exceptions, returning False.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): GetCurrentFile() not found in _fileInfoList",
                            "Will return False if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }


                    // -------  Do The Work  --------- //

                    // LOGIC:
                    // Fetch next/prev file, store it. Delete specified file from _fileInfolist. Add filename to blacklist. 
                    // Send store next/prev file back, so the caller can have it drawn.

                    if (_fileInfoList.Count == 1) // we are trying to remove the only file in the list
                    {
                        NextOrPreviousFile = null;
                        // After file is deleted, test to see if _currentIndex would properly have been set at zero.
                    }
                    else
                    {
                        if (!MovePrevious)
                        {
                            NextOrPreviousFile = getNextFile();
                        }
                        else
                        {
                            NextOrPreviousFile = getPreviousFile();
                        }
                        // this caused the current index to move. Restore it to what it was an entry:
                        _currentIndex = capturedIndex;
                    }

                    // remember if target file is the initializing file
                    bool JustBlacklistedInitializingFile = (targetFile.FullName == _initalizingFileInfo.FullName);

                    // Remove the file from the _infoFileList
                    if (removeFile(targetFile))
                    {
                        System.Diagnostics.Debug.WriteLine("   blacklistCurrentFile(): After removing GetCurrentFile(), index is at: " + _currentIndex);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("   * blacklistCurrentFile(): FAILED to REMOVE GetCurrentFile() file from _fileInfoList. Avoiding exceptions, returning False and Null.");
#if DEBUG
                        System.Diagnostics.Debug.Assert((false), "blacklistCurrentFile(): FAILED to REMOVE GetCurrentFile() file from _fileInfoList", "Will return False and Null if you click Continue.");
#endif
                        NextOrPreviousFile = null;
                        return false;
                    }

                    // Add file to blacklist
                    _blacklistedFullFilenames.Add(targetFile.FullName);

                    // Adjust _currentIndex appropriately.
                    if (NextOrPreviousFile == null)   // we just removed the only file in the list
                    {
                        // I'm curious, would this have been zero anyway?
                        if (_currentIndex != 0)
                        {
                            System.Diagnostics.Debug.WriteLine("   ? blackListFile(): After removing only file in list, index is curiously not ZERO, it is: " + _currentIndex);
                            _currentIndex = 0;
                            System.Diagnostics.Debug.WriteLine("   blackListFile(): After removing only file in list, we just forced the index to ZERO.");
                        }
                    }
                    else
                    {
                        // point _currentIndex to the file we just returned 
                        _currentIndex = indexOf(NextOrPreviousFile);
                        System.Diagnostics.Debug.WriteLine("   blackListFile(): After setting index indexOf(NextOrPreviousFile), index is at: " + _currentIndex);
                    }

                    // set the value of _initializingFileHasBeenBlacklisted
                    _initializingFileHasBeenBlacklisted = JustBlacklistedInitializingFile;

                    System.Diagnostics.Debug.WriteLine("blackListFile(): Exiting method.");
                    return true;
                }
            }
        }



        #endregion Implementations of Public Members

        /// <summary>
        /// Kicks off the Files list initialization by providing the directory to build from.
        /// </summary>
        /// <param name="di"></param>
        /// <returns></returns>
        private bool setDirectoryInfo(DirectoryInfo di)
        {
            if (di == null || di.Exists == false)
            {
                return false;
            }

            _directoryInfo = di;
            initializeFileInfoList();
            return true;
        }

        /// <summary>
        /// Initializes the Files list.
        /// </summary>
        private void initializeFileInfoList()
        {
            Debug.WriteLine("ExploreThisFolder.initializeFileInfoList(): Entering method.");
            _fileInfoList.Clear();

            // this is where we would order by DateTaken, if we knew how to build an IComparer for it, handle blank values, etc.
            IEnumerable<FileInfo> iEnumerableFileInfo = _directoryInfo.GetFiles().IsImageFile().OrderBy(c => c.FullName);
            Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): Initial count of Graphics files in directory: " + iEnumerableFileInfo.ToList().Count());

            // this is where we remove the files that are in the blacklist. We compare them to our parent MFIS' blacklist, not our own.
            // Scot: read this as iEnumerableFileInfo = all the fileinfo in iEnumerableFileInfo that are not contained in the blacklist of our parent MFIS.
            iEnumerableFileInfo = iEnumerableFileInfo.Where(c => !(_initializingMainFileInfoSource.BlacklistedFullFilenames.Contains(c.FullName)));
            Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): Count of files after removing blacklisted files: " + iEnumerableFileInfo.ToList().Count());

            // this is where we force the enumeration of the whole list (by calling ToList), so that we have a nice static
            // list of Files, instead of an enumerator with deferred execution (a pointer to an item that promises to give
            // us files, one at a time, using the descriptions we've provided so far.)
            _fileInfoList = iEnumerableFileInfo.ToList();

            // now we find the file that was used to initialize us (used to enter ETF mode)
            // so that we can set the index back to that file
            int restoreIndex = 0;
            FileInfo fiFind;

            if (_initalizingFileInfo != null)
            {
                IEnumerable<FileInfo> iefis = _fileInfoList.Where(c => c.FullName == _initalizingFileInfo.FullName);
                List<FileInfo> fis = iefis.ToList();
                if (fis.Count == 1)
                {
                    fiFind = fis[0];
                    restoreIndex = _fileInfoList.IndexOf(fiFind);
                }
                else if (fis.Count == 0)    // we didn't find file, 
                {
                    System.Diagnostics.Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): WARNING: did not find initializing file in directory...");
                    restoreIndex = 0;
                }
                else if (fis.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): found multiple files matching _initalizingFileInfo.FullName, something is seriously wrong. Filename: " + _initalizingFileInfo.FullName);
                    fiFind = fis[0];
                    restoreIndex = _fileInfoList.IndexOf(fiFind);
                }
            }
            else  // _initalizingFileInfo was null.  That shouldn't happen.
            {
                System.Diagnostics.Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): WARNING: _initializingFileInfo was NULL.");
                restoreIndex = 0;
            }

            // set the current index to the file that was used to initalize us
            _currentIndex = restoreIndex;

            // Tell me about top and bottom
            if (_fileInfoList != null && _fileInfoList.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): First file is: " + _fileInfoList[0].Name + ", Last file is: " + _fileInfoList[_fileInfoList.Count - 1]);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("   ExploreThisFolder.initializeFileInfoList(): BEWARE - _fileInfoList is NULL or EMPTY.");
            }
        }

        #endregion Private Methods


        //#region IEqualityComparers

        //public class FileInfoFullNameComparer : IEqualityComparer<FileInfo>
        //{
        //    public bool Equals(FileInfo x, FileInfo y)
        //    {
        //        return x.FullName.Equals(y.FullName);
        //    }

        //    public int GetHashCode(FileInfo obj)
        //    {
        //        return obj.FullName.GetHashCode();
        //    }
        //}

        //public class BlacklistFullFilenameComparer : IEqualityComparer<KeyValuePair<DateTime, string>>
        //{
        //    public bool Equals(KeyValuePair<DateTime, string> x, KeyValuePair<DateTime, string> y)
        //    {
        //        return x.Value.Equals(y.Value);
        //    }

        //    public int GetHashCode(KeyValuePair<DateTime, string> obj)
        //    {
        //        return obj.Value.GetHashCode();
        //    }
        //}

        //#endregion IEqualityComparers

    }
}



//namespace System.IO
//{
//    public static partial class FileInfoMethodExtenstions
//    {
//        /// <summary>
//        /// Used by IsImageFile to determine if a file is a graphic type we care about. (Not an Extension Method, just a helper method)
//        /// </summary>
//        public static string[] GraphicFileExtensions = new string[] { ".png", ".bmp", ".gif", ".jpg", ".jpeg" };

//        /// <summary>
//        /// Retrieves the extensions from GraphicsFileExtensions as a single filter string.
//        /// </summary>
//        /// <returns></returns>
//        public static string GetGraphicFilesFilter()
//        {
//            string returnString = "";
//            foreach (string s in GraphicFileExtensions)
//            {
//                returnString = returnString + "*" + s + ";";
//            }

//            return returnString;
//        }

//        /// <summary>
//        /// Method Extension - specifies that FileInfo IEnumerable should only return files whose extension matches one in GraphicFileExtensions[]. 
//        /// </summary>
//        /// <param name="files"></param>
//        /// <returns></returns>
//        public static IEnumerable<FileInfo> IsImageFile(this IEnumerable<FileInfo> files)
//        {
//            foreach (FileInfo file in files)
//            {
//                string ext = file.Extension.ToLower();
//                if (GraphicFileExtensions.Contains(ext))
//                    yield return file;
//            }
//        }
//    }
//}

namespace System.Collections.Generic
{
    public static partial class IEnumerableMethodExtensions
    {
        /// <summary>
        /// Method Extension - used by "shuffle" methods when randomizing a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        /// <summary>
        /// Method Extension - Recursively builds and traverses a tree stucture.  Sort of.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="childrenSelector"></param>
        /// <returns></returns>
        /// <remarks>I'm not going to pretend I understand this completely. I do know how to call it, though.</remarks>
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
        {
            var stack = new Stack<T>(source);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childrenSelector(next))
                    stack.Push(child);
            }
        }
    }
}

//    // ----------------------   Base Class   ---------------------- //

//    public abstract class FileInfoSource
//    {

//        // Scot, this is the abstract version of the FileInfoSource class. As an abstract class, it cannot be
//        // instantiated directly. Instead, a new class must be defined, and inherit from this class.
//        //
//        // The abstract class defines the base functionality of any classes will which inherit from it. In
//        // our case, two new classes which inherit from this base: MainFileInfoSource and ETFFileInfoSource.
//        //
//        // Where functionality will need to be specific to the inheriting classes, we declare "abstract" 
//        // members here in the base class, forcing the inheriting classes to implement those members themselves.
//        //
//        // For members where we want to provide base functionality, but allow the inheriting classes to 
//        // override that functionaity, we declare those members as "virtual".
//        //
//        // Finally, for members where we want the base class to define functionality and we do NOT want
//        // the inheriting members to be allowed to change that functionality, we simply declare them without
//        // the "abstract" or "virtual" keywords. 


//        #region Data Fields
//        // Fields which are common between the two inheriting classes.
//        // "Protected" fields can be accessed by classes which inherit from this class.

//        // The Shell object we'll use to build our dictionairy of Shell Folder object
//        protected Shell32.Shell GlobalShell = new Shell32.Shell();
//        // The dictionary of Shell Folder objects we'll build at initialization
//        protected Dictionary<string, Shell32.Folder> ShellDict = new Dictionary<string, Shell32.Folder>();

//        // IEqualityComparers that we'll use for finding files in _fileInfoList or in _blacklistedFileInfoList
//        protected IEqualityComparer<KeyValuePair<DateTime, string>> BlackListFilenameComparer = new BlacklistFullFilenameComparer();
//        protected IEqualityComparer<FileInfo> FullFilenameComparer = new FileInfoFullNameComparer();

//        protected IList<FileInfo> _fileInfoList = new List<FileInfo>();
//        protected IList<FileInfo> _blacklistedFileInfoList = new List<FileInfo>();
//        protected int _currentIndex;
//        protected object lockGetFile = new object();
//        protected object lockIndex = new object();

//        #endregion Data Fields


//        #region Constructors

//        // Do not create constructors for base class, but ALWAYS provide constructors for 
//        // for derived classes

//        #endregion Constructors


//        #region Public Methods and Properties

//        /// <summary>
//        /// The list of Files.
//        /// </summary>
//        public IReadOnlyList<FileInfo> Files
//        {
//            get { return (IReadOnlyList<FileInfo>)_fileInfoList; }
//        }

//        /// <summary>
//        /// The list of Files that have been blacklisted.
//        /// </summary>
//        public IReadOnlyList<FileInfo> BlacklistedFiles
//        {
//            get { return (IReadOnlyList<FileInfo>)_blacklistedFileInfoList; }
//        }

//        /// <summary>
//        /// Returns the count of Files in the Files List.
//        /// </summary>
//        public int Count
//        {
//            get { return _fileInfoList.Count; }
//        }

//        /// <summary>
//        /// Gets the next FileInfo in the List.
//        /// </summary>
//        /// <returns>FileInfo if succesful, NULL if not successful.</returns>
//        public FileInfo GetNextFile()
//        {
//            return getNextFile();
//        }

//        /// <summary>
//        /// Gets the previous FileInfo in the List.
//        /// </summary>
//        /// <returns>FileInfo if succesful, NULL if not successful.</returns>
//        public FileInfo GetPreviousFile()
//        {
//            return getPreviousFile();
//        }

//        /// <summary>
//        /// Gets the current FileInfo.
//        /// </summary>
//        /// <returns>FileInfo located at CurrentIndex.</returns>
//        public FileInfo GetCurrentFile()
//        {
//            return getFileAt(_currentIndex);
//        }

//        /// <summary>
//        /// Gets the FileInfo located at the specified index.
//        /// </summary>
//        /// <param name="index">Index of desired FileInfo.</param>
//        /// <returns>FileInfo located at index, or NULL if FileInfo cannot be returned.</returns>
//        public FileInfo GetFileAt(int index)
//        {
//            return getFileAt(index);
//        }

//        /// <summary>
//        /// Returns a FileInfo from the Files list matching the specified FullName.
//        /// </summary>
//        /// <param name="FullName"></param>
//        /// <returns>If no FileInfo is found matching the FullName, null is returned.</returns>
//        public FileInfo GetFileByFullName(string FullName)
//        {
//            return getFileByFullName(FullName);
//        }

//        /// <summary>
//        /// The current index of the FileInfo list; (-1) if List is empty.
//        /// </summary>
//        public int CurrentIndex
//        {
//            get
//            {
//                lock (lockIndex)
//                {
//                    if (_fileInfoList.Count < 1)
//                    {
//                        return (-1);
//                    }
//                    else
//                    {
//                        return _currentIndex;
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Returns the Index of the specified file, (-1) if Files list is empty.
//        /// </summary>
//        /// <param name="filename"></param>
//        /// <returns></returns>
//        public int IndexOf(FileInfo File)
//        {
//            return indexOf(File);
//        }

//        public bool RemoveFile(FileInfo File)
//        {
//            return removeFile(File);
//        }

//        #endregion Public Methods and Properties


//        #region Private Methods

//        #region Implementations of Public Members

//        /// <summary>
//        /// Returns NULL if List has no files, File at List[0] if an error occurs incrementing/decrementing Index.
//        /// </summary>
//        /// <returns></returns>
//        private FileInfo getNextFile()
//        {
//            lock (lockGetFile)
//            {
//                if (_fileInfoList.Count < 1)                        // no elements in list
//                {
//                    return null;
//                }

//                if (_currentIndex == _fileInfoList.Count - 1)       // we're pointing at end of list
//                {
//                    // we want to cycle, so move index to 0
//                    _currentIndex = 0;
//                }
//                else
//                {
//                    _currentIndex++;
//                }

//                // never let _currentIndex be less than zero
//                if (_currentIndex < 0)
//                {
//                    _currentIndex = 0;
//                }

//                return _fileInfoList[_currentIndex];
//            }
//        }

//        /// <summary>
//        /// Returns NULL if List has no files, File at List[0] if an error occurs incrementing/decrementing Index.
//        /// </summary>
//        /// <returns></returns>
//        private FileInfo getPreviousFile()
//        {
//            lock (lockGetFile)
//            {
//                if (_fileInfoList.Count < 1)                    // no elements in list
//                {
//                    return null;
//                }

//                if (_currentIndex == 0)                         // we're pointing to beginning of list
//                {
//                    // we want to cycle, so move index to Count - 1
//                    _currentIndex = (_fileInfoList.Count - 1);
//                }
//                else
//                {
//                    _currentIndex--;
//                }

//                return _fileInfoList[_currentIndex];
//            }
//        }

//        /// <summary>
//        /// Returns NULL if index is out of range.
//        /// </summary>
//        /// <param name="Index"></param>
//        /// <returns></returns>
//        private FileInfo getFileAt(int Index)
//        {
//            lock (lockGetFile)
//            {
//                if (_fileInfoList.Count < 1)    // no elements in list
//                {
//                    return null;
//                }

//                if (Index < 0)                  // negative index
//                {
//#if DEBUG
//                    System.Diagnostics.Debug.WriteLine("getFileAt(): called with Index < 0.  Avoiding exceptions, returning NULL FileINfo.");
//                    System.Diagnostics.Debug.Assert((false), "getFileAt(): called with Index < 0", "Will return NULL if you click Continue.");
//                    return null;
//#else
//                    return null;
//#endif
//                }

//                if (Index > _fileInfoList.Count - 1)
//                {
//#if DEBUG
//                    System.Diagnostics.Debug.WriteLine("getFileAt(): called with (Index > _fileInfoList.Count - 1).  Avoiding exceptions, returning NULL FileINfo.");
//                    System.Diagnostics.Debug.Assert((false), "getFileAt(): called with (Index > _fileInfoList.Count - 1)", "Will return NULL if you click Continue.");
//                    return null;
//#else
//                    return null;
//#endif
//                }
//                // no problems so far, just return it
//                return _fileInfoList[Index];
//            }
//        }

//        /// <summary>
//        /// Returns NULL is file was not found in List.
//        /// </summary>
//        /// <param name="FullName"></param>
//        /// <returns></returns>
//        private FileInfo getFileByFullName(string FullName)
//        {
//            lock (lockGetFile)
//            {
//                IEnumerable<FileInfo> iefis = _fileInfoList.Where(c => c.FullName == FullName);
//                List<FileInfo> fis = iefis.ToList();

//                if (fis.Count == 1)         // found one file, normal case
//                {
//                    return fis[0];
//                }
//                else if (fis.Count == 0)    // we didn't find file, return NULL
//                {
//                    return null;
//                }
//                else if (fis.Count > 1)     // found multiple files, which is really weird
//                {
//#if DEBUG
//                    System.Diagnostics.Debug.WriteLine("getFileByFullName() found multiple files, something is seriously wrong. Filename: " + FullName);
//                    System.Diagnostics.Debug.Assert((false), 
//                        "getFileByFullName() found multiple files, something is seriously wrong. Filename: " + FullName,
//                        "Will return first file found if you click Continue.");
//                    return fis[0];
//#else
//                    return fis[0];
//#endif
//                }

//                // we should not be able to get here, but compiler can't tell that, so we include a "return NULL" at end.
//#if DEBUG
//                System.Diagnostics.Debug.WriteLine("getFileByFullName(): No way! Fell through all conditionals.");
//                System.Diagnostics.Debug.Assert((false),
//                    "getFileByFullName(): No way! Fell through all conditionals." +
//                    "Will return NULL if you click Continue.");
//                return null;
//#else
//                return null;
//#endif
//            }
//        }

//        /// <summary>
//        /// Returns -1 if the File was not found in the List.
//        /// </summary>
//        /// <param name="File"></param>
//        /// <returns></returns>
//        private int indexOf(FileInfo File)
//        {
//            lock (lockIndex)
//            {
//                if (File == null)
//                {
//#if DEBUG
//                    System.Diagnostics.Debug.WriteLine("indexOf(): was passed a NULL FileInfo, returning (-1).");
//                    System.Diagnostics.Debug.Assert((false),
//                        "indexOf(): was passed a NULL FileInfo" +
//                        "Will return (-1) if you click Continue.");
//                    return (-1);
//#else
//                    return (-1);
//#endif
//                }

//                return _fileInfoList.IndexOf(File);
//            }
//        }

//        private bool removeFile(FileInfo File)
//        {
//            lock (lockIndex)
//            {
//                lock (lockGetFile)
//                {
//                    if (File == null)
//                    {
//#if DEBUG
//                        System.Diagnostics.Debug.WriteLine("removeFile(): was passed a NULL FileInfo, returning False.");
//                        System.Diagnostics.Debug.Assert((false),
//                            "removeFile(): was passed a NULL FileInfo" +
//                            "Will return False if you click Continue.");
//                        return false;
//#else
//                    return false;
//#endif
//                    }

//                    if (indexOf(File) < _currentIndex)
//                    {
//                        if (_currentIndex == _fileInfoList.Count - 1)
//                        {
//                            _currentIndex = 0;
//                        }
//                        else
//                        {
//                            _currentIndex++;
//                        }
//                    }
//                    return _fileInfoList.Remove(File);
//                }
//            }
//        }

//        #endregion Implementations of Public Members


//        #region IEqualityComparers

//        public class FileInfoFullNameComparer : IEqualityComparer<FileInfo>
//        {
//            public bool Equals(FileInfo x, FileInfo y)
//            {
//                return x.FullName.Equals(y.FullName);
//            }

//            public int GetHashCode(FileInfo obj)
//            {
//                return obj.FullName.GetHashCode();
//            }
//        }

//        public class BlacklistFullFilenameComparer : IEqualityComparer<KeyValuePair<DateTime, string>>
//        {
//            public bool Equals(KeyValuePair<DateTime, string> x, KeyValuePair<DateTime, string> y)
//            {
//                return x.Value.Equals(y.Value);
//            }

//            public int GetHashCode(KeyValuePair<DateTime, string> obj)
//            {
//                return obj.Value.GetHashCode();
//            }
//        }

//        #endregion IEqualityComparers


//        #endregion Private Methods

//    }
