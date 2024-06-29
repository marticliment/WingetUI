using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.ChocolateyManager;
using UniGetUI.PackageEngine.Managers.DotNetManager;
using UniGetUI.PackageEngine.Managers.NpmManager;
using UniGetUI.PackageEngine.Managers.PipManager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Managers.ScoopManager;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.PackageLoader;

namespace UniGetUI.PackageEngine
{
    public static class PEInterface
    {
        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Manager initialization

        public static readonly WinGet WinGet = new();
        public static readonly Scoop Scoop = new();
        public static readonly Chocolatey Chocolatey = new();
        public static readonly Npm Npm = new();
        public static readonly Pip Pip = new();
        public static readonly DotNet DotNet = new();
        public static readonly PowerShell PowerShell = new();

        public static readonly PackageManager[] Managers = [WinGet, Scoop, Chocolatey, Npm, Pip, DotNet, PowerShell];

        public static readonly DiscoverablePackagesLoader DiscoveredPackagesLoader = new(Managers);
        public static readonly UpgradablePackagesLoader UpgradablePackagesLoader = new(Managers);
        public static readonly InstalledPackagesLoader InstalledPackagesLoader = new(Managers);
        public static readonly PackageBundlesLoader PackageBundlesLoader = new(Managers);

        public static async Task Initialize()
        {
            List<Task> initializeTasks = new();

            foreach (PackageManager manager in Managers)
            {
                initializeTasks.Add(manager.InitializeAsync());
            }

            Task ManagersMetaTask = Task.WhenAll(initializeTasks);
            try
            {
                await ManagersMetaTask.WaitAsync(TimeSpan.FromMilliseconds(ManagerLoadTimeout));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            if (ManagersMetaTask.IsCompletedSuccessfully == false)
                Logger.Warn("Timeout: Not all package managers have finished initializing.");

            _ = UpgradablePackagesLoader.ReloadPackages();
            _ = InstalledPackagesLoader.ReloadPackages();
        }
    }
}
