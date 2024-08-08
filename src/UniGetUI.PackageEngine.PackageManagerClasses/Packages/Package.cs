using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class Package : IPackage
    {
        // Internal properties
        private bool __is_checked;
        public event PropertyChangedEventHandler? PropertyChanged;
        private PackageTag __tag;

        private readonly long __hash;
        private readonly long __versioned_hash;

        private IPackageDetails? __details;
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

        private OverridenInstallationOptions _overriden_options;
        public ref OverridenInstallationOptions OverridenOptions { get => ref _overriden_options; }
        public string Name { get; }
        public string AutomationName { get; }
        public string Id { get; }
        public string Version { get; }
        public double VersionAsFloat { get; }
        public double NewVersionAsFloat { get; }
        public bool IsPopulated { get; set; }
        public IManagerSource Source { get; }

        /// <summary>
        /// IPackageManager is guaranteed to be PackageManager, but C# doesn't allow covariant attributes
        /// </summary>
        public IPackageManager Manager { get; }
        public string NewVersion { get; }
        public virtual bool IsUpgradable { get; }

        /// <summary>
        /// Construct a package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        public Package(
            string name,
            string id,
            string version,
            IManagerSource source,
            IPackageManager manager,
            OverridenInstallationOptions? options = null)
        {
            Name = name;
            Id = id;
            Version = version;
            VersionAsFloat = CoreTools.GetVersionStringAsFloat(version);
            Source = source;
            Manager = manager;

            if (options != null)
            {
                _overriden_options = (OverridenInstallationOptions)options;
            }

            NewVersion = "";
            Tag = PackageTag.Default;
            AutomationName = CoreTools.Translate("Package {name} from {manager}",
                new Dictionary<string, object?> { { "name", Name }, { "manager", Source.AsString_DisplayName } });

            __hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.Name + "\\" + Id);
            __versioned_hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.Name + "\\" + Id + "\\" + Version);
            IsUpgradable = false;
        }

        /// <summary>
        /// Creates an UpgradablePackage object representing a package that can be upgraded; given its name, id, installed version, new version, source and manager, and an optional scope.
        /// </summary>
        public Package(
            string name,
            string id,
            string installed_version,
            string new_version,
            IManagerSource source,
            IPackageManager manager,
            OverridenInstallationOptions? options = null)
            : this(name, id, installed_version, source, manager, options)
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

        public bool Equals(IPackage? other)
        {
            return __versioned_hash == other?.GetHash();
        }

        public override int GetHashCode()
        {
            return (int)__versioned_hash;
        }

        /// <summary>
        /// Check whether two package instances represent the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        /// For more specific comparison use package.Equals(object? other)
        /// </summary>
        /// <param name="other">A package</param>
        /// <returns>Whether the two instances refer to the same instance</returns>
        public bool IsEquivalentTo(IPackage? other)
        {
            return __hash == other?.GetHash();
        }

        public string GetIconId()
        {
            string iconId = Id.ToLower();
            if (Manager.Name == "Winget")
            {
                iconId = string.Join('.', iconId.Split(".")[1..]);
            }
            else if (Manager.Name == "Chocolatey")
            {
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            }
            else if (Manager.Name == "Scoop")
            {
                iconId = iconId.Replace(".app", "");
            }

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
                {
                    Icon = new Uri("ms-appx:///Assets/Images/package_color.png");
                }
                else
                {
                    Icon = new Uri("file:///" + path);
                }

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

                if (JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) is not JsonObject IgnoredUpdatesJson)
                {
                    throw new InvalidOperationException("The IgnoredUpdates database seems to be invalid!");
                }

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                {
                    IgnoredUpdatesJson.Remove(IgnoredId);
                }

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

                if (JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) is not JsonObject IgnoredUpdatesJson)
                {
                    throw new InvalidOperationException("The IgnoredUpdates database seems to be invalid!");
                }

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

        /// <summary>
        /// Returns true if the package's updates are ignored. If the version parameter
        /// is passed it will be checked if that version is ignored. Please note that if
        /// all updates are ignored, calling this method with a specific version will
        /// still return true, although the passed version is not explicitly ignored.
        /// </summary>
        public async Task<bool> HasUpdatesIgnoredAsync(string Version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";

                if (JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) is not JsonObject IgnoredUpdatesJson)
                {
                    throw new InvalidOperationException("The IgnoredUpdates database seems to be invalid!");
                }

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId) && (IgnoredUpdatesJson[IgnoredId]?.ToString() == "*" || IgnoredUpdatesJson[IgnoredId]?.ToString() == Version))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not check whether package {Id} has updates ignored");
                Logger.Error(ex);
                return false;
            }

        }

        /// <summary>
        /// Returns (as a string) the version for which a package has been ignored. When no versions
        /// are ignored, an empty string will be returned; and when all versions are ignored an asterisk
        /// will be returned.
        /// </summary>
        public async Task<string> GetIgnoredUpdatesVersionAsync()
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";

                if (JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) is not JsonObject IgnoredUpdatesJson)
                {
                    throw new InvalidOperationException("The IgnoredUpdates database seems to be invalid!");
                }

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                {
                    return IgnoredUpdatesJson[IgnoredId]?.ToString() ?? "";
                }

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
            if (!IsUpgradable)
            {
                return false;
            }

            return PackageCacher.NewerVersionIsInstalled(this);
        }

        public async Task<SerializablePackage_v1> AsSerializable()
        {
            return new SerializablePackage_v1
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source.Name,
                ManagerName = Manager.Name,
                InstallationOptions = (await InstallationOptions.FromPackageAsync(this)).AsSerializable(),
                Updates = new SerializableUpdatesOptions_v1
                {
                    IgnoredVersion = await GetIgnoredUpdatesVersionAsync(),
                    UpdatesIgnored = await HasUpdatesIgnoredAsync(),
                }
            };
        }

        public SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            return new SerializableIncompatiblePackage_v1
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source.Name,
            };
        }
    }
}

