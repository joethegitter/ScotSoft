using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Drawing;
using System.Drawing.Text;

using ScotSoft;
using ScotSoft.PattySaver;
using ScotSoft.PattySaver.DebugUtils;
using ScotSoft.PattySaver.LaunchManager;

namespace ScotSoft.PattySaver
{

    /// <summary>
    /// This class holds the configuration data for the screensaver, and methods to get/set that data, and read/write it from storage.
    /// </summary>
    public class SettingsInfo
    {
        static bool fDebugOutput = true;
        static bool fDebugAtTraceLevel = false;
        static bool fDebugTrace = false;  // do not modify, recalculated at method level

        // Defaults
        public static bool ShuffleMode = false;
        public static bool ShowMetadata = false;
        public static bool UseCheckedFoldersOnly = false;
        public static bool UseRecursion = false;
        public static int SlideshowIntervalInSecs = 0;
        public static string SettingsDialogAddFolderLastSelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string metaDataFont_fontName = "Segoe UI";
        public static float metaDataFont_fontSize = 9;
        public static FontStyle metaDataFont_fontStyle = FontStyle.Regular;
        public static string metaDataFont_fontColorName = "Aqua";
        public static TextRenderingHint metaDataFont_textRenderingHint = TextRenderingHint.AntiAliasGridFit;
        public static int metaDataFont_contrastLevel = 0;
        public static float metaDataFont_maxFontSize = 124F;
        public static float metaDataFont_minFontSize = 6F;
        public static bool metaDataFont_shadowing = true;
        public static bool metaDataFont_allowNonAntiAliased = false;

        public static List<KeyValuePair<bool, string>> DirectoriesList = new List<KeyValuePair<bool, string>>();
        public static List<KeyValuePair<bool, string>> MetaDataList = new List<KeyValuePair<bool, string>>();
        public static List<KeyValuePair<DateTime, String>> BlacklistedFilenames = new List<KeyValuePair<DateTime, String>>();

        public static System.Drawing.Point dbgLastWindowLocationPoint = new System.Drawing.Point(0, 0);              // default window location for debug mode
        public static System.Drawing.Size dbgLastWindowSize =
            new System.Drawing.Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2,
            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2);                               // default window size for debug mode

        // An IEnumerable customComparer we'll use in adding filenames to the Blacklist
        private static IEqualityComparer<KeyValuePair<DateTime, string>> BlackListFilenameComparer = new BlacklistFullFilenameComparer();
        private static DateTime dummyDT = new DateTime(1961, 4, 26);                    // Dummy value we need for building comparison kvps


