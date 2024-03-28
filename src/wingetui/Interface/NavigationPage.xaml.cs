using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using ModernWindow.Core.Data;
using ModernWindow.Interface.Dialogs;
using ModernWindow.Interface.Pages;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    public sealed partial class NavigationPage : UserControl
    {
        public AppTools Tools = AppTools.Instance;
        public SettingsInterface SettingsPage;
        public DiscoverPackagesPage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public HelpDialog HelpPage;
        public PackageBundlePage BundlesPage;
        public Page OldPage;
        public Page CurrentPage;
        public InfoBadge UpdatesBadge;
        public InfoBadge BundleBadge;
        public StackPanel OperationStackPanel;
        private Dictionary<Page, NavButton> PageButtonReference = new();

        private AboutWingetUI AboutPage;
        private IgnoredUpdatesManager IgnoredUpdatesPage;

        public NavigationPage()
        {
            InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            BundleBadge = __bundle_count_badge;
            OperationStackPanel = __operations_list_stackpanel;
            SettingsPage = new SettingsInterface();
            DiscoverPage = new DiscoverPackagesPage();
            UpdatesPage = new SoftwareUpdatesPage();
            InstalledPage = new InstalledPackagesPage();
            AboutPage = new AboutWingetUI();
            BundlesPage = new PackageBundlePage();
            IgnoredUpdatesPage = new IgnoredUpdatesManager();

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

            if (Tools.IsAdministrator() && !Tools.GetSettings("AlreadyWarnedAboutAdmin"))
            {
                Tools.SetSettings("AlreadyWarnedAboutAdmin", true);
                WarnAboutAdminRights();
            }

            if (!Tools.GetSettings("AlreadyWarnedAboutNameChange"))
            {
                Tools.SetSettings("AlreadyWarnedAboutNameChange", true);
                WarnAboutNewName();
            }

            var NextPageReference = new Dictionary<Page, NavButton>
            {
                { DiscoverPage, UpdatesNavButton },
                { UpdatesPage, InstalledNavButton },
                { InstalledPage, BundlesNavButton },
                { BundlesPage, SettingsNavButton },
                { SettingsPage, DiscoverNavButton },
            };

            var PreviousTabReference = new Dictionary<Page, NavButton>
            {
                { DiscoverPage, SettingsNavButton },
                { UpdatesPage, DiscoverNavButton },
                { InstalledPage, UpdatesNavButton },
                { BundlesPage, InstalledNavButton },
                { SettingsPage, BundlesNavButton },
            };

            KeyUp += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Tab && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (!InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        if (NextPageReference.ContainsKey(CurrentPage))
                            NextPageReference[CurrentPage].ForceClick();
                        else
                            DiscoverNavButton.ForceClick();
                    }
                    else
                    {
                        if (NextPageReference.ContainsKey(CurrentPage))
                            PreviousTabReference[CurrentPage].ForceClick();
                        else
                            DiscoverNavButton.ForceClick();
                        }
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

            foreach (NavButton button in Tools.App.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;
            MoreNavButton.ToggleButton.IsChecked = true;

            (VersionMenuItem as MenuFlyoutItem).Text = Tools.Translate("WingetUI Version {0}", CoreData.VersionName);
            MoreNavButtonMenu.ShowAt(MoreNavButton, new FlyoutShowOptions() { ShowMode = FlyoutShowMode.Standard });

            MoreNavButtonMenu.Closed += (s, e) =>
            {
                foreach (NavButton button in Tools.App.MainWindow.NavButtonList)
                    button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage]);
            };
        }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private async void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            ContentDialog AboutDialog = new();
            AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            AboutDialog.XamlRoot = XamlRoot;
            AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
            AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
            AboutDialog.Content = AboutPage;
            AboutDialog.PrimaryButtonText = Tools.Translate("Close");
            foreach (NavButton button in Tools.App.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;

            await Tools.App.MainWindow.ShowDialogAsync(AboutDialog);

            AboutDialog.Content = null;
            foreach (NavButton button in Tools.App.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage]);
            AboutDialog = null;
        }

        public async Task ManageIgnoredUpdatesDialog()
        {
            ContentDialog UpdatesDialog = new();
            UpdatesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            UpdatesDialog.XamlRoot = XamlRoot;
            UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1200;
            UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;
            UpdatesDialog.SecondaryButtonText = Tools.Translate("Close");
            UpdatesDialog.PrimaryButtonText = Tools.Translate("Reset");
            UpdatesDialog.DefaultButton = ContentDialogButton.Secondary;
            UpdatesDialog.Title = Tools.Translate("Manage ignored updates");
            UpdatesDialog.PrimaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            UpdatesDialog.Content = IgnoredUpdatesPage;

            _ = IgnoredUpdatesPage.UpdateData();
            await Tools.App.MainWindow.ShowDialogAsync(UpdatesDialog);

            UpdatesDialog.Content = null;
            UpdatesDialog = null;
        }

        public async void WarnAboutAdminRights()
        {
            ContentDialog AdminDialog = new();
            AdminDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            while(this.XamlRoot == null)
            {
                await Task.Delay(100);
            }

            AdminDialog.XamlRoot = this.XamlRoot;
            AdminDialog.PrimaryButtonText = Tools.Translate("I understand");
            AdminDialog.DefaultButton = ContentDialogButton.Primary;
            AdminDialog.Title = Tools.Translate("Administrator privileges");
            AdminDialog.SecondaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            AdminDialog.Content = Tools.Translate("WingetUI has been ran as administrator, which is not recommended. When running WingetUI as administrator, EVERY operation launched from WingetUI will have administrator privileges. You can still use the program, but we highly recommend not running WingetUI with administrator privileges.");

            await Tools.App.MainWindow.ShowDialogAsync(AdminDialog);
        }

        public async void WarnAboutNewName()
        {
            ContentDialog AdminDialog = new();
            AdminDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            while (this.XamlRoot == null)
            {
                await Task.Delay(100);
            }

            string NEW_NAME = "UnigetUI";

            AdminDialog.XamlRoot = this.XamlRoot;
            AdminDialog.PrimaryButtonText = Tools.Translate("I understand");
            AdminDialog.DefaultButton = ContentDialogButton.Primary;
            AdminDialog.SecondaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            StackPanel p = new StackPanel() { Spacing = 16 };
            AdminDialog.Content = p;

            p.Children.Add(new Image() { Source = new BitmapImage() { UriSource = new Uri("ms-appx:///Assets/Images/icon.png") }, Height = 96 });

            var par = new Paragraph();
            par.Inlines.Add(new Run() { Text = Tools.Translate("WingetUI will become {newname} soon!", new Dictionary<string, object>{ { "newname", NEW_NAME } }), FontSize = 24, FontWeight = new Windows.UI.Text.FontWeight(700), FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI Variable Display Bold") });
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new Run() { Text = Tools.Translate("WingetUI will soon be named {newname}. This will not represent any change in the application. I (the developer) will continue the development of this project as I am doing right now, but under a different name.", new Dictionary<string, object>{ { "newname", NEW_NAME } }) });
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new Run() { Text = Tools.Translate("WingetUI is being renamed in order to emphasize the difference between WingetUI (the interface you are using right now) and Winget (a package manager developed by Microsoft with which I am not related)"), FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new Run() { Text = Tools.Translate("While Winget can be used within WingetUI, WingetUI can be used with other package managers, which can be confusing. In the past, WingetUI was designed to work only with Winget, but this is not true anymore, and therefore WingetUI does not represent what this project aims to become."), FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });

            var text = new RichTextBlock();
            text.Blocks.Add(par);
            p.Children.Add(text);

            await Tools.App.MainWindow.ShowDialogAsync(AdminDialog);
        }


        public async Task<bool> ShowInstallationSettingsForPackageAndContinue(Package package, OperationType Operation)
        {
            InstallOptionsPage OptionsPage = new(package, Operation);

            ContentDialog OptionsDialog = new();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            if (Operation == OperationType.Install)
                OptionsDialog.SecondaryButtonText = Tools.Translate("Install");
            else if (Operation == OperationType.Update)
                OptionsDialog.SecondaryButtonText = Tools.Translate("Update");
            else if (Operation == OperationType.Uninstall)
                OptionsDialog.SecondaryButtonText = Tools.Translate("Uninstall");
            else
                OptionsDialog.SecondaryButtonText = "";
            OptionsDialog.PrimaryButtonText = Tools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = Tools.Translate("{0} installation options", package.Name);
            OptionsDialog.Content = OptionsPage;

            ContentDialogResult result = await Tools.App.MainWindow.ShowDialogAsync(OptionsDialog);
            OptionsPage.SaveToDisk();

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return result == ContentDialogResult.Secondary;

        }

        public async Task<InstallationOptions> UpdateInstallationSettings(Package package, InstallationOptions options)
        {
            InstallOptionsPage OptionsPage = new(package, options);

            ContentDialog OptionsDialog = new();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            OptionsDialog.SecondaryButtonText = "";
            OptionsDialog.PrimaryButtonText = Tools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = Tools.Translate("{0} installation options", package.Name);
            OptionsDialog.Content = OptionsPage;
            await Tools.App.MainWindow.ShowDialogAsync(OptionsDialog);

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return await OptionsPage.GetUpdatedOptions();

        }

        private void NavigateToPage(Page TargetPage)
        {
            foreach (Page page in PageButtonReference.Keys)
                if (page.Visibility == Visibility.Visible)
                    OldPage = page;
            if (!PageButtonReference.ContainsKey(TargetPage))
            {
                PageButtonReference.Add(TargetPage, MoreNavButton);
                Grid.SetColumn(TargetPage, 0);
                Grid.SetRow(TargetPage, 0);
                MainContentPresenterGrid.Children.Add(TargetPage);
            }
            foreach (NavButton button in Tools.App.MainWindow.NavButtonList)
            {

                button.ToggleButton.IsChecked = (button == PageButtonReference[TargetPage]);
            }

            foreach (Page page in PageButtonReference.Keys)
                page.Visibility = (page == TargetPage) ? Visibility.Visible : Visibility.Collapsed;

            CurrentPage = TargetPage;

            if (CurrentPage == DiscoverPage)
                DiscoverPage.PackageList.Focus(FocusState.Programmatic);
            else if (CurrentPage == UpdatesPage)
                UpdatesPage.PackageList.Focus(FocusState.Programmatic);
            else if (CurrentPage == InstalledPage)
                InstalledPage.PackageList.Focus(FocusState.Programmatic);
            else if (CurrentPage == BundlesPage)
                BundlesPage.PackageList.Focus(FocusState.Programmatic);
        }

        private async void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
        {

            ContentDialog NotesDialog = new();
            NotesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            NotesDialog.XamlRoot = XamlRoot;
            NotesDialog.Resources["ContentDialogMaxWidth"] = 12000;
            NotesDialog.Resources["ContentDialogMaxHeight"] = 10000;
            NotesDialog.CloseButtonText = Tools.Translate("Close");
            NotesDialog.Title = Tools.Translate("Release notes");
            ReleaseNotes notes = new();
            NotesDialog.Content = notes;
            NotesDialog.SizeChanged += (s, e) =>
            {
                notes.MinWidth = ActualWidth - 300;
                notes.MinHeight = ActualHeight - 200;
            };

            await Tools.App.MainWindow.ShowDialogAsync(NotesDialog);

            notes.Dispose();
            notes = null;
            NotesDialog = null;
        }

        public async Task ShowPackageDetails(Package package, OperationType ActionOperation)
        {
            PackageDetailsPage DetailsPage = new(package, ActionOperation);

            ContentDialog DetailsDialog = new();
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

            await Tools.App.MainWindow.ShowDialogAsync(DetailsDialog);

            DetailsDialog.Content = null;
            DetailsDialog = null;

        }

        private void OperationHistoryMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LogPage(LogType.OperationHistory));
        }

        private void ManagerLogsMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LogPage(LogType.ManagerLogs));
        }

        public void WingetUILogs_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LogPage(LogType.WingetUILog));
        }


        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            if(HelpPage == null)
                HelpPage = new HelpDialog();
            NavigateToPage(HelpPage);
        }

        private void QuitWingetUI_Click(object sender, RoutedEventArgs e)
        {
            Tools.App.DisposeAndQuit();
        }
    }
}
