using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using ModernWindow.Core.Data;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ModernWindow.PackageEngine.Operations
{
    public enum OperationVeredict
    {
        Succeeded,
        Failed,
        AutoRetry,
    }
    public enum OperationStatus
    {
        Pending,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    public enum OperationType
    {
        Install,
        Update,
        Uninstall,
        None
    }

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

        public Package Package;
        protected InstallationOptions Options;
        public PackageOperation(Package package, InstallationOptions options)
        {
            Package = package;
            Options = options;
            MainProcedure();
        }

        protected override async Task WaitForAvailability()
        {
            if(Tools.GetSettings("AllowParallelInstalls") || Tools.GetSettings("AllowParallelInstallsForManager" + Package.Manager.Name))
            {
                AppTools.Log("Parallel installs are allowed. Skipping queue check");
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
                currentIndex = Tools.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = Tools.Translate("Operation on queue (position {0})...", currentIndex);
                    oldIndex = currentIndex;
                }
                await Task.Delay(100);
            }

            Package.SetTag(PackageTag.BeingProcessed);
        }

        public PackageOperation(Package package) : this(package, new InstallationOptions(package)) { }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public InstallPackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Tools.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (Tools.GetSettings("DoCacheAdminRights") || Tools.GetSettings("DoCacheAdminRightsForBatches"))
                {
                    AppTools.Log("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = CoreData.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = "\"" + Package.Manager.Status.ExecutablePath + "\" " + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetInstallParameters(Package, Options));

            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetInstallParameters(Package, Options));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package install operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetInstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = Tools.Translate("{package} installation failed", new Dictionary<string, object>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try {
                    new ToastContentBuilder()
                        .AddArgument("action", "openWingetUI")
                        .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                        .AddText(Tools.Translate("Installation failed"))
                        .AddText(Tools.Translate("{package} could not be installed", new Dictionary<string, object>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Resources["ContentDialogMaxWidth"] = 750;
            dialog.Resources["ContentDialogMaxHeight"] = 1000;
            dialog.Title = Tools.Translate("{package} installation failed", new Dictionary<string, object>{ { "package", Package.Name } });

            StackPanel panel = new StackPanel() { Spacing = 16 };
            panel.Children.Add(new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = Tools.Translate("{package} could not be installed", new Dictionary<string, object>{ { "package", Package.Name } }) + ". " + Tools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.") });

            Expander expander = new Expander() { CornerRadius = new CornerRadius(8) };

            StackPanel HeaderPanel = new StackPanel() { Orientation = Orientation.Horizontal, Spacing = 8 };
            HeaderPanel.Children.Add(new LocalIcon("console") { VerticalAlignment = VerticalAlignment.Center, Height = 24, Width = 24, HorizontalAlignment = HorizontalAlignment.Left });
            HeaderPanel.Children.Add(new TextBlock() { Text = Tools.Translate("Command-line Output"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });

            expander.Header = HeaderPanel;
            expander.HorizontalAlignment = HorizontalAlignment.Stretch;
            panel.Children.Add(expander);

            RichTextBlock output = new RichTextBlock() { FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap };
            ScrollViewer sv = new ScrollViewer();
            sv.MaxHeight = 500;
            Paragraph par = new Paragraph();
            foreach(var line in ProcessOutput)
                par.Inlines.Add(new Run() { Text = line + "\x0a" });
            output.Blocks.Add(par);

            sv.Content = output;
            expander.Content = sv;

            dialog.Content = panel;
            dialog.PrimaryButtonText = Tools.Translate("Retry");
            dialog.CloseButtonText = Tools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            ContentDialogResult result = await Tools.App.MainWindow.ShowDialogAsync(dialog);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = Tools.Translate("{package} was installed successfully", new Dictionary<string, object>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.AlreadyInstalled);
            Tools.App.MainWindow.NavigationPage.InstalledPage.AddInstalledPackage(Package);

            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                
                try{
                    new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Installation succeeded"))
                    .AddText(Tools.Translate("{package} was installed successfully", new Dictionary<string, object>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = Tools.Translate("{package} Installation", new Dictionary<string, object>{ { "package", Package.Name } });
            IconSource = Package.GetIconUrl();
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UpdatePackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Tools.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (Tools.GetSettings("DoCacheAdminRights") || Tools.GetSettings("DoCacheAdminRightsForBatches"))
                {
                    AppTools.Log("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = CoreData.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = "\"" + Package.Manager.Status.ExecutablePath + "\" " + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUpdateParameters(Package, Options));
            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUpdateParameters(Package, Options));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package update operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUpdateOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = Tools.Translate("{package} update failed. Click here for more details.", new Dictionary<string, object>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Update failed"))
                    .AddText(Tools.Translate("{package} could not be updated", new Dictionary<string, object>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Resources["ContentDialogMaxWidth"] = 750;
            dialog.Resources["ContentDialogMaxHeight"] = 1000;
            dialog.Title = Tools.Translate("{package} update failed", new Dictionary<string, object>{ { "package", Package.Name } });

            StackPanel panel = new StackPanel() { Spacing = 16 };
            panel.Children.Add(new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = Tools.Translate("{package} could not be updated", new Dictionary<string, object>{ { "package", Package.Name } }) + ". " + Tools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.") });

            Expander expander = new Expander() { CornerRadius = new CornerRadius(8) };

            StackPanel HeaderPanel = new StackPanel() { Orientation = Orientation.Horizontal, Spacing = 8 };
            HeaderPanel.Children.Add(new LocalIcon("console") { VerticalAlignment = VerticalAlignment.Center, Height = 24, Width = 24, HorizontalAlignment = HorizontalAlignment.Left });
            HeaderPanel.Children.Add(new TextBlock() { Text = Tools.Translate("Command-line Output"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });

            expander.Header = HeaderPanel;
            expander.HorizontalAlignment = HorizontalAlignment.Stretch;
            panel.Children.Add(expander);

            RichTextBlock output = new RichTextBlock() { FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap };
            ScrollViewer sv = new ScrollViewer();
            sv.MaxHeight = 500;
            Paragraph par = new Paragraph();
            foreach (var line in ProcessOutput)
                par.Inlines.Add(new Run() { Text = line + "\x0a" });
            output.Blocks.Add(par);

            sv.Content = output;
            expander.Content = sv;

            dialog.Content = panel;
            dialog.PrimaryButtonText = Tools.Translate("Retry");
            dialog.CloseButtonText = Tools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            ContentDialogResult result = await Tools.App.MainWindow.ShowDialogAsync(dialog);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = Tools.Translate("{package} was updated successfully", new Dictionary<string, object>{ { "package", Package.Name } });

            Package.GetInstalledPackage()?.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);
            Tools.App.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            
            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                .AddArgument("action", "openWingetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(Tools.Translate("Update succeeded"))
                .AddText(Tools.Translate("{package} was updated successfully", new Dictionary<string, object>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }

            if(Package.Version == "Unknown")
                await Package.AddToIgnoredUpdatesAsync(Package.NewVersion);
            
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = Tools.Translate("{package} Update", new Dictionary<string, object>{ { "package", Package.Name } });
            IconSource = Package.GetIconUrl();
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UninstallPackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Tools.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (Tools.GetSettings("DoCacheAdminRights") || Tools.GetSettings("DoCacheAdminRightsForBatches"))
                {
                    AppTools.Log("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = CoreData.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = "\"" + Package.Manager.Status.ExecutablePath + "\" " + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUninstallParameters(Package, Options));
            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUninstallParameters(Package, Options));
            }
            Process process = new();
            process.StartInfo = startInfo;


            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package uninstall operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUninstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = Tools.Translate("{package} uninstall failed", new Dictionary<string, object>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Uninstall failed"))
                    .AddText(Tools.Translate("{package} could not be uninstalled", new Dictionary<string, object>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Resources["ContentDialogMaxWidth"] = 750;
            dialog.Resources["ContentDialogMaxHeight"] = 1000;
            dialog.Title = Tools.Translate("{package} uninstall failed", new Dictionary<string, object>{ { "package", Package.Name } });

            StackPanel panel = new StackPanel() { Spacing = 16 };
            panel.Children.Add(new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = Tools.Translate("{package} could not be uninstalled", new Dictionary<string, object>{ { "package", Package.Name } }) + ". " + Tools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.") });

            Expander expander = new Expander() { CornerRadius = new CornerRadius(8) };

            StackPanel HeaderPanel = new StackPanel() { Orientation = Orientation.Horizontal, Spacing = 8 };
            HeaderPanel.Children.Add(new LocalIcon("console") { VerticalAlignment = VerticalAlignment.Center, Height = 24, Width = 24, HorizontalAlignment = HorizontalAlignment.Left });
            HeaderPanel.Children.Add(new TextBlock() { Text = Tools.Translate("Command-line Output"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });

            expander.Header = HeaderPanel;
            expander.HorizontalAlignment = HorizontalAlignment.Stretch;
            panel.Children.Add(expander);

            RichTextBlock output = new RichTextBlock() { FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap };
            ScrollViewer sv = new ScrollViewer();
            sv.MaxHeight = 500;
            Paragraph par = new Paragraph();
            foreach (var line in ProcessOutput)
                par.Inlines.Add(new Run() { Text = line + "\x0a" });
            output.Blocks.Add(par);

            sv.Content = output;
            expander.Content = sv;

            dialog.Content = panel;
            dialog.PrimaryButtonText = Tools.Translate("Retry");
            dialog.CloseButtonText = Tools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            ContentDialogResult result = await Tools.App.MainWindow.ShowDialogAsync(dialog);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = Tools.Translate("{package} was uninstalled successfully", new Dictionary<string, object>{ { "package", Package.Name } });

            Package.GetAvailablePackage()?.SetTag(PackageTag.Default);
            Tools.App.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            Tools.App.MainWindow.NavigationPage.InstalledPage.RemoveCorrespondingPackages(Package);

            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                .AddArgument("action", "openWingetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(Tools.Translate("Uninstall succeeded"))
                .AddText(Tools.Translate("{package} was uninstalled successfully", new Dictionary<string, object>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = Tools.Translate("{package} Uninstall", new Dictionary<string, object>{ { "package", Package.Name } });
            IconSource = Package.GetIconUrl();
        }
    }
}
