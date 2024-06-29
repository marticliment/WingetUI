using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using Windows.System;

namespace UniGetUI.Interface.SoftwarePages
{
    public class DiscoverSoftwarePage : AbstractPackagesPage
    {
        BetterMenuItem? MenuAsAdmin;
        BetterMenuItem? MenuInteractive;
        BetterMenuItem? MenuSkipHash;
        public DiscoverSoftwarePage()
        : base(new PackagesPageData()
        {
            DisableAutomaticPackageLoadOnStart = true,
            MegaQueryBlockEnabled = true,
            PackagesAreCheckedByDefault = false,
            ShowLastLoadTime = false,
            DisableSuggestedResultsRadio = false,
            PageName = "Discover",

            Loader = PEInterface.DiscoveredPackagesLoader,
            PageRole = OperationType.Install,

            NoPackages_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),
            NoPackages_SourcesText = CoreTools.Translate("No packages were found"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("No packages were found"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Discover Packages"),
            Glyph = "\uF6FA"
        })
        {   
            InstantSearchCheckbox.IsEnabled = false;
            InstantSearchCheckbox.Visibility = Visibility.Collapsed;

            FindButton.Click += Event_SearchPackages;
            MegaFindButton.Click += Event_SearchPackages;

            QueryBlock.KeyUp += (s, e) => { if (e.Key == VirtualKey.Enter) Event_SearchPackages(s, e); };
            MegaQueryBlock.KeyUp += (s, e) => { if (e.Key == VirtualKey.Enter) Event_SearchPackages(s, e); };
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();

            BetterMenuItem menuInstall = new()
            {
                Text = "Install",
                IconName = "newversion",
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuInstall.Click += MenuInstall_Invoked;
            menu.Items.Add(menuInstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            BetterMenuItem menuInstallSettings = new()
            {
                Text = "Installation options",
                IconName = "options",
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(menuInstallSettings);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuAsAdmin = new BetterMenuItem
            {
                Text = "Install as administrator",
                IconName = "runasadmin"
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = "Interactive installation",
                IconName = "interactive"
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuSkipHash = new BetterMenuItem
            {
                Text = "Skip hash check",
                IconName = "checksum"
            };
            MenuSkipHash.Click += MenuSkipHash_Invoked;
            menu.Items.Add(MenuSkipHash);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            BetterMenuItem menuShare = new()
            {
                Text = "Share this package",
                IconName = "share"
            };
            menuShare.Click += MenuShare_Invoked;
            menu.Items.Add(menuShare);

            BetterMenuItem menuDetails = new()
            {
                Text = "Package details",
                IconName = "info",
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(menuDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            AppBarButton InstallSelected = new();
            AppBarButton InstallAsAdmin = new();
            AppBarButton InstallSkipHash = new();
            AppBarButton InstallInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton ExportSelection = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(InstallSelected);
            ToolBar.PrimaryCommands.Add(InstallAsAdmin);
            ToolBar.PrimaryCommands.Add(InstallSkipHash);
            ToolBar.PrimaryCommands.Add(InstallInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(ExportSelection);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            {   // Entries with a trailing space are collapsed
                // Their texts will be used as the tooltip
                { InstallSelected,        CoreTools.Translate("Install selected packages") },
                { InstallAsAdmin,         " " + CoreTools.Translate("Install as administrator") },
                { InstallSkipHash,        " " + CoreTools.Translate("Skip integrity checks") },
                { InstallInteractive,     " " + CoreTools.Translate("Interactive installation") },
                { InstallationSettings,   CoreTools.Translate("Installation options") },
                { PackageDetails,         " " + CoreTools.Translate("Package details") },
                { SharePackage,           " " + CoreTools.Translate("Share") },
                { ExportSelection,        CoreTools.Translate("Add selection to bundle") },
                { HelpButton,             CoreTools.Translate("Help") }
            };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = Labels[toolButton].Trim();
            }

            Dictionary<AppBarButton, string> Icons = new()
            {
                { InstallSelected,      "install" },
                { InstallAsAdmin,       "runasadmin" },
                { InstallSkipHash,      "checksum" },
                { InstallInteractive,   "interactive" },
                { InstallationSettings, "options" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { ExportSelection,      "add_to" },
                { HelpButton,           "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
            {
                toolButton.Icon = new LocalIcon(Icons[toolButton]);
            }

            PackageDetails.Click += (s, e) => ShowDetailsForPackage(SelectedItem);
            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (s, e) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (s, e) => ShowInstallationOptionsForPackage(SelectedItem);

            InstallSelected.Click += (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
            };

            InstallAsAdmin.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, elevated: true);
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, options));
                }
            };

            InstallSkipHash.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, no_integrity: true);
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, options));
                }
            };

            InstallInteractive.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, interactive: true);
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, options));
                }
            };

            SharePackage.Click += (s, e) => MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        public override async Task LoadPackages()
        {
            if(QueryBlock.Text.Trim() != "") await LoadPackages(ReloadReason.External);
        }

        private void Event_SearchPackages(object sender, RoutedEventArgs e)
        {
            if (QueryBlock.Text.Trim() != "")
                _ = (Loader as DiscoverablePackagesLoader)?.ReloadPackages(QueryBlock.Text.Trim());
            else
                Loader.StopLoading();
        }

        protected override void WhenPackageCountUpdated()
        {
            return;
        }

