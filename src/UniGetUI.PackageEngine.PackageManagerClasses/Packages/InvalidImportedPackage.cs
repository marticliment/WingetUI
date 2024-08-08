using System.ComponentModel;
using System.Runtime.CompilerServices;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class InvalidImportedPackage : IPackage, INotifyPropertyChanged
    {
        public IPackageDetails Details { get; }

        public PackageTag Tag { get => PackageTag.Unavailable; set { } }

        private bool __is_checked;
        public bool IsChecked
        {
            get { return __is_checked; }
            set { __is_checked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        private long __hash;
        private long __extended_hash;

        private static OverridenInstallationOptions __overriden_options;
        public ref OverridenInstallationOptions OverridenOptions { get => ref __overriden_options; }

        public string Name { get; }

        public string Id { get; }

        public string Version { get; }

        public double VersionAsFloat { get; }

        public double NewVersionAsFloat { get => .0F; }

        public IManagerSource Source { get; }

        public IPackageManager Manager { get; }

        public string NewVersion { get => ""; }

        public bool IsUpgradable { get => false; }

        public PackageScope Scope { get => PackageScope.Local; set { } }

        public string SourceAsString { get; }

        public string AutomationName { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public InvalidImportedPackage(SerializableIncompatiblePackage_v1 data, IManagerSource source)
        {
            Name = data.Name;
            Id = data.Id.Split('\\')[^1];
            Version = data.Version;
            VersionAsFloat = CoreTools.GetVersionStringAsFloat(data.Version);
            SourceAsString = data.Source;
            AutomationName = data.Name;
            Manager = source.Manager;
            Source = source;
            Details = new PackageDetails(this);

            __hash = CoreTools.HashStringAsLong(data.Name + data.Id);
            __extended_hash = CoreTools.HashStringAsLong(data.Name + data.Id + data.Version);
        }
#pragma warning disable CS1998
        public async Task AddToIgnoredUpdatesAsync(string version = "*")
        {
            return;
        }

        public Task<SerializablePackage_v1> AsSerializable()
        {
            throw new NotImplementedException();
        }

        public SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            return new SerializableIncompatiblePackage_v1
            {
                Name = Name,
                Id = Id,
                Version = Version,
                Source = SourceAsString,
            };
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IPackage? GetAvailablePackage()
        {
            return null;
        }

        public long GetHash()
        {
            return __hash;
        }

        public string GetIconId()
        {
            throw new NotImplementedException();
        }

        public async Task<Uri> GetIconUrl()
        {
            return new Uri("ms-appx:///Assets/Images/package_color.png");
        }

        public async Task<string> GetIgnoredUpdatesVersionAsync()
        {
            return "";
        }

        public IPackage? GetInstalledPackage()
        {
            return null;
        }

        public async Task<Uri[]> GetPackageScreenshots()
        {
            return [];
        }

        public IPackage? GetUpgradablePackage()
        {
            return null;
        }

        public long GetVersionedHash()
        {
            return __extended_hash;
        }

        public async Task<bool> HasUpdatesIgnoredAsync(string Version = "*")
        {
            return false;
        }

        public bool Equals(IPackage? other)
        {
            return __hash == other?.GetVersionedHash();
        }

        public bool IsEquivalentTo(IPackage? other)
        {
            return __hash == other?.GetHash();
        }

        public bool NewerVersionIsInstalled()
        {
            return false;
        }

        public async Task RemoveFromIgnoredUpdatesAsync()
        {
            return;
        }

        public void SetTag(PackageTag tag)
        {
            return;
        }

#pragma warning restore CS1998
    }

    public class NullSource : IManagerSource
    {
        public static NullSource Instance = new(CoreTools.Translate("Unknown"));
        public IconType IconId { get; }
        public bool IsVirtualManager { get; }
        public IPackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; set; }
        public int? PackageCount { get; }
        public string? UpdateDate { get; }

        public string AsString { get => Name; }
        public string AsString_DisplayName { get => Name; }

        public NullSource(string name)
        {
            Name = name;
            Url = new Uri("about:blank");
            IsVirtualManager = true;
            IconId = IconType.Help;
            Manager = (IPackageManager)NullPackageManager.Instance;
        }

        /// <summary>
        /// Returns a human-readable string representing the source name
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        public void RefreshSourceNames()
        { }

    }
}
