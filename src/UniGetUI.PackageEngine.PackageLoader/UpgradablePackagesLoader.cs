using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class UpgradablePackagesLoader : AbstractPackageLoader
    {

        System.Timers.Timer? UpdatesTimer;

        public UpgradablePackagesLoader(IEnumerable<PackageManager> managers)
        : base(managers, "DISCOVERABLE_PACKAGES", AllowMultiplePackageVersions: false)
        {
            FinishedLoading += (s, e) => StartAutoCheckTimeout();
        }

        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            if (await package.HasUpdatesIgnoredAsync(package.NewVersion))
                return false;

            if (package.IsUpgradable && package.NewerVersionIsInstalled())
                return false;

            return true;
        }

        protected override Task<IPackage[]> LoadPackagesFromManager(IPackageManager manager)
        {
            return manager.GetAvailableUpdates();
        }
#pragma warning disable 
        protected override async Task WhenAddingPackage(IPackage package)
        {
            package.GetAvailablePackage()?.SetTag(PackageTag.IsUpgradable);
            package.GetInstalledPackage()?.SetTag(PackageTag.IsUpgradable);
        }
#pragma warning restore

        protected void StartAutoCheckTimeout()
        {
            if (!Settings.Get("DisableAutoCheckforUpdates"))
            {
                long waitTime = 3600;
                try
                {
                    waitTime = long.Parse(Settings.GetValue("UpdatesCheckInterval"));
                    Logger.Debug($"Starting check for updates wait interval with waitTime={waitTime}");
                }
                catch
                {
                    Logger.Debug("Invalid value for UpdatesCheckInterval, using default value of 3600 seconds");
                }

                if (UpdatesTimer != null)
                {
                    UpdatesTimer.Stop();
                    UpdatesTimer.Dispose();
                }

                UpdatesTimer = new System.Timers.Timer(waitTime * 1000);
                UpdatesTimer.Enabled = false;
                UpdatesTimer.AutoReset = false;
                UpdatesTimer.Elapsed += (s, e) => _ = ReloadPackages();
                UpdatesTimer.Start();
            }
        }
    }
}
