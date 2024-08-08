using System.Diagnostics;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Operations
{

    public class OperationCancelledEventArgs : EventArgs
    {
        public OperationStatus OldStatus;
        public OperationCancelledEventArgs(OperationStatus OldStatus)
        {
            this.OldStatus = OldStatus;
        }
    }

    public abstract class PackageOperation : AbstractOperation
    {
        public IPackage Package;
        protected IInstallationOptions Options;
        protected OperationType Role;
        public PackageOperation(
            IPackage package,
            IInstallationOptions options,
            OperationType role,
            bool IgnoreParallelInstalls = false)
        : base(IgnoreParallelInstalls)
        {
            Package = package;
            Options = options;
            Role = role;
            MainProcedure();
        }

        public PackageOperation(
            IPackage package,
            OperationType role,
            bool IgnoreParallelInstalls = false)
            : this(package, InstallationOptions.FromPackage(package), role, IgnoreParallelInstalls)
        { }

        protected sealed override async Task<Process> BuildProcessInstance(ProcessStartInfo startInfo)
        {
            string operation_args = string.Join(" ", Package.Manager.GetOperationParameters(Package, Options, Role));

            if (Package.OverridenOptions.RunAsAdministrator == true || Options.RunAsAdministrator || Settings.Get("AlwaysElevate" + Package.Manager.Name))
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = $"\"{Package.Manager.Status.ExecutablePath}\" {Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = $"{Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }

            return new Process()
            {
                StartInfo = startInfo
            };
        }

#pragma warning disable CS1998
        protected sealed override async Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetOperationResult(Package, Role, Output, ReturnCode);
        }
#pragma warning restore CS1998

        protected override async Task WaitForAvailability()
        {
            if (!IGNORE_PARALLEL_OPERATION_SETTINGS &&
                (Settings.Get("AllowParallelInstalls")
                || Settings.Get($"AllowParallelInstallsForManager{Package.Manager.Name}")))
            {
                Logger.Debug("Parallel installs are allowed. Skipping queue check");
                Package.SetTag(PackageTag.BeingProcessed);
                return;
            }

            Package.SetTag(PackageTag.OnQueue);

            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status == OperationStatus.Cancelled)
                {
                    Package.Tag = PackageTag.Default;
                    return; // If th operation has been cancelled
                }
                currentIndex = MainApp.Instance.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = CoreTools.Translate("Operation on queue (position {0})...", currentIndex);
                    oldIndex = currentIndex;
                }
                await Task.Delay(100);
            }
            Package.SetTag(PackageTag.BeingProcessed);

        }

    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Install, IgnoreParallelInstalls) { }

        public InstallPackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Install, IgnoreParallelInstalls) { }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting package install operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString(),
            ];
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!(Settings.Get("DisableErrorNotifications") || Settings.AreNotificationsDisabled()))
            {
                try
                {
                    new ToastContentBuilder()
                        .AddArgument("action", "OpenUniGetUI")
                        .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                        .AddText(CoreTools.Translate("Installation failed"))
                        .AddText(CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (result == ContentDialogResult.Primary)
            {
                return AfterFinshAction.Retry;
            }

            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was installed successfully", new Dictionary<string, object?> { { "package", Package.Name } });

            Package.SetTag(PackageTag.AlreadyInstalled);
            PEInterface.InstalledPackagesLoader.AddForeign(Package);

            if (!Settings.Get("DisableSuccessNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Installation succeeded"))
                    .AddText(CoreTools.Translate("{package} was installed successfully", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override async void Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Installation", new Dictionary<string, object?> { { "package", Package.Name } });
            IconSource = await Package.GetIconUrl();
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Update, IgnoreParallelInstalls) { }
        public UpdatePackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Update, IgnoreParallelInstalls) { }
      

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting package update operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString(),
            ];
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} update failed. Click here for more details.", new Dictionary<string, object?> { { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Settings.Get("DisableErrorNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Update failed"))
                    .AddText(CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("{package} update failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (result == ContentDialogResult.Primary)
            {
                return AfterFinshAction.Retry;
            }

            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was updated successfully", new Dictionary<string, object?> { { "package", Package.Name } });

            Package.GetInstalledPackage()?.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);

            if (await Package.HasUpdatesIgnoredAsync() && await Package.GetIgnoredUpdatesVersionAsync() != "*")
            {
                await Package.RemoveFromIgnoredUpdatesAsync();
            }

            PEInterface.UpgradablePackagesLoader.Remove(Package);

            if (!Settings.Get("DisableSuccessNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                .AddArgument("action", "OpenUniGetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(CoreTools.Translate("Update succeeded"))
                .AddText(CoreTools.Translate("{package} was updated successfully", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            if (Package.Version == "Unknown")
            {
                await Package.AddToIgnoredUpdatesAsync(Package.NewVersion);
            }

            return AfterFinshAction.TimeoutClose;
        }

        protected override async void Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Update", new Dictionary<string, object?> { { "package", Package.Name } });
            IconSource = await Package.GetIconUrl();
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Uninstall, IgnoreParallelInstalls) { }
        public UninstallPackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Uninstall, IgnoreParallelInstalls) { }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting package uninstall operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString(),
            ];
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Settings.Get("DisableErrorNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Uninstall failed"))
                    .AddText(CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (result == ContentDialogResult.Primary)
            {
                return AfterFinshAction.Retry;
            }

            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was uninstalled successfully", new Dictionary<string, object?> { { "package", Package.Name } });

            Package.GetAvailablePackage()?.SetTag(PackageTag.Default);
            PEInterface.UpgradablePackagesLoader.Remove(Package);
            PEInterface.InstalledPackagesLoader.Remove(Package);

            if (!Settings.Get("DisableSuccessNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                .AddArgument("action", "OpenUniGetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(CoreTools.Translate("Uninstall succeeded"))
                .AddText(CoreTools.Translate("{package} was uninstalled successfully", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override async void Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Uninstall", new Dictionary<string, object?> { { "package", Package.Name } });
            IconSource = await Package.GetIconUrl();
        }
    }
}
