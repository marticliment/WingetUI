using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class InstalledPackagesLoader : AbstractPackageLoader
    {
        public InstalledPackagesLoader(IEnumerable<PackageManager> managers)
        : base(managers, "INSTALLED_PACKAGES", AllowMultiplePackageVersions: true)
        {
        }

#pragma warning disable
        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            return true;
        }
#pragma warning restore

        protected override Task<IPackage[]> LoadPackagesFromManager(IPackageManager manager)
        {
            return manager.GetInstalledPackages();
        }

        protected override async Task WhenAddingPackage(IPackage package)
        {
            if (await package.HasUpdatesIgnoredAsync(Version: "*"))
                package.Tag = PackageTag.Pinned;
            else if (package.GetUpgradablePackage() != null)
                package.Tag = PackageTag.IsUpgradable;

            package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);
        }
    }
}
