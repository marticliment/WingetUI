using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class DiscoverablePackagesLoader : AbstractPackageLoader
    {
        private string QUERY_TEXT = string.Empty;

        public DiscoverablePackagesLoader(IEnumerable<PackageManager> managers)
        : base(managers, "DISCOVERABLE_PACKAGES", AllowMultiplePackageVersions: false)
        { }

        public async Task ReloadPackages(string query)
        {
            QUERY_TEXT = query;
            await ReloadPackages();
        }

        public override async Task ReloadPackages()
        {
            if (QUERY_TEXT == "") return;
            else await base.ReloadPackages();
        }

#pragma warning disable
        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            return true;
        }
#pragma warning restore

        protected override Task<IPackage[]> LoadPackagesFromManager(IPackageManager manager)
        {
            string text = QUERY_TEXT;
            text = CoreTools.EnsureSafeQueryString(text);
            if (text == string.Empty)
                return new Task<IPackage[]>(() => { return []; });
            else
                return manager.FindPackages(text);
        }

#pragma warning disable
        protected override async Task WhenAddingPackage(IPackage package)
        {
            if (package.GetUpgradablePackage() != null)
                package.SetTag(PackageTag.IsUpgradable);
            else if (package.GetInstalledPackage() != null)
                package.SetTag(PackageTag.AlreadyInstalled);
        }
#pragma warning restore
    }
}
