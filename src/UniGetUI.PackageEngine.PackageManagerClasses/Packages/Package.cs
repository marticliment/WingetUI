using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class Package : IPackage
    {
        // Internal properties
        private bool __is_checked = false;
        public event PropertyChangedEventHandler? PropertyChanged;
        private PackageTag __tag;

        private readonly long __hash;
        private readonly long __versioned_hash;

        private PackageDetails? __details = null;
        public IPackageDetails Details
        {
            get => __details ??= new PackageDetails(this);
        }

        public PackageTag Tag
        {
            get => __tag;
            set
            {
                __tag = value;
                OnPropertyChanged(nameof(Tag));
            }
        }
        public bool IsChecked
        {
            get { return __is_checked; }
            set { __is_checked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public string Name { get; }
        public string Id { get; }
        public string Version { get; }
        public double VersionAsFloat { get; }
        public double NewVersionAsFloat { get; }
        public IManagerSource Source { get; }
        public IPackageManager Manager { get; }
        public string NewVersion { get; }
        public virtual bool IsUpgradable { get; }
        public PackageScope Scope { get; set; }
        public string SourceAsString { get; }
        public string AutomationName { get; }

        /// <summary>
        /// Constuct a package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="version"></param>
        /// <param name="source"></param>
        /// <param name="manager"></param>
        /// <param name="scope"></param>
        public Package(string name, string id, string version, IManagerSource source, IPackageManager manager, PackageScope scope = PackageScope.Local)
        {
            Name = name;
            Id = id;
            Version = version;
            VersionAsFloat = CoreTools.GetVersionStringAsFloat(version);
            Source = source;
            Manager = manager;
            Scope = scope;
            NewVersion = "";
            Tag = PackageTag.Default;
            SourceAsString = source.ToString();
            AutomationName = CoreTools.Translate("Package {name} from {manager}", new Dictionary<string, object?> { { "name", Name }, { "manager", SourceAsString } });
            __hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.Name + "\\" + Id);
            __versioned_hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.Name + "\\" + Id + "\\" + Version);
            IsUpgradable = false;
        }

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
        public Package(string name, string id, string installed_version, string new_version, IManagerSource source, IPackageManager manager, PackageScope scope = PackageScope.Local)
            : this(name, id, installed_version, source, manager, scope)
        {
            IsUpgradable = true;
            NewVersion = new_version;
            NewVersionAsFloat = CoreTools.GetVersionStringAsFloat(new_version);

            // Packages in the updates tab are checked by default
            IsChecked = true;
        }

        public long GetHash()
        {
            return __hash;
        }

        public long GetVersionedHash()
        {
            return __versioned_hash;
        }

        public override bool Equals(object? other)
        {
            return __versioned_hash == (other as IPackage)?.GetHash();
        }

        public bool IsEquivalentTo(IPackage? other)
        {
            return __hash == (other as IPackage)?.GetHash();
        }

        public string GetIconId()
        {
            string iconId = Id.ToLower();
            if (Manager.Name == "Winget")
                iconId = string.Join('.', iconId.Split(".")[1..]);
            else if (Manager.Name == "Chocolatey")
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            else if (Manager.Name == "Scoop")
                iconId = iconId.Replace(".app", "");
            return iconId;
        }

        public virtual async Task<Uri> GetIconUrl()
        {
            try
            {
                string iconId = GetIconId();

                CacheableIcon? icon = await Manager.GetPackageIconUrl(this);
                string path = await IconCacheEngine.DownloadIconOrCache(icon, Manager.Name, Id);

                Uri Icon;
                if (path == "")
                    Icon = new Uri("ms-appx:///Assets/Images/package_color.png");
                else
                    Icon = new Uri("file:///" + path);

                Logger.Debug($"Icon for package {Id} was loaded from {Icon}");
                return Icon;
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while retrieving the icon for package {Id}");
                Logger.Error(ex);
                return new Uri("ms-appx:///Assets/Images/package_color.png");
            }
        }

        public virtual async Task<Uri[]> GetPackageScreenshots()
        {
            return await Manager.GetPackageScreenshotsUrl(this);
        }

        public virtual async Task AddToIgnoredUpdatesAsync(string version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    IgnoredUpdatesJson.Remove(IgnoredId);
                IgnoredUpdatesJson.Add(IgnoredId, version);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                GetInstalledPackage()?.SetTag(PackageTag.Pinned);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not add package {Id} to ignored updates");
                Logger.Error(ex);
            }
        }

        public virtual async Task RemoveFromIgnoredUpdatesAsync()
        {
            try
            {

                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                {
                    IgnoredUpdatesJson.Remove(IgnoredId);
                    await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                }

                GetInstalledPackage()?.SetTag(PackageTag.Default);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not remove package {Id} from ignored updates");
                Logger.Error(ex);
            }
        }

        public virtual async Task<bool> HasUpdatesIgnoredAsync(string Version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId) && (IgnoredUpdatesJson[IgnoredId]?.ToString() == "*" || IgnoredUpdatesJson[IgnoredId]?.ToString() == Version))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not check whether package {Id} has updates ignored");
                Logger.Error(ex);
                return false;
            }

        }

        public virtual async Task<string> GetIgnoredUpdatesVersionAsync()
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    return IgnoredUpdatesJson[IgnoredId]?.ToString() ?? "";
                else
                    return "";
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not retrieve the ignored updates version for package {Id}");
                Logger.Error(ex);
                return "";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IPackage? GetInstalledPackage()
        {
            return PackageCacher.GetInstalledPackageOrNull(this);
        }

        public IPackage? GetAvailablePackage()
        {
            return PackageCacher.GetAvailablePackageOrNull(this);
        }

        public IPackage? GetUpgradablePackage()
        {
            return PackageCacher.GetUpgradablePackageOrNull(this);
        }

        public virtual void SetTag(PackageTag tag)
        {
            Tag = tag;
        }

        public virtual bool NewerVersionIsInstalled()
        {
            if (!IsUpgradable) return false;
            return PackageCacher.NewerVersionIsInstalled(this);
        }

        public async Task<SerializablePackage_v1> AsSerializable()
        {
            return new SerializablePackage_v1()
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source.Name,
                ManagerName = Manager.Name,
                InstallationOptions = (await InstallationOptions.FromPackageAsync(this)).AsSerializable(),
                Updates = new SerializableUpdatesOptions_v1()
                {
                    IgnoredVersion = await GetIgnoredUpdatesVersionAsync(),
                    UpdatesIgnored = await HasUpdatesIgnoredAsync(),
                }
            };
        }

        public SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            return new SerializableIncompatiblePackage_v1()
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source.Name,
            };
        }
    }
}

