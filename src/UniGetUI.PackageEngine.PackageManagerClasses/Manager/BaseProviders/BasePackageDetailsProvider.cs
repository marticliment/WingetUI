using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Manager.BaseProviders
{
    public abstract class BasePackageDetailsProvider<T> : IPackageDetailsProvider where T : IPackageManager
    {
        protected T Manager;

        public BasePackageDetailsProvider(T manager)
        {
            Manager = manager;
        }

        public async Task GetPackageDetails(IPackageDetails details)
        {
            await GetPackageDetails_Unsafe(details);
        }

        public async Task<string[]> GetPackageVersions(IPackage package)
        {
            if (Manager.Capabilities.SupportsCustomVersions)
            {
                string[] result = await GetPackageVersions_Unsafe(package);
                Logger.Debug($"Found {result.Length} versions for package Id={package.Id} on manager {Manager.Name}");
                return result;
            }
            else
            {
                Logger.Warn($"Manager {Manager.Name} does not support version retrieving, this method should have not been called");
                return [];
            }
        }

        public async Task<CacheableIcon?> GetPackageIconUrl(IPackage package)
        {
            CacheableIcon? Icon = null;
            if (Manager.Capabilities.SupportsCustomPackageIcons)
            {
                Icon = await GetPackageIcon_Unsafe(package);
                if(Icon == null) Logger.Debug($"Manager {Manager.Name} did not find a native icon for {package.Id}");
            }
            else
                Logger.Debug($"Manager {Manager.Name} does not support native icons");

            if (Icon == null)
            {
                string url = IconDatabase.Instance.GetIconUrlForId(package.GetIconId());
                if(url != "") Icon = new CacheableIcon(new Uri(url), package.Version);
            }

            if (Icon == null)
            {
                Logger.Warn($"Icon for package {package.Id} was not found, returning default icon");
                return null;
            }
            else
            { 
                Logger.Info($"Loaded icon with URL={Icon.ToString()} for package Id={package.Id}");
            }
            return Icon;
        }

        public async Task<Uri[]> GetPackageScreenshotsUrl(IPackage package)
        {
            Uri[] URIs = [];

            if (Manager.Capabilities.SupportsCustomPackageScreenshots)
                URIs = await GetPackageScreenshots_Unsafe(package);
            else
                Logger.Debug($"Manager {Manager.Name} does not support native screenshots");

            if(URIs.Length == 0){
                string[] UrlArray = IconDatabase.Instance.GetScreenshotsUrlForId(package.Id);
                List<Uri> UriList = new();
                foreach (string url in UrlArray) if (url != "") UriList.Add(new Uri(url));
                URIs = UriList.ToArray();
            }
            Logger.Info($"Found {URIs.Length} screenshots for package Id={package.Id}");
            return URIs;
        }

        protected abstract Task GetPackageDetails_Unsafe(IPackageDetails details);
        protected abstract Task<string[]> GetPackageVersions_Unsafe(IPackage package);
        protected abstract Task<CacheableIcon?> GetPackageIcon_Unsafe(IPackage package);
        protected abstract Task<Uri[]> GetPackageScreenshots_Unsafe(IPackage package);
    }
}
