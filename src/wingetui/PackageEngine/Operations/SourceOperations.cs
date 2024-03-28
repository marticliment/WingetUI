using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Data;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Operations
{

    public abstract class SourceOperation : AbstractOperation
    {
        protected ManagerSource Source;
        public SourceOperation(ManagerSource source)
        {
            Source = source;
            MainProcedure();
        }
    }

    public class AddSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs> OperationSucceeded;
        public AddSourceOperation(ManagerSource source) : base(source) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
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
                startInfo.Arguments = "\"" + Source.Manager.Status.ExecutablePath + "\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetAddSourceParameters(Source));

            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetAddSourceParameters(Source));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting adding source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return (Source.Manager as PackageManagerWithSources).GetAddSourceOperationVeredict(Source, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = Tools.Translate("Could not add source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Installation failed"))
                    .AddText(Tools.Translate("Could not add source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();

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
            dialog.Title = Tools.Translate("Source addition failed");

            StackPanel panel = new StackPanel() { Spacing = 16 };
            panel.Children.Add(new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = Tools.Translate("Could not add source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name) + ". " + Tools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.") });

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
            OperationSucceeded?.Invoke(this, new EventArgs());
            LineInfoText = Tools.Translate("The source {source} was added to {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                
                try{
                    new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Addition succeeded"))
                    .AddText(Tools.Translate("The source {source} was added to {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();

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
            OperationTitle = Tools.Translate("Adding source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }

    public class RemoveSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs> OperationSucceeded;
        public RemoveSourceOperation(ManagerSource source) : base(source) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
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
                startInfo.Arguments = "\"" + Source.Manager.Status.ExecutablePath + "\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetRemoveSourceParameters(Source));

            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetRemoveSourceParameters(Source));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting remove source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return (Source.Manager as PackageManagerWithSources).GetRemoveSourceOperationVeredict(Source, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = Tools.Translate("Could not remove source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Removal failed"))
                    .AddText(Tools.Translate("Could not remove source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Resources["ContentDialogMaxWidth"] = 750;
            dialog.Resources["ContentDialogMaxHeight"] = 1000;
            dialog.Title = Tools.Translate("Source removal failed");

            StackPanel panel = new StackPanel() { Spacing = 16 };
            panel.Children.Add(new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = Tools.Translate("Could not remove source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name) + ". " + Tools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.") });

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
            OperationSucceeded?.Invoke(this, new EventArgs());
            LineInfoText = Tools.Translate("The source {source} was removed from {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                try { 
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Removal succeeded"))
                    .AddText(Tools.Translate("The source {source} was removed from {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();

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
            OperationTitle = Tools.Translate("Removing source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }
}