        /// <summary>
        /// Writes ConfigSettings block to one or more files on disk.
        /// </summary>
        public static void SaveConfigSettingsToStorage()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "SaveConfigSettingsToStorage(): entered.");

            // Create a table to hold non-list-based settings
            DataTable dtData = new DataTable("ConfigurationData", "myTableNamespace");
            dtData.Columns.Add("ShuffleMode", ShuffleMode.GetType());
            dtData.Columns.Add("ShowMetadata", ShowMetadata.GetType());
            dtData.Columns.Add("UseCheckedFoldersOnly", UseCheckedFoldersOnly.GetType());
            dtData.Columns.Add("UseRecursion", UseRecursion.GetType());
            dtData.Columns.Add("SlideshowInterval", SlideshowIntervalInSecs.GetType());
            dtData.Columns.Add("dbgLastWindowLocationPoint", dbgLastWindowLocationPoint.GetType());
            dtData.Columns.Add("dbgLastWindowSize", dbgLastWindowSize.GetType());
            dtData.Columns.Add("SettingsDialogAddFolderLastSelectedPath", SettingsDialogAddFolderLastSelectedPath.GetType());
            dtData.Columns.Add("metaDataFont_fontName", metaDataFont_fontName.GetType());
            dtData.Columns.Add("metaDataFont_fontSize", metaDataFont_fontSize.GetType());
            dtData.Columns.Add("metaDataFont_fontStyle", metaDataFont_fontStyle.GetType());
            dtData.Columns.Add("metaDataFont_fontColorName", metaDataFont_fontColorName.GetType());
            dtData.Columns.Add("metaDataFont_textRenderingHint", metaDataFont_textRenderingHint.GetType());
            dtData.Columns.Add("metaDataFont_contrastLevel", metaDataFont_contrastLevel.GetType());
            dtData.Columns.Add("metaDataFont_maxFontSize", metaDataFont_maxFontSize.GetType());
            dtData.Columns.Add("metaDataFont_minFontSize", metaDataFont_minFontSize.GetType());
            dtData.Columns.Add("metaDataFont_shadowing", metaDataFont_shadowing.GetType());
            dtData.Columns.Add("metaDataFont_allowNonAntiAliased", metaDataFont_allowNonAntiAliased.GetType());

            // Add current value of those settings to the table. All of the values are 
            // stored in the first (and only) row of this table, one columun per setting.
            DataRow drd = dtData.NewRow();
            drd["ShuffleMode"] = ShuffleMode;
            drd["ShowMetadata"] = ShowMetadata;
            drd["UseCheckedFoldersOnly"] = UseCheckedFoldersOnly;
            drd["UseRecursion"] = UseRecursion;
            drd["SlideshowInterval"] = SlideshowIntervalInSecs;
            drd["dbgLastWindowLocationPoint"] = dbgLastWindowLocationPoint;
            drd["dbgLastWindowSize"] = dbgLastWindowSize;
            drd["SettingsDialogAddFolderLastSelectedPath"] = SettingsDialogAddFolderLastSelectedPath;
            drd["metaDataFont_fontName"] = metaDataFont_fontName;
            drd["metaDataFont_fontSize"] = metaDataFont_fontSize;
            drd["metaDataFont_fontStyle"] = metaDataFont_fontStyle;
            drd["metaDataFont_fontColorName"] = metaDataFont_fontColorName;
            drd["metaDataFont_textRenderingHint"] = metaDataFont_textRenderingHint;
            drd["metaDataFont_contrastLevel"] = metaDataFont_contrastLevel;
            drd["metaDataFont_maxFontSize"] = metaDataFont_maxFontSize;
            drd["metaDataFont_minFontSize"] = metaDataFont_minFontSize;
            drd["metaDataFont_shadowing"] = metaDataFont_shadowing;
            drd["metaDataFont_allowNonAntiAliased"] = metaDataFont_allowNonAntiAliased;
            dtData.Rows.Add(drd);

            // Create another table to hold list based data (files and metadata settings so far);
            // we use a second table because the schema for storing "an unknown number of items" 
            // is different than that for "all of these other values, whose count is fixed".
            DataTable dtLists = new DataTable("ListsData", "myTableNamespace");
            dtLists.Columns.Add("RowType", typeof(string));  // we'll use "File" or "MetaData"
            dtLists.Columns.Add("Designated", typeof(bool));
            dtLists.Columns.Add("Name", typeof(string));

            // Add current value of those settings to the table. First column is Type of data,
            // second column is "user has specified to use this value" (ie, checked a checkmark
            // in our Folders list), and third column is the value of the setting.
            foreach (KeyValuePair<bool, string> kvp in DirectoriesList)
            {
                DataRow drl = dtLists.NewRow();
                drl["RowType"] = "File";
                drl["Designated"] = kvp.Key;
                drl["Name"] = kvp.Value;
                dtLists.Rows.Add(drl);
            }

            // Create another table to hold the filenames we have blacklisted.
            DataTable dtBlacklist = new DataTable("BlacklistData", "myTableNamespace");
            dtBlacklist.Columns.Add("DateStamp", typeof(DateTime));
            dtBlacklist.Columns.Add("FullFilename", typeof(string));

            // Add current values to that table
            foreach (KeyValuePair<DateTime, string> kvp in BlacklistedFilenames)
            {
                DataRow drb = dtBlacklist.NewRow();
                drb["DateStamp"] = kvp.Key;
                drb["FullFilename"] = kvp.Value;
                dtBlacklist.Rows.Add(drb);
            }

            // Now write these three tables to disk. 

            // First table:
            // Start by deleting the existing Config file, if it exists.
            try
            {
                if (System.IO.File.Exists(GetConfigSettingsDataFileName()))
                {
                    System.IO.File.Delete(GetConfigSettingsDataFileName());
                }

                // Then write the first table as one file.
                // This is why we use a table for storage. It knows how to write itself to disk as XML!
                dtData.WriteXml(GetConfigSettingsDataFileName(), XmlWriteMode.WriteSchema);
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "Error: could not save Data Config File to path: " + GetConfigSettingsDataFileName() + ", Exception: " + ex.Message);
            }

            // Now write the second table as a separate file.
            try
            {
                if (System.IO.File.Exists(GetConfigSettingsListFileName()))
                {
                    System.IO.File.Delete(GetConfigSettingsListFileName());
                }

                dtLists.WriteXml(GetConfigSettingsListFileName(), XmlWriteMode.WriteSchema);
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "Error: could not save Lists Config File to path: " + GetConfigSettingsListFileName() + ", Exception: " + ex.Message);
            }

            // Now write the third table as a separate file.
            try
            {
                if (System.IO.File.Exists(GetConfigSettingsBlacklistFileName()))
                {
                    System.IO.File.Delete(GetConfigSettingsBlacklistFileName());
                }

                dtBlacklist.WriteXml(GetConfigSettingsBlacklistFileName(), XmlWriteMode.WriteSchema);
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "Error: could not save Blacklist Config File to path: " + GetConfigSettingsBlacklistFileName() + ", Exception: " + ex.Message);
            }

            Logging.LogLineIf(fDebugTrace, "SaveConfigSettingsToStorage(): exiting.");

        }

        /// <summary>
        /// Reads data from disk into ConfigSettings block.
        /// </summary>
        public static void InitializeAndLoadConfigSettingsFromStorage()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "InitializeAndLoadConfigSettingsFromStorage(): entered.");

            // Load the non-list data from storage (will set defaults if no stored values)
            SetNonListData_FromPersistedStorage();                  // Read non-list data from persisted storage

            // Clear the Files, Metadata, and BlacklistFile lists so we can start clean.
            InitializeAndSetDefaultAllLists();

            // Load list-based data from storage
            AddDirectoriesAndMetadataLists_FromPersistedStorage();    // Read in the second table.
            AddBlacklistedFilesList_FromPersistedStorage();                       // Read in the third table.

            Logging.LogLineIf(fDebugTrace, "InitializeAndLoadConfigSettingsFromStorage(): exiting.");
        }

        public static void AddFullFilenamesToBlacklist(List<String> FullFilenames)
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "AddFullFilenamesToBlacklist(): entered.");

            KeyValuePair<DateTime, string> kvp;

            foreach (string FullFilename in FullFilenames)
            {
                kvp = new KeyValuePair<DateTime, string>(dummyDT, FullFilename);
                if (!BlacklistedFilenames.Contains(kvp, BlackListFilenameComparer))
                {
                    BlacklistedFilenames.Add(kvp);
                }
            }
            Logging.LogLineIf(fDebugTrace, "AddFullFilenamesToBlacklist(): entered.");
        }

        public static List<String> GetBlacklistedFullFilenames()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "GetBlacklistedFullFilenames(): entered.");

            List<string> retVal = new List<string>();

            foreach (KeyValuePair<DateTime, String> kvp in BlacklistedFilenames)
            {
                retVal.Add(kvp.Value);
            }

            Logging.LogLineIf(fDebugTrace, "GetBlacklistedFullFilenames(): exiting.");
            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IList<DirectoryInfo> GetListOfDirectoryInfo()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "GetListOfDirectoryInfo(): entered.");

            IList<DirectoryInfo> retVal = new List<DirectoryInfo>();

            foreach (KeyValuePair<bool, string> kvp in SettingsInfo.DirectoriesList)
            {
                // If we are in UseCheckedDirsOnly mode, return only the checked dirs
                if (SettingsInfo.UseCheckedFoldersOnly)
                {
                    if (kvp.Key == true)
                    {
                        retVal.Add(new DirectoryInfo(kvp.Value));
                    }
                }
                else    // ignore checked state and add anyway
                {
                    retVal.Add(new DirectoryInfo(kvp.Value));
                }
            }

            // If we came out with zero dirs (or zero checked dirs), return special folder pictures dir instead.
            if (retVal.Count < 1)
            {
                Logging.LogLineIf(fDebugOutput, "No directories listed (and/or checked) in config file, using My Pictures.");
                retVal.Add(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));
            }

            Logging.LogLineIf(fDebugTrace, "GetListOfDirectoryInfo(): entered.");
            return retVal;
        }

        /// <summary>
        /// Refreshes ConfigSettings with just the Directories and MetaData Lists.
        /// </summary>
        private static void AddDirectoriesAndMetadataLists_FromPersistedStorage()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "AddDirectoriesAndMetadataLists_FromPersistedStorage(): entered.");

            bool readFromDisk = false;                  // Did we successfully read from disk?
            DataTable dtLists = new DataTable();

            try
            {
                // Tables also know how to read themselves from XML
                dtLists.ReadXml(GetConfigSettingsListFileName());
                readFromDisk = true;
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "Could not read Lists Config file from " + GetConfigSettingsListFileName() + "; Exception: " + ex.Message);
            }

            // We didn't successfully read from disk, so set Default configuraton value.
            if (!readFromDisk)
            {
                InitializeAndSetDefaultAllLists();
            }
            else
            {
                // from the now in-memory table, create the two Lists in our Config Block
                for (int i = 0; i < dtLists.Rows.Count; i++)
                {
                    // Get the value from the first column that tells us which type of data this is
                    string rowType = (string)dtLists.Rows[i]["RowType"];
                    if (rowType == "File")
                    {
                        // Add the data from that row to the correct List
                        DirectoriesList.Add(new KeyValuePair<bool, string>((bool)dtLists.Rows[i]["Designated"], (string)dtLists.Rows[i]["Name"]));
                    }
                    else if (rowType == "MetaData")
                    {
                        MetaDataList.Add(new KeyValuePair<bool, string>((bool)dtLists.Rows[i]["Designated"], (string)dtLists.Rows[i]["Name"]));
                    }
                    else
                    {
                        Logging.LogLineIf(fDebugOutput, "Error reading data from Lists Config: encountered rogue 'RowType' column vallue: " + rowType);
                    }
                }
            }
            Logging.LogLineIf(fDebugTrace, "AddDirectoriesAndMetadataLists_FromPersistedStorage(): entered.");
        }

        private static void AddBlacklistedFilesList_FromPersistedStorage()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "AddBlacklistedFilesList_FromPersistedStorage(): entered.");

            bool readFromDisk = false;                  // Did we successfully read from disk?
            DataTable dtBlacklist = new DataTable();

            try
            {
                // Tables also know how to read themselves from XML
                dtBlacklist.ReadXml(GetConfigSettingsBlacklistFileName());
                readFromDisk = true;
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "Could not read Blacklist Config file from " + GetConfigSettingsBlacklistFileName() + "; Exception: " + ex.Message);
            }

            // If we didn't successfully read from disk, set Default configuraton value.
            if (!readFromDisk)
            {
                // Don't do anything, ConfigSettings constructor property initializes this list
            }
            else
            {
                // from the now in-memory table, create the Blacklist List in our Config Block
                for (int i = 0; i < dtBlacklist.Rows.Count; i++)
                {
                    BlacklistedFilenames.Add(new KeyValuePair<DateTime, string>((DateTime)dtBlacklist.Rows[i]["DateStamp"], (string)dtBlacklist.Rows[i]["FullFilename"]));
                }
            }
            Logging.LogLineIf(fDebugTrace, "AddBlacklistedFilesList_FromPersistedStorage(): exiting.");
        }

        private static void SetNonListData_FromPersistedStorage()
        {
            fDebugTrace = fDebugOutput && fDebugAtTraceLevel;

            Logging.LogLineIf(fDebugTrace, "SetNonListData_FromPersistedStorage(): entered.");

            bool readFromDisk = false;
            DataTable dtData = new DataTable();

            try
            {
                dtData.ReadXml(GetConfigSettingsDataFileName());
                readFromDisk = true;
            }
            catch (Exception ex)
            {
                Logging.LogLineIf(fDebugOutput, "Could not read Data Config file from " + GetConfigSettingsDataFileName() + "; Exception: " + ex.Message);
            }

            if (!readFromDisk)
            {
                InitializeAndSetDefault_NonListDataValues();
            }
            else
            {
                DataRow row = dtData.Rows[0];
                ShuffleMode = (bool)row["ShuffleMode"];
                ShowMetadata = (bool)row["ShowMetadata"];
                UseCheckedFoldersOnly = (bool)row["UseCheckedFoldersOnly"];
                UseRecursion = (bool)row["UseRecursion"];
                SlideshowIntervalInSecs = (int)row["SlideshowInterval"];
                SettingsDialogAddFolderLastSelectedPath = (string)row["SettingsDialogAddFolderLastSelectedPath"];
                metaDataFont_fontName = (string)row["metaDataFont_fontName"];
                metaDataFont_fontSize = (float)row["metaDataFont_fontSize"];
                metaDataFont_fontStyle = (FontStyle)row["metaDataFont_fontStyle"];
                metaDataFont_fontColorName = (string)row["metaDataFont_fontColorName"];
                metaDataFont_textRenderingHint = (TextRenderingHint)row["metaDataFont_textRenderingHint"];
                metaDataFont_contrastLevel = (int)row["metaDataFont_contrastLevel"];
                metaDataFont_maxFontSize = (float)row["metaDataFont_maxFontSize"];
                metaDataFont_minFontSize = (float)row["metaDataFont_minFontSize"];
                metaDataFont_shadowing = (bool)row["metaDataFont_shadowing"];
                metaDataFont_allowNonAntiAliased = (bool)row["metaDataFont_allowNonAntiAliased"];

                dbgLastWindowLocationPoint = (System.Drawing.Point)row["dbgLastWindowLocationPoint"];
                dbgLastWindowSize = (System.Drawing.Size)row["dbgLastWindowSize"];
            }
            Logging.LogLineIf(fDebugTrace, "SetNonListData_FromPersistedStorage(): entered.");
        }

        private static void InitializeAndSetDefaultAllLists()
        {
            DirectoriesList = new List<KeyValuePair<bool, string>>();
            MetaDataList = new List<KeyValuePair<bool, string>>();
            BlacklistedFilenames = new List<KeyValuePair<DateTime, String>>();
        }

        private static void InitializeAndSetDefault_NonListDataValues()
        {
            ShuffleMode = true;
            ShowMetadata = false;
            UseCheckedFoldersOnly = false;
            UseRecursion = false;
            SlideshowIntervalInSecs = 7;
            // dbgLastWindow stuff is set in ConfigSettings initialization code
        }

        private static string GetConfigBaseFilename()
        {
            // get name of our .exe/.scr, remove the extension, add the target path
            string[] args = Environment.GetCommandLineArgs();  // returns full name of our exe/scr, including path, in item zero

            // get just the filename, without extension, from the path in item zero
            string BaseFilename = System.IO.Path.GetFileNameWithoutExtension(args[0]);

            // now remove any secondary extensions, like ".vshost"
            while (BaseFilename != System.IO.Path.GetFileNameWithoutExtension(BaseFilename))
            {
                BaseFilename = System.IO.Path.GetFileNameWithoutExtension(BaseFilename);
            }

            // add the target path
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), BaseFilename);
        }

        private static string GetConfigSettingsListFileName()
        {
            return GetConfigBaseFilename() + ".directories.cfg";
        }

        private static string GetConfigSettingsDataFileName()
        {
            return GetConfigBaseFilename() + ".settings.cfg";
        }

        private static string GetConfigSettingsBlacklistFileName()
        {
            return GetConfigBaseFilename() + ".ignorefiles.cfg";
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
    }
}
