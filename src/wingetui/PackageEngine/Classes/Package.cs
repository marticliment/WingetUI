using UniGetUI.Core.Data;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Bluetooth.Advertisement;

namespace UniGetUI.PackageEngine.Classes
{
    /// <summary>
    /// Represents the scope of a package. To be coherent with package manager naming, the values are repeated.
    /// </summary>
    public enum PackageScope
    {
        // Repeated entries for coherence with Package Managers
        Global = 1,
        Machine = 1,
        Local = 0,
        User = 0,
    }

    public enum PackageTag
    {
        Default,
        AlreadyInstalled,
        IsUpgradable,
        Pinned,
        OnQueue,
        BeingProcessed,
        Failed
    }

    /// <summary>
    /// This class represents a installable package or a package that is already installed.
    /// </summary>
    public class Package : INotifyPropertyChanged
    {
        // Internal properties
        public AppTools Tools = AppTools.Instance;
        private bool __is_checked = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private string __listed_icon_id;
        private string __name_tooltip;
        private PackageTag __tag;
        private float __opacity;
        private bool __show_icon_highlight;
        private string __hash;

        public PackageTag Tag
        {
            get { return __tag; }

            set
            {
                __tag = value;
                switch(__tag)
                {
                    case PackageTag.Default:
                        ListedIconId = "install";
                        ListIconShowHighlight = false;
                        ListedOpacity = 1;
                        ListedNameTooltip = Name;
                        break;

                    case PackageTag.AlreadyInstalled:
                        ListedIconId = "installed";
                        ListIconShowHighlight = true;
                        ListedOpacity = 1;
                        ListedNameTooltip = Tools.Translate("This package is already installed") + " - " + Name;
                        break;

                    case PackageTag.IsUpgradable:
                        ListedIconId = "update";
                        ListIconShowHighlight = true;
                        ListedOpacity = 1;
                        ListedNameTooltip = Tools.Translate("This package can be updated") + " - " + Name;
                        break;

                    case PackageTag.Pinned:
                        ListedIconId = "pin_fill";
                        ListIconShowHighlight = false;
                        ListedOpacity = 1;
                        ListedNameTooltip = Tools.Translate("Updates for this package are ignored") + " - " + Name;
                        break;

                    case PackageTag.OnQueue:
                        ListedIconId = "sandclock";
                        ListIconShowHighlight = false;
                        ListedOpacity = .5F;
                        ListedNameTooltip = Tools.Translate("This package is on the queue") + " - " + Name;
                        break;

                    case PackageTag.BeingProcessed:
                        ListedIconId = "gears";
                        ListIconShowHighlight = false;
                        ListedOpacity = .5F;
                        ListedNameTooltip = Tools.Translate("This package is being processed") + " - " + Name;
                        break;

                    case PackageTag.Failed:
                        ListedIconId = "stop";
                        ListIconShowHighlight = true;
                        ListedOpacity = 1;
                        ListedNameTooltip = Tools.Translate("An error occurred while processing this package") + " - " + Name;
                        break;
                }
            }
        }

        // Public properties
        public bool ListIconShowHighlight
        {
            get { return __show_icon_highlight; }
            set { __show_icon_highlight = value; OnPropertyChanged(); }
        }

        public bool IsChecked
        {
            get { return __is_checked; }
            set { __is_checked = value; OnPropertyChanged(); }
        }

        public string ListedIconId
        {
            set { __listed_icon_id = value; OnPropertyChanged(); }
            get { return __listed_icon_id; }
        }

        public string ListedNameTooltip
        {
            get { return __name_tooltip; }
            set { __name_tooltip = value; OnPropertyChanged(); }
        }

        public float ListedOpacity
        {
            get { return __opacity; }
            set { __opacity = value; OnPropertyChanged(); }
        }

