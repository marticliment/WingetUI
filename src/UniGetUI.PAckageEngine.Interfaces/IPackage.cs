﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Interfaces
{
    public interface IPackage : INotifyPropertyChanged
    {
        public IPackageDetails Details { get; }
        public PackageTag Tag { get; set; }
        public bool IsChecked { get; set; }
        public string Name { get; }
        public string Id { get; }
        public string Version { get; }
        public double VersionAsFloat { get; }
        public double NewVersionAsFloat { get; }
        public IManagerSource Source { get; }
        public IPackageManager Manager { get; }
        public string NewVersion { get; }
        public bool IsUpgradable { get; }
        public PackageScope Scope { get; set; }
        public string SourceAsString { get; }
        public string AutomationName { get; }


        /// <summary>
        /// Returns an identifier that can be used to compare different packahe instances that refer to the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        /// For more specific comparsion use GetVersionedHash()
        /// </summary>
        /// <returns></returns>
        public long GetHash();

        /// <summary>
        /// Returns an identifier that can be used to compare different packahe instances that refer to the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        ///    - Package version
        ///    - Package new version (if any)
        /// </summary>
        /// <returns></returns>
        public long GetVersionedHash();

        /// <summary>
        /// Check wether two packages are **REALLY** the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        ///    - Package version
        ///    - Package new version (if any)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(object? other);

        /// <summary>
        /// Check wether two package instances represent the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        /// For more specific comparsion use package.Equals(object? other)
        /// </summary>
        /// <param name="other">A package</param>
        /// <returns>Wether the two instances refer to the same instance</returns>
        public bool IsEquivalentTo(IPackage? other);

        /// <summary>
        /// Load the package's normalized icon id,
        /// </summary>
        /// <returns>a string with the package's normalized icon id</returns>
        public string GetIconId();

        /// <summary>
        /// Get the package's icon url. If the package has no icon, a fallback image is returned.
        /// After calling this method, the returned URL points to a location on the local machine
        /// </summary>
        /// <returns>An always-valid URI object, pointing to a file:// or to a ms-appx:// URL</returns>
        public Task<Uri> GetIconUrl();

        /// <summary>
        /// Retrieves a list og URIs representing the available screenshots for this package.
        /// </summary>
        /// <returns></returns>
        public Task<Uri[]> GetPackageScreenshots();


        /// <summary>
        /// Adds the package to the ignored updates list. If no version is provided, all updates are ignored.
        /// Calling this method will override older ignored updates.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public Task AddToIgnoredUpdatesAsync(string version = "*");

        /// <summary>
        /// Removes the package from the ignored updates list, either if it is ignored for all updates or for a specific version only.
        /// </summary>
        /// <returns></returns>
        public Task RemoveFromIgnoredUpdatesAsync();

        /// <summary>
        /// Returns true if the package's updates are ignored. If the version parameter
        /// is passed it will be checked if that version is ignored. Please note that if 
        /// all updates are ignored, calling this method with a specific version will 
        /// still return true, although the passed version is not explicitly ignored. 
        /// </summary>
        /// <param name="Version"></param>
        /// <returns></returns>
        public Task<bool> HasUpdatesIgnoredAsync(string Version = "*");

        /// <summary>
        /// Returns (as a string) the version for which a package has been ignored. When no versions 
        /// are ignored, an empty string will be returned; and when all versions are ignored an asterisk
        /// will be returned.
        /// </summary>
        /// <returns></returns>
        public Task<string> GetIgnoredUpdatesVersionAsync();

        /// <summary>
        /// Returns the corresponding installed Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public IPackage? GetInstalledPackage();

        /// <summary>
        /// Returns the corresponding available Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public IPackage? GetAvailablePackage();

        /// <summary>
        /// Returns the corresponding upgradable Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public IPackage? GetUpgradablePackage();

        /// <summary>
        /// Sets the package tag. You may as well use the Tag property.
        /// This function is used for compatibility with the ? operator
        /// </summary>
        /// <param name="tag"></param>
        public void SetTag(PackageTag tag);

        /// <summary>
        /// Checks wether a new version of this package is installed
        /// </summary>
        /// <returns></returns>
        public bool NewerVersionIsInstalled();

    }
}