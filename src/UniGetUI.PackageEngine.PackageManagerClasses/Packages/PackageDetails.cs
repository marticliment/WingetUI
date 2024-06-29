using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    /// <summary>
    /// Holds the details of a Package.
    /// </summary>
    public class PackageDetails: IPackageDetails
    {
        public IPackage Package { get; }
        public bool IsPopulated { get; set; } = false;
        public string? Description { get; set; } = null;
        public string? Publisher { get; set; } = null;
        public string? Author { get; set; } = null;
        public Uri? HomepageUrl { get; set; } = null;
        public string? License { get; set; } = null;
        public Uri? LicenseUrl { get; set; } = null;
        public Uri? InstallerUrl { get; set; } = null;
        public string? InstallerHash { get; set; } = null;
        public string? InstallerType { get; set; } = null;
        public double InstallerSize { get; set; } = 0;
        public Uri? ManifestUrl { get; set; } = null;
        public string? UpdateDate { get; set; } = null;
        public string? ReleaseNotes { get; set; } = null;
        public Uri? ReleaseNotesUrl { get; set; } = null;
        public string[] Tags { get; set; } = [];

        public PackageDetails(IPackage package)
        {
            Package = package;
        }

        public async Task Load()
        {
            try {
                await Package.Manager.GetPackageDetails(this);
                IsPopulated = true;
            } 
            catch (Exception ex)
            {
                Logger.Error($"PackageDetails.Load failed for package {Package.Name}");
                Logger.Error(ex);
            }
        }
    }

}