        public string IsCheckedAsString { get { return IsChecked ? "True" : "False"; } }
        public string Name { get; }
        public string Id { get; set; }
        public string Version { get; }
        public float VersionAsFloat { get; }
        public ManagerSource Source { get; set; }
        public PackageManager Manager { get; }
        public string UniqueId { get; }
        public string NewVersion { get; set;  }
        public virtual bool IsUpgradable { get; } = false;
        public PackageScope Scope { get; set; }
        public string SourceAsString
        {
            get
            {
                if (Source != null) return Source.ToString();
                else return "";
            }
        }

        /// <summary>
        /// Constuct a package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="version"></param>
        /// <param name="source"></param>
        /// <param name="manager"></param>
        /// <param name="scope"></param>
        public Package(string name, string id, string version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local)
        {
            Name = name;
            Id = id;
            Version = version;
            Source = source;
            Manager = manager;
            Scope = scope;
            UniqueId = $"{Manager.Properties.Name}\\{Id}\\{Version}";
            NewVersion = "";
            VersionAsFloat = GetFloatVersion();
            Tag = PackageTag.Default;
            __hash = Manager.Name + "\\" + Source.Name + "\\" + Id;
        }

        public string GetHash()
        {
            return __hash ;
        }


        /// <summary>
        /// Internal method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Package))
                return false;
            else
                return (obj as Package).GetHash() == GetHash();
        }

        /// <summary>
        /// Load the package's normalized icon id,
        /// </summary>
        /// <returns>a string with the package's normalized icon id</returns>
        public string GetIconId()
        {
            string iconId = Id.ToLower();
            if (Manager == Tools.App.Winget)
                iconId = string.Join('.', iconId.Split(".")[1..]);
            else if (Manager == Tools.App.Choco)
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            else if (Manager == Tools.App.Scoop)
                iconId = iconId.Replace(".app", "");
            return iconId;
        }

        /// <summary>
        /// Get the package's icon url. If the package has no icon, a fallback image is returned.
        /// </summary>
        /// <returns>An always-valid URI object</returns>
        public Uri GetIconUrl()
        {
            string iconId = GetIconId();
            if (CoreData.IconDatabaseData.ContainsKey(iconId))
                if (CoreData.IconDatabaseData[iconId].icon != "")
                    return new Uri(CoreData.IconDatabaseData[iconId].icon);

            return new Uri("ms-appx:///Assets/Images/package_color.png");
        }

        /// <summary>
        /// Returns a float representation of the package's version for comparison purposes.
        /// </summary>
        /// <returns>A float value. Returns 0.0F if the version could not be parsed</returns>
        public float GetFloatVersion()
        {
            string _ver = "";
            bool _dotAdded = false;
            foreach (char _char in Version)
            {
                if (char.IsDigit(_char))
                    _ver += _char;
                else if (_char == '.')
                {
                    if (!_dotAdded)
                    {
                        _ver += _char;
                        _dotAdded = true;
                    }
                }
            }
            float res = 0.0F;
            if (_ver != "" && _ver != ".")
                try
                {
                    return float.Parse(_ver);
                }
                catch { }
            return res;
        }

        /// <summary>
        /// Adds the package to the ignored updates list. If no version is provided, all updates are ignored.
        /// Calling this method will override older ignored updates.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task AddToIgnoredUpdatesAsync(string version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    IgnoredUpdatesJson.Remove(IgnoredId);
                IgnoredUpdatesJson.Add(IgnoredId, version);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                Tools.App.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(this);

