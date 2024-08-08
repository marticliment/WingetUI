using System.Diagnostics;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Operations
{

    public abstract class SourceOperation : AbstractOperation
    {
        protected IManagerSource Source;
        public SourceOperation(IManagerSource source)
        {
            Source = source;
            MainProcedure();
        }
    }

    public class AddSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs>? OperationSucceeded;
        public AddSourceOperation(IManagerSource source) : base(source) { }
        protected override async Task<Process> BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetAddSourceParameters(Source));
            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetAddSourceParameters(Source));
            }
            Process process = new()
            {
                StartInfo = startInfo
            };

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting adding source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            ];
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.GetAddSourceOperationVeredict(Source, ReturnCode, Output));
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            if (!Settings.Get("DisableErrorNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Installation failed"))
                    .AddText(CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("Source addition failed"),
                CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            if (result == ContentDialogResult.Primary)
            {
                return AfterFinshAction.Retry;
            }

            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            OperationSucceeded?.Invoke(this, EventArgs.Empty);
            LineInfoText = CoreTools.Translate("The source {source} was added to {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            if (!Settings.Get("DisableSuccessNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Addition succeeded"))
                    .AddText(CoreTools.Translate("The source {source} was added to {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })).Show();

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

        protected override void Initialize()
        {
            OperationTitle = CoreTools.Translate("Adding source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }

    public class RemoveSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs>? OperationSucceeded;
        public RemoveSourceOperation(IManagerSource source) : base(source) { }
        protected override async Task<Process> BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetRemoveSourceParameters(Source));

            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetRemoveSourceParameters(Source));
            }
            Process process = new()
            {
                StartInfo = startInfo
            };

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting remove source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            ];
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.GetRemoveSourceOperationVeredict(Source, ReturnCode, Output));
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("Could not remove source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            if (!Settings.Get("DisableErrorNotifications") && !Settings.AreNotificationsDisabled())
            {
                new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Removal failed"))
                    .AddText(CoreTools.Translate("Could not remove source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })).Show();
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("Source removal failed"),
                CoreTools.Translate("Could remove source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            if (result == ContentDialogResult.Primary)
            {
                return AfterFinshAction.Retry;
            }

            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            OperationSucceeded?.Invoke(this, EventArgs.Empty);
            LineInfoText = CoreTools.Translate("The source {source} was removed from {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            if (!Settings.Get("DisableSuccessNotifications") && !Settings.AreNotificationsDisabled())
            {
                try
                {
                    new ToastContentBuilder()
                        .AddArgument("action", "OpenUniGetUI")
                        .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                        .AddText(CoreTools.Translate("Removal succeeded"))
                        .AddText(CoreTools.Translate("The source {source} was removed from {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })).Show();

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

        protected override void Initialize()
        {
            OperationTitle = CoreTools.Translate("Removing source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }
}
