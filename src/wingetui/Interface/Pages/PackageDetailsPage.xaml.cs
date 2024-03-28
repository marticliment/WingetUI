using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Core;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackageDetailsPage : Page
    {
        public AppTools Tools = AppTools.Instance;
        public Package Package;
        private InstallOptionsPage InstallOptionsPage;
        public event EventHandler Close;
        private PackageDetails Info;
        OperationType FutureOperation;
        bool PackageHasScreenshots = false;
        public ObservableCollection<TextBlock> ShowableTags = new();

        private enum LayoutMode
        {
            Normal,
            Wide,
            Unloaded
        }

        private LayoutMode layoutMode = LayoutMode.Unloaded;
        public PackageDetailsPage(Package package, OperationType futureOperation)
        {
            FutureOperation = futureOperation;
            Package = package;

            InitializeComponent();

            InstallOptionsPage = new InstallOptionsPage(package, futureOperation);
            InstallOptionsExpander.Content = InstallOptionsPage;

            SizeChanged += PackageDetailsPage_SizeChanged;

            if (futureOperation == OperationType.None)
                futureOperation = OperationType.Install;

            switch (futureOperation)
            {
                case OperationType.Install:
                    ActionButton.Content = Tools.Translate("Install");
                    break;
                case OperationType.Uninstall:
                    ActionButton.Content = Tools.Translate("Uninstall");
                    break;
                case OperationType.Update:
                    ActionButton.Content = Tools.Translate("Update");
                    break;
            }

            IdTextBlock.Text = package.Id;
            VersionTextBlock.Text = package.Version;
            if (package is UpgradablePackage)
                VersionTextBlock.Text += " - " + Tools.Translate("Update to {0} available").Replace("{0}", (package as UpgradablePackage).NewVersion);
            PackageName.Text = package.Name;
            PackageIcon.Source = new BitmapImage() { UriSource = package.GetIconUrl() };
            SourceNameTextBlock.Text = package.SourceAsString;


            string LoadingString = Tools.Translate("Loading...");
            LoadingIndicator.Visibility = Visibility.Visible;


            HomepageUrlButton.Content = LoadingString;
            PublisherTextBlock.Text = LoadingString;
            AuthorTextBlock.Text = LoadingString;
            LicenseTextBlock.Text = LoadingString;
            LicenseUrlButton.Content = LoadingString;

            DescriptionBox.Text = LoadingString;
            ManifestUrlButton.Content = LoadingString;
            HashTextBlock.Text = LoadingString;
            InstallerUrlButton.Content = LoadingString;
            InstallerTypeTextBlock.Text = LoadingString;
            UpdateDateTextBlock.Text = LoadingString;
            ReleaseNotesBlock.Text = LoadingString;
            InstallerSizeTextBlock.Text = LoadingString;
            DownloadInstallerButton.IsEnabled = false;
            ReleaseNotesUrlButton.Content = LoadingString;

            if (CoreData.IconDatabaseData.ContainsKey(Package.GetIconId()))
            {
                if (CoreData.IconDatabaseData[Package.GetIconId()].images.Count > 0)
                {
                    PackageHasScreenshots = true;
                    IconsExtraBanner.Visibility = Visibility.Visible;
                    ScreenshotsCarroussel.Items.Clear();
                    foreach (string image in CoreData.IconDatabaseData[Package.GetIconId()].images)
                        ScreenshotsCarroussel.Items.Add(new Image() { Source = new BitmapImage(new Uri(image)) });
                }
            }


            _ = LoadInformation();

        }
        public async Task LoadInformation()
        {
            LoadingIndicator.Visibility = Visibility.Visible;

            string NotFound = Tools.Translate("Not available");
            Uri InvalidUri = new("about:blank");
            Info = await Package.Manager.GetPackageDetails(Package);
            AppTools.Log("Received info " + Info);

            string command = "";

            switch (FutureOperation)
            {
                case OperationType.Install:
                    command = Package.Manager.Properties.ExecutableFriendlyName + " " + String.Join(' ', Package.Manager.GetInstallParameters(Package, await InstallationOptions.FromPackageAsync(Package)));
                    break;

                case OperationType.Uninstall:
                    command = Package.Manager.Properties.ExecutableFriendlyName + " " + String.Join(' ', Package.Manager.GetUninstallParameters(Package, await InstallationOptions.FromPackageAsync(Package)));
                    break;  

                case OperationType.Update:
                    command = Package.Manager.Properties.ExecutableFriendlyName + " " + String.Join(' ', Package.Manager.GetUpdateParameters(Package, await InstallationOptions.FromPackageAsync(Package)));
                    break;
            }
            CommandTextBlock.Text = command;

            LoadingIndicator.Visibility = Visibility.Collapsed;

            HomepageUrlButton.Content = Info.HomepageUrl != null ? Info.HomepageUrl : NotFound;
            HomepageUrlButton.NavigateUri = Info.HomepageUrl != null ? Info.HomepageUrl : InvalidUri;
            PublisherTextBlock.Text = Info.Publisher != "" ? Info.Publisher : NotFound;
            AuthorTextBlock.Text = Info.Author != "" ? Info.Author : NotFound;
            LicenseTextBlock.Text = Info.License != "" ? Info.License : NotFound;
            if (Info.License != "" && Info.LicenseUrl != null)
            {
                LicenseTextBlock.Text = Info.License;
                LicenseUrlButton.Content = "(" + Info.LicenseUrl + ")";
                LicenseUrlButton.NavigateUri = Info.LicenseUrl;
            }
            else if (Info.License != "" && Info.LicenseUrl == null)
            {
                LicenseTextBlock.Text = Info.License;
                LicenseUrlButton.Content = "";
                LicenseUrlButton.NavigateUri = InvalidUri;
            }
            else if (Info.License == "" && Info.LicenseUrl != null)
            {
                LicenseTextBlock.Text = "";
                LicenseUrlButton.Content = Info.LicenseUrl;
                LicenseUrlButton.NavigateUri = Info.LicenseUrl;
            }
            else
            {
                LicenseTextBlock.Text = NotFound;
                LicenseUrlButton.Content = "";
                LicenseUrlButton.NavigateUri = InvalidUri;
            }

            DescriptionBox.Text = Info.Description != "" ? Info.Description : NotFound;
            ManifestUrlButton.Content = Info.ManifestUrl != null ? Info.ManifestUrl : NotFound;
            ManifestUrlButton.NavigateUri = Info.ManifestUrl != null ? Info.ManifestUrl : InvalidUri;
            HashTextBlock.Text = Info.InstallerHash != "" ? Info.InstallerHash : NotFound;
            InstallerUrlButton.Content = Info.InstallerUrl != null ? Info.InstallerUrl : NotFound;
            InstallerUrlButton.NavigateUri = Info.InstallerUrl != null ? Info.InstallerUrl : InvalidUri;
            InstallerTypeTextBlock.Text = Info.InstallerType != "" ? Info.InstallerType : NotFound;
            UpdateDateTextBlock.Text = Info.UpdateDate != "" ? Info.UpdateDate : NotFound;
            ReleaseNotesBlock.Text = Info.ReleaseNotes != "" ? Info.ReleaseNotes : NotFound;
            InstallerSizeTextBlock.Text = Info.InstallerSize != 0.0 ? Info.InstallerSize.ToString() + " MB" : NotFound;
            DownloadInstallerButton.IsEnabled = Info.InstallerUrl != null;
            ReleaseNotesUrlButton.Content = Info.ReleaseNotesUrl != null ? Info.ReleaseNotesUrl : NotFound;
            ReleaseNotesUrlButton.NavigateUri = Info.ReleaseNotesUrl != null ? Info.ReleaseNotesUrl : InvalidUri;

            ShowableTags.Clear();
            foreach (string tag in Info.Tags)
                ShowableTags.Add(new TextBlock() { Text = tag });
        }

        public void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, new EventArgs());
            InstallOptionsPage.SaveToDisk();
            switch (FutureOperation)
            {
                case OperationType.Install:
                    Tools.AddOperationToList(new InstallPackageOperation(Package, InstallOptionsPage.Options));
                    break;
                case OperationType.Uninstall:
                    Tools.App.MainWindow.NavigationPage.InstalledPage.ConfirmAndUninstall(Package, InstallOptionsPage.Options);
                    break;
                case OperationType.Update:
                    Tools.AddOperationToList(new UpdatePackageOperation(Package, InstallOptionsPage.Options));
                    break;
            }
        }

        public void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.App.MainWindow.SharePackage(Package);
        }

        public async void DownloadInstallerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Info.InstallerUrl == null)
                    return;

                ErrorOutput.Text = "";
                FileSavePicker savePicker = new();
                MainWindow window = Tools.App.MainWindow;
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                savePicker.SuggestedFileName = Package.Id + " installer." + Info.InstallerUrl.ToString().Split('.')[^1];
                if (Info.InstallerUrl.ToString().Split('.')[^1] == "nupkg")
                    savePicker.FileTypeChoices.Add("Compressed Manifest File", new System.Collections.Generic.List<string>() { ".zip" });
                savePicker.FileTypeChoices.Add("Default", new System.Collections.Generic.List<string>() { "." + Info.InstallerUrl.ToString().Split('.')[^1] });
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    DownloadInstallerButton.Content = Tools.Translate("Downloading");
                    DownloadInstallerButtonProgress.Visibility = Visibility.Visible;
                    AppTools.Log(file.Path.ToString());
                    using HttpClient httpClient = new();
                    await using Stream s = await httpClient.GetStreamAsync(Info.InstallerUrl);
                    await using FileStream fs = File.OpenWrite(file.Path.ToString());
                    await s.CopyToAsync(fs);
                    fs.Dispose();
                    DownloadInstallerButtonProgress.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Process.Start("explorer.exe", "/select," + file.Path.ToString());
                    DownloadInstallerButton.Content = Tools.Translate("Download succeeded");
                }
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
                DownloadInstallerButton.Content = Tools.Translate("An error occurred");
                DownloadInstallerButtonProgress.Visibility = Visibility.Collapsed;
                ErrorOutput.Text = ex.Message;
            }


        }
        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, new EventArgs());
        }

        public void PackageDetailsPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 950)
            {
                if (layoutMode != LayoutMode.Normal)
                {
                    layoutMode = LayoutMode.Normal;

                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(TitlePanel, 0);
                    Grid.SetColumn(BasicInfoPanel, 0);
                    Grid.SetColumn(ScreenshotsPanel, 0);
                    Grid.SetColumn(ActionsPanel, 0);
                    Grid.SetColumn(InstallOptionsBorder, 0);
                    Grid.SetColumn(MoreDataStackPanel, 0);

                    MainGrid.RowDefinitions.Clear();
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(TitlePanel, 0);
                    Grid.SetRow(DescriptionPanel, 1);
                    Grid.SetRow(BasicInfoPanel, 2);
                    Grid.SetRow(ActionsPanel, 3);
                    Grid.SetRow(InstallOptionsBorder, 4);
                    Grid.SetRow(ScreenshotsPanel, 5);
                    Grid.SetRow(MoreDataStackPanel, 6);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();
                    MainGrid.Children.Add(TitlePanel);
                    MainGrid.Children.Add(DescriptionPanel);
                    MainGrid.Children.Add(BasicInfoPanel);
                    MainGrid.Children.Add(ScreenshotsPanel);
                    MainGrid.Children.Add(ActionsPanel);
                    MainGrid.Children.Add(InstallOptionsBorder);
                    MainGrid.Children.Add(MoreDataStackPanel);
                    ScreenshotsCarroussel.Height = PackageHasScreenshots ? 225 : 150;

                    InstallOptionsExpander.IsExpanded = false;

                }
            }
            else
            {
                if (layoutMode != LayoutMode.Wide)
                {
                    layoutMode = LayoutMode.Wide;

                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 550 });
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(LeftPanel, 0);
                    Grid.SetColumn(RightPanel, 1);
                    Grid.SetColumn(TitlePanel, 0);
                    Grid.SetColumnSpan(TitlePanel, 1);

                    MainGrid.RowDefinitions.Clear();
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(LeftPanel, 1);
                    Grid.SetRow(RightPanel, 0);
                    Grid.SetRow(TitlePanel, 0);
                    Grid.SetRowSpan(RightPanel, 2);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();
                    LeftPanel.Children.Add(DescriptionPanel);
                    LeftPanel.Children.Add(BasicInfoPanel);
                    RightPanel.Children.Add(ScreenshotsPanel);
                    LeftPanel.Children.Add(ActionsPanel);
                    LeftPanel.Children.Add(InstallOptionsBorder);
                    RightPanel.Children.Add(MoreDataStackPanel);
                    ScreenshotsCarroussel.Height = PackageHasScreenshots ? 400 : 150;

                    InstallOptionsExpander.IsExpanded = true;

                    MainGrid.Children.Add(LeftPanel);
                    MainGrid.Children.Add(RightPanel);
                    MainGrid.Children.Add(TitlePanel);

                }
            }
        }
    }
}
