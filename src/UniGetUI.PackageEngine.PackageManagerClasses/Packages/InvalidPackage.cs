using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class InvalidPackage : IPackage, INotifyPropertyChanged
    {
        public IPackageDetails Details => throw new NotImplementedException();

        public PackageTag Tag { get => PackageTag.Unavailable; set { } }

        private bool is_checked = false;
        public bool IsChecked {
            get => is_checked;
            set { is_checked = value; }
        }

        private long __hash;
        private long __extended_hash;

        public string Name { get; }

        public string Id { get; }

        public string Version { get; }

        public double VersionAsFloat { get; }

        public double NewVersionAsFloat { get => .0F; }

        public IManagerSource Source { get; }

        public IPackageManager Manager => throw new NotImplementedException();

        public string NewVersion { get => ""; }

        public bool IsUpgradable { get => false; }

        public PackageScope Scope { get => PackageScope.Local; set { } }

        public string SourceAsString { get; }

        public string AutomationName { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public InvalidPackage(SerializableIncompatiblePackage_v1 data)
        {
            Name = data.Name;
            Id = data.Id;
            Version = data.Version;
            VersionAsFloat = CoreTools.GetVersionStringAsFloat(data.Version);
            SourceAsString = data.Source;
            AutomationName = data.Name;
            Source = new NullSource(data.Source);

            __hash = CoreTools.HashStringAsLong(data.Name + data.Id);
            __extended_hash = CoreTools.HashStringAsLong(data.Name + data.Id + data.Version);
        }

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
            return new SerializableIncompatiblePackage_v1()
            { 
                Name = Name,
                Id = Id,
                Version = Version,
                Source = SourceAsString,
            };
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

        public bool IsEquivalentTo(IPackage? other)
        {
            return __hash == (other as IPackage)?.GetHash();
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
    }

    public class NullSource: IManagerSource
    {
        public string IconId { get; }
        public bool IsVirtualManager { get; }
        public IPackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; set; }
        public int? PackageCount { get; }
        public string? UpdateDate { get; }

        public NullSource(string name)
        {
            Name = name;
            Url = new Uri("about:blank");
            IsVirtualManager = true;
            IconId = "question";
            Manager = NullPackageManager.Instance;
        }

        /// <summary>
        /// Returns a human-readable string representing the source name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
