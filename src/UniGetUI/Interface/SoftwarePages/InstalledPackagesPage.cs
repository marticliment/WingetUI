using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.SoftwarePages.UniGetUI.Interface.SoftwarePages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class InstalledPackagesPage : AbstractPackagesPage
    {
        bool HasDoneBackup = false;

        BetterMenuItem? MenuAsAdmin;
        BetterMenuItem? MenuInteractive;
        BetterMenuItem? MenuRemoveData;

        public InstalledPackagesPage()
        : base(new PackagesPageData()
        {
            DisableAutomaticPackageLoadOnStart = false,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = false,
            PackagesAreCheckedByDefault = false,
            DisableSuggestedResultsRadio = true,
            PageName = "Installed",

            Loader = PEInterface.InstalledPackagesLoader,
            PageRole = OperationType.Uninstall,

            NoPackages_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),
            NoPackages_SourcesText = CoreTools.Translate("No packages were found"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("No packages were found"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Installed Packages"),
            Glyph = "\uE977"
        })
        {
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();
            BetterMenuItem menuUninstall = new()
            {
                Text = "Uninstall",
                IconName = "trash",
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuUninstall.Click += MenuUninstall_Invoked;
            menu.Items.Add(menuUninstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            BetterMenuItem menuInstallSettings = new()
            {
                Text = "Installation options",
                IconName = "options",
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(menuInstallSettings);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuAsAdmin = new BetterMenuItem
            {
                Text = "Uninstall as administrator",
                IconName = "runasadmin"
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = "Interactive uninstall",
                IconName = "interactive"
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuRemoveData = new BetterMenuItem
            {
                Text = "Uninstall and remove data",
                IconName = "menu_close"
            };
            MenuRemoveData.Click += MenuRemoveData_Invoked;
            menu.Items.Add(MenuRemoveData);

            menu.Items.Add(new MenuFlyoutSeparator());

            BetterMenuItem menuReinstall = new()
            {
                Text = "Reinstall package",
                IconName = "newversion"
            };
            menuReinstall.Click += MenuReinstall_Invoked;
            menu.Items.Add(menuReinstall);

            BetterMenuItem menuUninstallThenReinstall = new()
            {
                Text = "Uninstall package, then reinstall it",
                IconName = "undelete"
            };
            menuUninstallThenReinstall.Click += MenuUninstallThenReinstall_Invoked;
            menu.Items.Add(menuUninstallThenReinstall);

            menu.Items.Add(new MenuFlyoutSeparator());

            BetterMenuItem menuIgnorePackage = new()
            {
                Text = "Ignore updates for this package",
                IconName = "pin"
            };
            menuIgnorePackage.Click += MenuIgnorePackage_Invoked;
            menu.Items.Add(menuIgnorePackage);

            menu.Items.Add(new MenuFlyoutSeparator());

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
            AppBarButton UninstallSelected = new();
            AppBarButton UninstallAsAdmin = new();
            AppBarButton UninstallInteractive = new();
            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton IgnoreSelected = new();
            AppBarButton ManageIgnored = new();
            AppBarButton ExportSelection = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(UninstallSelected);
            ToolBar.PrimaryCommands.Add(UninstallAsAdmin);
            ToolBar.PrimaryCommands.Add(UninstallInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(IgnoreSelected);
            ToolBar.PrimaryCommands.Add(ManageIgnored);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(ExportSelection);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { UninstallSelected,    CoreTools.Translate("Uninstall selected packages") },
                { UninstallAsAdmin,     " " + CoreTools.Translate("Uninstall as administrator") },
                { UninstallInteractive, " " + CoreTools.Translate("Interactive uninstall") },
                { InstallationSettings, " " + CoreTools.Translate("Installation options") },
                { PackageDetails,       " " + CoreTools.Translate("Package details") },
                { SharePackage,         " " + CoreTools.Translate("Share") },
                { IgnoreSelected,       CoreTools.Translate("Ignore selected packages") },
                { ManageIgnored,        CoreTools.Translate("Manage ignored updates") },
                { ExportSelection,      CoreTools.Translate("Add selection to bundle") },
                { HelpButton,           CoreTools.Translate("Help") }
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
                { UninstallSelected,      "trash" },
                { UninstallAsAdmin,       "runasadmin" },
                { UninstallInteractive,   "interactive" },
                { InstallationSettings,   "options" },
                { PackageDetails,         "info" },
                { SharePackage,           "share" },
                { IgnoreSelected,         "pin" },
                { ManageIgnored,          "clipboard_list" },
                { ExportSelection,        "add_to" },
                { HelpButton,             "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) => ShowDetailsForPackage(SelectedItem);

            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (s, e) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (s, e) => ShowInstallationOptionsForPackage(SelectedItem);
            ManageIgnored.Click += async (s, e) => await MainApp.Instance.MainWindow.NavigationPage.ManageIgnoredUpdatesDialog();
            IgnoreSelected.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    PEInterface.UpgradablePackagesLoader.Remove(package);
                    await package.AddToIgnoredUpdatesAsync();
                }
            };

            UninstallSelected.Click += (s, e) => ConfirmAndUninstall(FilteredPackages.GetCheckedPackages());
            UninstallAsAdmin.Click += (s, e) => ConfirmAndUninstall(FilteredPackages.GetCheckedPackages(), elevated: true);
            UninstallInteractive.Click += (s, e) => ConfirmAndUninstall(FilteredPackages.GetCheckedPackages(), interactive: true);
            SharePackage.Click += (s, e) =>  MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        protected override void WhenPackageCountUpdated()
        {
            return;
        }

#pragma warning disable
        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            if (!HasDoneBackup)
            {
                if (Settings.Get("EnablePackageBackup"))
                    _ = BackupPackages();
            }
        }
#pragma warning restore

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if(MenuAsAdmin == null || MenuInteractive == null || MenuRemoveData == null)
            {
                Logger.Error("Menu items are null on InstalledPackagesTab");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuRemoveData.IsEnabled = package.Manager.Capabilities.CanRemoveDataOnUninstall;
        }

        private void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.BundlesNavButton.ForceClick();
            PEInterface.PackageBundlesLoader.AddPackages(FilteredPackages.GetCheckedPackages());
        }

        public async void ConfirmAndUninstall(IPackage package, IInstallationOptions options)
        {
            if (await MainApp.Instance.MainWindow.NavigationPage.ConfirmUninstallation(package))
            {
                MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package, options));
            }
        }

        public async void ConfirmAndUninstall(IEnumerable<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? remove_data = null)
        {
            if (await MainApp.Instance.MainWindow.NavigationPage.ConfirmUninstallation(packages))
            {
                foreach (IPackage package in packages)
                {
                    MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package,
                        await InstallationOptions.FromPackageAsync(package, elevated, interactive, remove_data: remove_data)));
                }
            }
        }

        public async Task BackupPackages()
        {

            try
            {
                // TODO: FIXME
                Logger.Debug("Starting package backup");
                string BackupContents = await NewPackageBundlesPage.CreateBundle(Loader.Packages, BundleFormatType.JSON);

                string dirName = Settings.GetValue("ChangeBackupOutputDirectory");
                if (dirName == "")
                    dirName = CoreData.UniGetUI_DefaultBackupDirectory;

                if (!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                string fileName = Settings.GetValue("ChangeBackupFileName");
                if (fileName == "")
                    fileName = CoreTools.Translate("{pcName} installed packages", new Dictionary<string, object?>{ { "pcName", Environment.MachineName } });

                if (Settings.Get("EnableBackupTimestamping"))
                    fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

                fileName += ".json";

                string filePath = Path.Combine(dirName, fileName);
                await File.WriteAllTextAsync(filePath, BackupContents);
                HasDoneBackup = true;
                Logger.ImportantInfo("Backup saved to " + filePath);
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while performing a backup");
                Logger.Error(ex);
            }
        }

        private async void MenuUninstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package));
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;
            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package, elevated: true));
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package, interactive: true));
        }

        private async void MenuRemoveData_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package, remove_data: true));
        }

        private void MenuReinstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
        }

        private void MenuUninstallThenReinstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package, IgnoreParallelInstalls: true));
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, IgnoreParallelInstalls: true));

        }
        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            _ = package.AddToIgnoredUpdatesAsync();
            PEInterface.UpgradablePackagesLoader.Remove(package);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs args)
        {
            if (PackageList.SelectedItem == null) return;
            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs args)
        {
            ShowDetailsForPackage(SelectedItem);
        }

        private async void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package != null && 
                await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(package, OperationType.Uninstall))
            {
                ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package));
            }
        }
    }
}