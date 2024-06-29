using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.SoftwarePages;
using UniGetUI.Interface.SoftwarePages.UniGetUI.Interface.SoftwarePages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public sealed partial class MainView : UserControl
    {
        public SettingsInterface SettingsPage;
        public DiscoverSoftwarePage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public HelpDialog? HelpPage;
        public NewPackageBundlesPage BundlesPage;
        public Page? OldPage;
        public Page? CurrentPage;
        public InfoBadge UpdatesBadge;
        public InfoBadge BundleBadge;
        public StackPanel OperationStackPanel;
        private readonly Dictionary<Page, NavButton> PageButtonReference = new();

        public MainView()
        {
            InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            BundleBadge = __bundle_count_badge;
            OperationStackPanel = __operations_list_stackpanel;
            DiscoverPage = new DiscoverSoftwarePage();
            UpdatesPage = new SoftwareUpdatesPage();
            UpdatesPage.ExternalCountBadge = UpdatesBadge;
            InstalledPage = new InstalledPackagesPage();
            BundlesPage = new NewPackageBundlesPage();
            SettingsPage = new SettingsInterface();

            int i = 0;
            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, SettingsPage, BundlesPage })
            {
                Grid.SetColumn(page, 0);
                Grid.SetRow(page, 0);
                MainContentPresenterGrid.Children.Add(page);
                i++;
            }

            PageButtonReference.Add(DiscoverPage, DiscoverNavButton);
            PageButtonReference.Add(UpdatesPage, UpdatesNavButton);
            PageButtonReference.Add(InstalledPage, InstalledNavButton);
            PageButtonReference.Add(SettingsPage, SettingsNavButton);
            PageButtonReference.Add(BundlesPage, BundlesNavButton);

            DiscoverNavButton.ForceClick();

            if (CoreTools.IsAdministrator() && !Settings.Get("AlreadyWarnedAboutAdmin"))
            {
                Settings.Set("AlreadyWarnedAboutAdmin", true);
                WarnAboutAdminRights();
            }


            Dictionary<Page, NavButton> NextPageReference = new()
            {
                { DiscoverPage, UpdatesNavButton },
                { UpdatesPage, InstalledNavButton },
                { InstalledPage, BundlesNavButton },
                { BundlesPage, SettingsNavButton },
                { SettingsPage, DiscoverNavButton },
            };

            Dictionary<Page, NavButton> PreviousTabReference = new()
            {
                { DiscoverPage, SettingsNavButton },
                { UpdatesPage, DiscoverNavButton },
                { InstalledPage, UpdatesNavButton },
                { BundlesPage, InstalledNavButton },
                { SettingsPage, BundlesNavButton },
            };

            KeyUp += (s, e) =>
            {
                bool IS_CONTROL_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                bool IS_SHIFT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                if (e.Key == Windows.System.VirtualKey.Tab && IS_CONTROL_PRESSED)
                {
                    if (CurrentPage != null)
                    {
                        if (!IS_SHIFT_PRESSED)
                        {
                            if (NextPageReference.ContainsKey(CurrentPage)) NextPageReference[CurrentPage].ForceClick();
                            else DiscoverNavButton.ForceClick();
                        }
                        else
                        {
                            if (PreviousTabReference.ContainsKey(CurrentPage)) PreviousTabReference[CurrentPage].ForceClick();
                            else DiscoverNavButton.ForceClick();
                        }
                    }
                }
                else if (e.Key == Windows.System.VirtualKey.F1)
                {
                    HelpMenu_Click(s, e);
                }
                else if (IS_CONTROL_PRESSED && (e.Key == Windows.System.VirtualKey.Q || e.Key == Windows.System.VirtualKey.W))
                {
                    MainApp.Instance.MainWindow.Close();
                }
                else if (e.Key == Windows.System.VirtualKey.F5 || (IS_CONTROL_PRESSED && e.Key == Windows.System.VirtualKey.R))
                {
                    (CurrentPage as IPageWithKeyboardShortcuts)?.ReloadTriggered();
                }
                else if (IS_CONTROL_PRESSED && e.Key == Windows.System.VirtualKey.F)
                {
                    (CurrentPage as IPageWithKeyboardShortcuts)?.SearchTriggered();
                }
                else if (IS_CONTROL_PRESSED && e.Key == Windows.System.VirtualKey.A)
                {
                    (CurrentPage as IPageWithKeyboardShortcuts)?.SelectAllTriggered();
                }
            };
        }

        private void DiscoverNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(DiscoverPage);
        }

        private void InstalledNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(InstalledPage);
        }

        private void UpdatesNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(UpdatesPage);
        }

        private void BundlesNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(BundlesPage);
        }

        private void MoreNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {

            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;
            MoreNavButton.ToggleButton.IsChecked = true;

            (VersionMenuItem as MenuFlyoutItem).Text = CoreTools.Translate("WingetUI Version {0}", CoreData.VersionName);
            MoreNavButtonMenu.ShowAt(MoreNavButton, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard });

            MoreNavButtonMenu.Closed += (s, e) =>
            {
                foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                    button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage ?? DiscoverPage]);
            };
        }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private async void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            ContentDialog? AboutDialog = new();
            AboutUniGetUI AboutPage = new();
            AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            AboutDialog.XamlRoot = XamlRoot;
            AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
            AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
            AboutDialog.Content = AboutPage;
            AboutDialog.PrimaryButtonText = CoreTools.Translate("Close");
            AboutPage.Close += (s, e) => { AboutDialog.Hide(); };
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;

            await MainApp.Instance.MainWindow.ShowDialogAsync(AboutDialog);

            AboutDialog.Content = null;
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage ?? DiscoverPage]);
            AboutDialog = null;
        }

        public async Task ManageIgnoredUpdatesDialog()
        {
            ContentDialog? UpdatesDialog = new();
            UpdatesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            UpdatesDialog.XamlRoot = XamlRoot;
            UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1200;
            UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;
            UpdatesDialog.SecondaryButtonText = CoreTools.Translate("Close");
            UpdatesDialog.PrimaryButtonText = CoreTools.Translate("Reset");
            UpdatesDialog.DefaultButton = ContentDialogButton.Secondary;
            UpdatesDialog.Title = CoreTools.Translate("Manage ignored updates");
            IgnoredUpdatesManager IgnoredUpdatesPage = new();
            UpdatesDialog.PrimaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            UpdatesDialog.Content = IgnoredUpdatesPage;
            IgnoredUpdatesPage.Close += (s, e) => { UpdatesDialog.Hide(); };

            _ = IgnoredUpdatesPage.UpdateData();
            await MainApp.Instance.MainWindow.ShowDialogAsync(UpdatesDialog);

            UpdatesDialog.Content = null;
            UpdatesDialog = null;
        }

        public async Task<ContentDialogResult> ShowOperationFailedDialog(
            IEnumerable<string> processOutput, 
            string dialogTitle, 
            string shortDescription)
        {
            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Resources["ContentDialogMaxWidth"] = 850;
            dialog.Resources["ContentDialogMaxHeight"] = 800;
            dialog.Title = dialogTitle;

            Grid grid = new() { 
                RowSpacing = 16,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock headerContent = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = $"{shortDescription}. "
                        + CoreTools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.")
            };

            StackPanel HeaderPanel = new() { 
                Orientation = Orientation.Horizontal, 
                Spacing = 8 
            };
            
            HeaderPanel.Children.Add(new LocalIcon("console") { 
                VerticalAlignment = VerticalAlignment.Center, 
                Height = 24, 
                Width = 24, 
                HorizontalAlignment = HorizontalAlignment.Left 
            });
            
            HeaderPanel.Children.Add(new TextBlock { 
                Text = CoreTools.Translate("Command-line Output"), 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center 
            });


            RichTextBlock CommandLineOutput = new() { 
                FontFamily = new FontFamily("Consolas"), 
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            ScrollViewer ScrollView = new() {
                BorderBrush = new SolidColorBrush(),
                Content = CommandLineOutput,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            Grid OutputGrid = new();
            OutputGrid.Children.Add(ScrollView);
            OutputGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            OutputGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(ScrollView, 0);
            Grid.SetRow(ScrollView, 0);

            Expander expander = new()
            {
                Header = HeaderPanel,
                Content = OutputGrid,
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            Paragraph par = new();
            foreach (string line in processOutput)
                par.Inlines.Add(new Run { Text = line + "\x0a" });
            CommandLineOutput.Blocks.Add(par);
            
            grid.Children.Add(headerContent);
            grid.Children.Add(expander);
            Grid.SetRow(headerContent, 0);
            Grid.SetRow(expander, 1);

            dialog.Content = grid;
            dialog.PrimaryButtonText = CoreTools.Translate("Retry");
            dialog.CloseButtonText = CoreTools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            return await MainApp.Instance.MainWindow.ShowDialogAsync(dialog);
        }
        
        public async void WarnAboutAdminRights()
        {
            ContentDialog AdminDialog = new();
            AdminDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            while (XamlRoot == null)
            {
                await Task.Delay(100);
            }

            AdminDialog.XamlRoot = XamlRoot;
            AdminDialog.PrimaryButtonText = CoreTools.Translate("I understand");
            AdminDialog.DefaultButton = ContentDialogButton.Primary;
            AdminDialog.Title = CoreTools.Translate("Administrator privileges");
            AdminDialog.Content = CoreTools.Translate("WingetUI has been ran as administrator, which is not recommended. When running WingetUI as administrator, EVERY operation launched from WingetUI will have administrator privileges. You can still use the program, but we highly recommend not running WingetUI with administrator privileges.");

            await MainApp.Instance.MainWindow.ShowDialogAsync(AdminDialog);
        }

        public async Task<bool> ShowInstallationSettingsForPackageAndContinue(IPackage package, OperationType Operation)
        {
            InstallOptionsPage OptionsPage = new(package, Operation);

            ContentDialog? OptionsDialog = new();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            if (Operation == OperationType.Install)
                OptionsDialog.SecondaryButtonText = CoreTools.Translate("Install");
            else if (Operation == OperationType.Update)
                OptionsDialog.SecondaryButtonText = CoreTools.Translate("Update");
            else if (Operation == OperationType.Uninstall)
                OptionsDialog.SecondaryButtonText = CoreTools.Translate("Uninstall");
            else
                OptionsDialog.SecondaryButtonText = "";
            OptionsDialog.PrimaryButtonText = CoreTools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = CoreTools.Translate("{0} installation options", package.Name);
            OptionsDialog.Content = OptionsPage;
            OptionsPage.Close += (s, e) => { OptionsDialog.Hide(); };

            ContentDialogResult result = await MainApp.Instance.MainWindow.ShowDialogAsync(OptionsDialog);
            OptionsPage.SaveToDisk();

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return result == ContentDialogResult.Secondary;

        }

        public async Task<IInstallationOptions> UpdateInstallationSettings(IPackage package, IInstallationOptions options)
        {
            InstallOptionsPage OptionsPage = new(package, options);

            ContentDialog? OptionsDialog = new();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            OptionsDialog.SecondaryButtonText = "";
            OptionsDialog.PrimaryButtonText = CoreTools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = CoreTools.Translate("{0} installation options", package.Name);
            OptionsDialog.Content = OptionsPage;
            OptionsPage.Close += (s, e) => { OptionsDialog.Hide(); };

            await MainApp.Instance.MainWindow.ShowDialogAsync(OptionsDialog);

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return await OptionsPage.GetUpdatedOptions();

        }

        private void NavigateToPage(Page TargetPage)
        {
            if (!PageButtonReference.ContainsKey(TargetPage))
            {
                PageButtonReference.Add(TargetPage, MoreNavButton);
                Grid.SetColumn(TargetPage, 0);
                Grid.SetRow(TargetPage, 0);
                MainContentPresenterGrid.Children.Add(TargetPage);
            }
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {

                button.ToggleButton.IsChecked = (button == PageButtonReference[TargetPage]);
            }

            foreach (Page page in PageButtonReference.Keys)
                page.Visibility = (page == TargetPage) ? Visibility.Visible : Visibility.Collapsed;

            OldPage = CurrentPage;
            CurrentPage = TargetPage;
        }

        private async void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
        {

            ContentDialog? NotesDialog = new();
            NotesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            NotesDialog.XamlRoot = XamlRoot;
            NotesDialog.Resources["ContentDialogMaxWidth"] = 12000;
            NotesDialog.Resources["ContentDialogMaxHeight"] = 10000;
            NotesDialog.CloseButtonText = CoreTools.Translate("Close");
            NotesDialog.Title = CoreTools.Translate("Release notes");
            ReleaseNotes? notes = new();
            NotesDialog.Content = notes;
            NotesDialog.SizeChanged += (s, e) =>
            {
                notes.MinWidth = ActualWidth - 300;
                notes.MinHeight = ActualHeight - 200;
            };

            await MainApp.Instance.MainWindow.ShowDialogAsync(NotesDialog);

            notes.Dispose();
            notes = null;
            NotesDialog = null;
        }

        public async Task ShowPackageDetails(IPackage package, OperationType ActionOperation)
        {
            PackageDetailsPage? DetailsPage = new(package, ActionOperation);

            ContentDialog? DetailsDialog = new();
            DetailsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            DetailsDialog.XamlRoot = XamlRoot;
            DetailsDialog.Resources["ContentDialogMaxWidth"] = 8000;
            DetailsDialog.Resources["ContentDialogMaxHeight"] = 4000;
            DetailsDialog.Content = DetailsPage;
            DetailsDialog.SizeChanged += (s, e) =>
            {
                int hOffset = (ActualWidth < 1300) ? 100 : 300;
                DetailsPage.MinWidth = ActualWidth - hOffset;
                DetailsPage.MinHeight = ActualHeight - 100;
                DetailsPage.MaxWidth = ActualWidth - hOffset;
                DetailsPage.MaxHeight = ActualHeight - 100;
            };

            DetailsPage.Close += (s, e) => { DetailsDialog.Hide(); };

            await MainApp.Instance.MainWindow.ShowDialogAsync(DetailsDialog);

            DetailsDialog.Content = null;
            DetailsDialog = null;

        }

        public async Task<bool> ConfirmUninstallation(IPackage package)
        {
            ContentDialog dialog = new();

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = CoreTools.Translate("Are you sure?");
            dialog.PrimaryButtonText = CoreTools.Translate("No");
            dialog.SecondaryButtonText = CoreTools.Translate("Yes");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = CoreTools.Translate("Do you really want to uninstall {0}?", package.Name);

            return await MainApp.Instance.MainWindow.ShowDialogAsync(dialog) == ContentDialogResult.Secondary;
        }

        public async Task<bool> ConfirmUninstallation(IEnumerable<IPackage> packages)
        {
            if (packages.Count() == 0) return false;
            if (packages.Count() == 1)
            {
                return await ConfirmUninstallation(packages.First());
            }

            ContentDialog dialog = new();

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = CoreTools.Translate("Are you sure?");
            dialog.PrimaryButtonText = CoreTools.Translate("No");
            dialog.SecondaryButtonText = CoreTools.Translate("Yes");
            dialog.DefaultButton = ContentDialogButton.Primary;

            StackPanel p = new();
            p.Children.Add(new TextBlock { 
                Text = CoreTools.Translate("Do you really want to uninstall the following {0} packages?", packages.Count()), 
                Margin = new Thickness(0, 0, 0, 5) 
            });

            string pkgList = "";
            foreach (Package package in packages)
                pkgList += " ● " + package.Name + "\x0a";

            TextBlock PackageListTextBlock = new() { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Text = pkgList };
            p.Children.Add(new ScrollView { Content = PackageListTextBlock, MaxHeight = 200 });

            dialog.Content = p;

            return await MainApp.Instance.MainWindow.ShowDialogAsync(dialog) == ContentDialogResult.Secondary;
        }



        private void OperationHistoryMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Logger_LogPage(Logger_LogType.OperationHistory));
        }

        private void ManagerLogsMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Logger_LogPage(Logger_LogType.ManagerLogs));
        }

        public void UniGetUILogs_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Logger_LogPage(Logger_LogType.UniGetUILog));
        }

        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            if (HelpPage == null)
                HelpPage = new HelpDialog();
            NavigateToPage(HelpPage);
        }

        private void QuitUniGetUI_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.DisposeAndQuit();
        }
    }
}