                GetInstalledPackage()?.SetTag(PackageTag.Pinned);
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Removes the package from the ignored updates list, either if it is ignored for all updates or for a specific version only.
        /// </summary>
        /// <returns></returns>
        public async Task RemoveFromIgnoredUpdatesAsync()
        {
            try
            {

                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                {
                    IgnoredUpdatesJson.Remove(IgnoredId);
                    await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                }

                GetInstalledPackage()?.SetTag(PackageTag.Default);
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
}

        /// <summary>
        /// Returns true if the package's updates are ignored. If the version parameter
        /// is passed it will be checked if that version is ignored. Please note that if 
        /// all updates are ignored, calling this method with a specific version will 
        /// still return true, although the passed version is not explicitly ignored. 
        /// </summary>
        /// <param name="Version"></param>
        /// <returns></returns>
        public async Task<bool> HasUpdatesIgnoredAsync(string Version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId) && (IgnoredUpdatesJson[IgnoredId].ToString() == "*" || IgnoredUpdatesJson[IgnoredId].ToString() == Version))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
                return false;
            }
            
        }

        /// <summary>
        /// Returns (as a string) the version for which a package has been ignored. When no versions 
        /// are ignored, an empty string will be returned; and when all versions are ignored an asterisk
        /// will be returned.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetIgnoredUpdatesVersionAsync()
        {
            try 
            { 
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    return IgnoredUpdatesJson[IgnoredId].ToString();
                else
                    return "";
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
                return "";
            }
        }

        /// <summary>
        /// Internal method to raise the PropertyChanged event.
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Returns the corresponding installed Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetInstalledPackage()
        {
            foreach (var package in Tools.App.MainWindow.NavigationPage.InstalledPage.Packages)
                if (package.Equals(this))
                    return package;
            return null;
        }

        /// <summary>
        /// Returns the corresponding available Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetAvailablePackage()
        {
            foreach (var package in Tools.App.MainWindow.NavigationPage.DiscoverPage.Packages)
                if (package.Equals(this))
                    return package;
            return null;
        }

        /// <summary>
        /// Returns the corresponding upgradable Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetUpgradablePackage()
        {
            foreach (var package in Tools.App.MainWindow.NavigationPage.UpdatesPage.Packages)
                if (package.Equals(this))
                    return package;
            return null;
        }

        /// <summary>
        /// Sets the package tag. You may as well use the Tag property.
        /// This function is used for compatibility with the ? operator
        /// </summary>
        /// <param name="tag"></param>
        public void SetTag(PackageTag tag)
        {
            Tag = tag;
        }

    }

    public class UpgradablePackage : Package
    {
        // Public properties
        public float NewVersionAsFloat { get; }
        public override bool IsUpgradable { get; } = true;

        private string __hash;

        /// <summary>
        /// Creates an UpgradablePackage object representing a package that can be upgraded; given its name, id, installed version, new version, source and manager, and an optional scope.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="installed_version"></param>
        /// <param name="new_version"></param>
        /// <param name="source"></param>
        /// <param name="manager"></param>
        /// <param name="scope"></param>
        public UpgradablePackage(string name, string id, string installed_version, string new_version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local) : base(name, id, installed_version, source, manager, scope)
        {
            NewVersion = new_version;
            IsChecked = true;
            NewVersionAsFloat = GetFloatNewVersion();

            __hash = Manager.Name + "\\" + Source.Name + "\\" + Id + "\\" + Version + "->" + NewVersion;
        }
        public new string GetHash()
        {
            return __hash;
        }

        /// <summary>
        /// Returns a float value representing the new new version of the package, for comparison purposes.
        /// </summary>
        /// <returns></returns>
        public float GetFloatNewVersion()
        {
            string _ver = "";
            bool _dotAdded = false;
            foreach (char _char in NewVersion)
            {
                if (char.IsDigit(_char))
                    _ver += _char;
                else if (_char == '.')
                {
                    if (!_dotAdded)
                    {
                        _ver += _char;
                        _dotAdded = true;
                    }
                }
            }
            float res = 0.0F;
            if (_ver != "" && _ver != ".")
                try
                {
                    return float.Parse(_ver);
                }
                catch (Exception)
                {
                }
            return res;
        }

        /// <summary>
        /// This version will check if the new version of the package is already present 
        /// on the InstalledPackages list, to prevent already installed updates from being updated again.
        /// </summary>
        /// <returns></returns>
        public bool NewVersionIsInstalled()
        {
            foreach (Package package in Tools.App.MainWindow.NavigationPage.InstalledPage.Packages)
                if (package.Manager == Manager && package.Id == Id && package.Version == NewVersion && package.Source.Name == Source.Name)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// The properties of a given package.
    /// </summary>
    public class PackageDetails
    {
        public Package Package { get; }
        public string Name { get; }
        public string Id { get; }
        public string Version { get; }
        public string NewVersion { get; }
        public ManagerSource Source { get; }
        public string Description { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string Author { get; set; } = "";
        public Uri HomepageUrl { get; set; } = null;
        public string License { get; set; } = "";
        public Uri LicenseUrl { get; set; } = null;
        public Uri InstallerUrl { get; set; } = null;
        public string InstallerHash { get; set; } = "";
        public string InstallerType { get; set; } = "";
        public double InstallerSize { get; set; } = 0; // In Megabytes
        public Uri ManifestUrl { get; set; } = null;
        public string UpdateDate { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public Uri ReleaseNotesUrl { get; set; } = null;
        public string[] Tags { get; set; } = new string[0];

        /// <summary>
        /// Construct a PackageDetails object from a given package. The constructor does 
        /// NOT load the package's details. They must be loaded manually
        /// </summary>
        /// <param name="package"></param>
        public PackageDetails(Package package)
        {
            Package = package;
            Name = package.Name;
            Id = package.Id;
            Version = package.Version;
            Source = package.Source;
            if (package is UpgradablePackage)
                NewVersion = ((UpgradablePackage)package).NewVersion;
            else
                NewVersion = "";
        }
    }

    /// <summary>
    /// This class represents the options in which a package must be installed, updated or uninstalled.
    /// </summary>
    public class InstallationOptions
    {
        public bool SkipHashCheck { get; set; } = false;
        public bool InteractiveInstallation { get; set; } = false;
        public bool RunAsAdministrator { get; set; } = false;
        public string Version { get; set; } = "";
        public Architecture? Architecture { get; set; } = null;
        public PackageScope? InstallationScope { get; set; } = null;
        public List<string> CustomParameters { get; set; } = new List<string>();
        public bool RemoveDataOnUninstall { get; set; } = false;
        public bool PreRelease { get; set; } = false;
        public string CustomInstallLocation { get; set; } = "";

        public Package Package { get; }

        private string _saveFileName = "Unknown.Unknown.InstallationOptions";

        /// <summary>
        /// Construct a new InstallationOptions object for a given package. The options will be 
        /// loaded from disk unless the reset parameter is set to true, in which case the options
        /// will be the default ones.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="reset"></param>
        public InstallationOptions(Package package, bool reset = false)
        {
            Package = package;
            _saveFileName = Package.Manager.Name.Replace(" ", "").Replace(".", "") + "." + Package.Id;
            if (!reset)
            {
                LoadOptionsFromDisk();
            }
        }

        /// <summary>
        /// Returns a new InstallationOptions object from a given package. The options will be
        /// loaded from the disk asynchronously.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static async Task<InstallationOptions> FromPackageAsync(Package package)
        {
            var options = new InstallationOptions(package, reset: true);
            await options.LoadOptionsFromDiskAsync();
            return options;
        }

        /// <summary>
        /// Overload of the constructor that accepts an UpgradablePackage object.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="reset"></param>
        public InstallationOptions(UpgradablePackage package, bool reset = false) : this((Package)package, reset)
        { }

        /// <summary>
        /// Returns a new InstallationOptions object from a given SerializableInstallationOptions_v1 and a package.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public static InstallationOptions FromSerialized(SerializableInstallationOptions_v1 options, Package package)
        {
            InstallationOptions opt = new(package, reset: true);
            opt.FromSerialized(options);
            return opt;
        }

        /// <summary>
        /// Loads and applies the options from the given SerializableInstallationOptions_v1 object to the current object.
        /// </summary>
        /// <param name="options"></param>
        public void FromSerialized(SerializableInstallationOptions_v1 options)
        {
            SkipHashCheck = options.SkipHashCheck;
            InteractiveInstallation = options.InteractiveInstallation;
            RunAsAdministrator = options.RunAsAdministrator;
            CustomInstallLocation = options.CustomInstallLocation;
            Version = options.Version;
            PreRelease = options.PreRelease;
            if (options.Architecture != "" && CommonTranslations.InvertedArchNames.ContainsKey(options.Architecture))
                Architecture = CommonTranslations.InvertedArchNames[options.Architecture];
            if (options.InstallationScope != "" && CommonTranslations.InvertedScopeNames_NonLang.ContainsKey(options.InstallationScope))
                InstallationScope = CommonTranslations.InvertedScopeNames_NonLang[options.InstallationScope];
            CustomParameters = options.CustomParameters;
        }

        /// <summary>
        /// Returns a SerializableInstallationOptions_v1 object containing the options of the current instance.
        /// </summary>
        /// <returns></returns>
        public SerializableInstallationOptions_v1 Serialized()
        {
            SerializableInstallationOptions_v1 options = new();
            options.SkipHashCheck = SkipHashCheck;
            options.InteractiveInstallation = InteractiveInstallation;
            options.RunAsAdministrator = RunAsAdministrator;
            options.CustomInstallLocation = CustomInstallLocation;
            options.PreRelease = PreRelease;
            options.Version = Version;
            if (Architecture != null)
                options.Architecture = CommonTranslations.ArchNames[Architecture.Value];
            if (InstallationScope != null)
                options.InstallationScope = CommonTranslations.ScopeNames_NonLang[InstallationScope.Value];
            options.CustomParameters = CustomParameters;
            return options;
        }

        private FileInfo GetPackageOptionsFile()
        {
            var optionsFileName = Package.Manager.Name + "." + Package.Id + ".json";
            return new FileInfo(Path.Join(CoreData.WingetUIInstallationOptionsDirectory, optionsFileName));
        }

        /// <summary>
        /// Saves the current options to disk.
        /// </summary>
        public void SaveOptionsToDisk()
        {
            try
            {
                var optionsFile = GetPackageOptionsFile();
                if (optionsFile.Directory?.Exists == false)
                    optionsFile.Directory.Create();
                
                using var outputStream = optionsFile.OpenWrite();
                JsonSerializer.Serialize(outputStream, Serialized());
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Saves the current options to disk, asynchronously.
        /// </summary>
        public async Task SaveOptionsToDiskAsync()
        {
            try
            {
                var optionsFile = GetPackageOptionsFile();
                if (optionsFile.Directory?.Exists == false)
                    optionsFile.Directory.Create();

                await using var outputStream = optionsFile.OpenWrite();
                await JsonSerializer.SerializeAsync(outputStream, Serialized());
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Loads the options from disk, asynchronously.
        /// </summary>
        public void LoadOptionsFromDisk()
        {
            try
            {
                var optionsFile = GetPackageOptionsFile();
                if (!optionsFile.Exists)
                    return;

                using var inputStream = optionsFile.OpenRead();
                var options = JsonSerializer.Deserialize<SerializableInstallationOptions_v1>(inputStream);
                FromSerialized(options);
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }
        }

        /// <summary>
        /// Loads the options from disk.
        /// </summary>
        public async Task LoadOptionsFromDiskAsync()
        {
            try
            {
                var optionsFile = GetPackageOptionsFile();
                if (!optionsFile.Exists)
                    return;
                
                await using var inputStream = optionsFile.OpenRead();
                var options = await JsonSerializer.DeserializeAsync<SerializableInstallationOptions_v1>(inputStream);
                FromSerialized(options);
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }
        }

        /// <summary>
        /// Returns a string representation of the current options.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string customparams = CustomParameters != null ? string.Join(",", CustomParameters) : "[]";
            return $"<InstallationOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"InstallationScope={CustomInstallLocation};" +
                   $"CustomParameters={customparams};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall}>";
        }
    }
}
