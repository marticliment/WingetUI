using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Search;
using System.Net.Http;

namespace UniGetUI.Core.Data
{
    public static class CoreData
    {
        public static string VersionName =  "3.1.0-beta"; // Do not modify this line, use file scripts/apply_versions.py
        public static double VersionNumber =  3.099; // Do not modify this line, use file scripts/apply_versions.py

        private static string __ignored_updates_database_file = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui", "IgnoredPackageUpdates.json");
        public static string IgnoredUpdatesDatabaseFile
        {
            get
            {
                if (!Directory.Exists(Path.GetDirectoryName(__ignored_updates_database_file)))
                    Directory.CreateDirectory(Path.GetDirectoryName(__ignored_updates_database_file));
                if (!File.Exists(__ignored_updates_database_file))
                    File.WriteAllText(__ignored_updates_database_file, "{}");
                return __ignored_updates_database_file;
            }
        }
        public static bool IsDaemon = false;

        public static string WingetUILog = "";
        public static string ManagerLogs = "";


        private static int __volatile_notification_id_counter = 1235;
        public static int VolatileNotificationIdCounter { get { return __volatile_notification_id_counter++; } }
        public static int UpdatesAvailableNotificationId = 1234;

        public static string WingetUIExecutableDirectory = Directory.GetParent(Environment.ProcessPath).FullName;
        public static string WingetUIExecutableFile = Environment.ProcessPath;
        public static string GSudoPath = Path.Join(WingetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");

        public static string WingetUIDataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui");
        public static string WingetUIInstallationOptionsDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui", "InstallationOptions");
        public static string WingetUICacheDirectory_Data = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedData");
        public static string WingetUICacheDirectory_Icons = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedIcons");
        public static string WingetUICacheDirectory_Lang = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedLangFiles");

        public static string WingetUI_DefaultBackupDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WingetUI");

        public static Dictionary<string, IconScreenshotDatabase_v2.PackageIconAndScreenshots> IconDatabaseData = new();

        public class IconScreenshotDatabase_v2
        {
            public class PackageCount
            {
                public int total { get; set; }
                public int done { get; set; }
                public int packages_with_icon { get; set; }
                public int packages_with_screenshot { get; set; }
                public int total_screenshots { get; set; }
            }
            public class PackageIconAndScreenshots
            {
                public string icon { get; set; }
                public List<string> images { get; set; }
            }

            public PackageCount package_count { get; set; }
            public Dictionary<string, PackageIconAndScreenshots> icons_and_screenshots { get; set; }
        }

        public static async Task LoadIconAndScreenshotsDatabase()
        {
            string IconsAndScreenshotsFile = Path.Join(WingetUICacheDirectory_Data, "Icon Database.json");

            try
            {
                if (!File.Exists(IconsAndScreenshotsFile))
                    if (!Directory.Exists(Path.GetDirectoryName(IconsAndScreenshotsFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(IconsAndScreenshotsFile));

                Uri DownloadUrl = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/WebBasedData/screenshot-database-v2.json");
                if (AppTools.GetSettings_Static("IconDataBaseURL"))
                    DownloadUrl = new Uri(AppTools.GetSettingsValue_Static("IconDataBaseURL"));

                using (HttpClient client = new())
                {
                    string fileContents = await client.GetStringAsync(DownloadUrl);
                    await File.WriteAllTextAsync(IconsAndScreenshotsFile, fileContents);
                }

                AppTools.Log("Downloaded icons and screenshots successfully!");

            }
            catch (Exception e)
            {
                AppTools.Log("Failed to download icons and screenshots");
                AppTools.Log(e);
            }


            if (!File.Exists(IconsAndScreenshotsFile))
            {
                AppTools.Log("WARNING: Icon Database file not found");
                return;
            }

            try
            {
                IconScreenshotDatabase_v2 JsonData = JsonSerializer.Deserialize<IconScreenshotDatabase_v2>(await File.ReadAllTextAsync(IconsAndScreenshotsFile));
                if (JsonData.icons_and_screenshots != null)
                    IconDatabaseData = JsonData.icons_and_screenshots;
            }
            catch (Exception ex)
            {
                AppTools.Log("Failed to load icon database");
                AppTools.Log(ex);
            }
        }
    }
}