#pragma warning disable
        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            return;
        }
#pragma warning restore

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin == null || MenuInteractive == null || MenuSkipHash == null)
            {
                Logger.Warn("MenuItems are null on DiscoverPackagesPage");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuSkipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
        }

        private void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.BundlesNavButton.ForceClick();
            PEInterface.PackageBundlesLoader.AddPackages(FilteredPackages.GetCheckedPackages());
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs e)
        {
            ShowDetailsForPackage(SelectedItem);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs e)
        {
            if (PackageList.SelectedItem == null) return;
            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
        }

        private async void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, no_integrity: true)));
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, interactive: true)));
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;
            
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, elevated: true)));
        }

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            ShowInstallationOptionsForPackage(SelectedItem);
        }

        public void ShowSharedPackage_ThreadSafe(string pId, string pSource)
        {
            MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() => { ShowSharedPackage(pId, pSource); });
        }

        private async void ShowSharedPackage(string pId, string pSource)
        {
            Logger.Info($"Showing shared package with pId=${pId} and pSource=${pSource}...");

            MainApp.Instance.MainWindow.Activate();

            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Please wait...", pId));
            QueryIdRadio.IsChecked = true;
            QueryBlock.Text = pId;
            await LoadPackages();
            QueryBothRadio.IsChecked = true;
            MainApp.Instance.MainWindow.HideLoadingDialog();
            if (FilteredPackages.Count == 1)
            {
                Logger.Debug("Only one package was found for pId=" + pId + ", showing it.");
                await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(FilteredPackages[0].Package, OperationType.Install);
            }
            else if (FilteredPackages.Count > 1)
            {
                string managerName = pSource.Contains(':') ? pSource.Split(':')[0] : pSource;
                foreach (IPackage match in FilteredPackages.GetPackages())
                    if (match.Source.Manager.Name == managerName)
                    {
                        Logger.Debug("Equivalent package for pId=" + pId + " and pSource=" + pSource + " found: " + match.ToString());
                        await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(match, OperationType.Install);
                        return;
                    }
                Logger.Debug("No package found with the exact same manager, showing the first one.");
                await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(FilteredPackages[0].Package, OperationType.Install);
            }
            else
            {
                Logger.Error("No packages were found matching the given pId=" + pId);
                ContentDialog c = new();
                c.XamlRoot = XamlRoot;
                c.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                c.Title = CoreTools.Translate("Package not found");
                c.Content = CoreTools.Translate("The package {0} from {1} was not found.", pId, pSource);
                c.PrimaryButtonText = CoreTools.Translate("OK");
                c.DefaultButton = ContentDialogButton.Primary;
                await MainApp.Instance.MainWindow.ShowDialogAsync(c);
            }
        }

    }
}
