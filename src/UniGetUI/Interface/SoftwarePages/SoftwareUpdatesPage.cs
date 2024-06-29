using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class SoftwareUpdatesPage : AbstractPackagesPage
    {
        private BetterMenuItem? MenuAsAdmin;
        private BetterMenuItem? MenuInteractive;
        private BetterMenuItem? MenuskipHash;

        public SoftwareUpdatesPage()
        : base(new PackagesPageData()
        {
            DisableAutomaticPackageLoadOnStart = false,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = true,
            PackagesAreCheckedByDefault = true,
            DisableSuggestedResultsRadio = true,
            PageName = "Updates",

            Loader = PEInterface.UpgradablePackagesLoader,
            PageRole = OperationType.Update,

            NoPackages_BackgroundText = CoreTools.Translate("Hooray! No updates were found."),
            NoPackages_SourcesText = CoreTools.Translate("Everything is up to date"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("Everything is up to date"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Software Updates"),
            Glyph = "\uE895"
        })
        {
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu ContextMenu = new();

            BetterMenuItem menuInstall = new()
            {
                Text = "Update",
                IconName="menu_updates",
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuInstall.Click += MenuInstall_Invoked;

            BetterMenuItem menuInstallSettings = new()
            {
                Text = "Installation options",
                IconName="options",
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += (s, e) => { 
                ShowInstallationOptionsForPackage(SelectedItem); 
            };
            
            MenuAsAdmin = new BetterMenuItem()
            {
                Text = "Update as administrator",
                IconName="runasadmin",
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;

            MenuInteractive = new BetterMenuItem()
            {
                Text = "Interactive update",
                IconName="interactive",
            };
            MenuInteractive.Click += MenuInteractive_Invoked;

            MenuskipHash = new BetterMenuItem()
            {
                Text = "Skip hash check",
                IconName="checksum",
            };
            MenuskipHash.Click += MenuSkipHash_Invoked;

            BetterMenuItem menuUpdateAfterUninstall = new()
            {
                Text = "Uninstall package, then update it",
                IconName="undelete",
            };
            menuUpdateAfterUninstall.Click += MenuUpdateAfterUninstall_Invoked;

            BetterMenuItem menuUninstall = new()
            {
                Text = "Uninstall package",
                IconName="trash",
            };
            menuUninstall.Click += MenuUninstall_Invoked;


            BetterMenuItem menuIgnorePackage = new()
            {
                Text = "Ignore updates for this package",
                IconName="pin",
            };
            menuIgnorePackage.Click += MenuIgnorePackage_Invoked;

            BetterMenuItem menuSkipVersion = new()
            {
                Text = "Skip this version",
                IconName="skip",
            };
            menuSkipVersion.Click += MenuSkipVersion_Invoked;

            BetterMenuItem menuShare = new()
            {
                Text = "Share this package",
                IconName="share",
            };
            menuShare.Click += (o, e) => SharePackage(SelectedItem);

            BetterMenuItem menuDetails = new()
            {
                Text = "Package details",
                IconName="info",
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += (o, e) => ShowDetailsForPackage(SelectedItem);

            ContextMenu.Items.Add(menuInstall);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuInstallSettings);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(MenuAsAdmin);
            ContextMenu.Items.Add(MenuInteractive);
            ContextMenu.Items.Add(MenuskipHash);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuUpdateAfterUninstall);
            ContextMenu.Items.Add(menuUninstall);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuIgnorePackage);
            ContextMenu.Items.Add(menuSkipVersion);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuShare);
            ContextMenu.Items.Add(menuDetails);

            return ContextMenu;
        }

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if(MenuAsAdmin == null || MenuInteractive == null || MenuskipHash == null)
            {
                Logger.Error("Menu items are null on SoftwareUpdatesTab");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuskipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
        }

        public override void GenerateToolBar()
        {
            AppBarButton UpdateSelected = new();
            AppBarButton UpdateAsAdmin = new();
            AppBarButton UpdateSkipHash = new();
            AppBarButton UpdateInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton IgnoreSelected = new();
            AppBarButton ManageIgnored = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(UpdateSelected);
            ToolBar.PrimaryCommands.Add(UpdateAsAdmin);
            ToolBar.PrimaryCommands.Add(UpdateSkipHash);
            ToolBar.PrimaryCommands.Add(UpdateInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(IgnoreSelected);
            ToolBar.PrimaryCommands.Add(ManageIgnored);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { UpdateSelected,       CoreTools.Translate("Update selected packages") },
                { UpdateAsAdmin,        " " + CoreTools.Translate("Update as administrator") },
                { UpdateSkipHash,       " " + CoreTools.Translate("Skip integrity checks") },
                { UpdateInteractive,    " " + CoreTools.Translate("Interactive update") },
                { InstallationSettings, " " + CoreTools.Translate("Installation options") },
                { PackageDetails,       " " + CoreTools.Translate("Package details") },
                { SharePackage,         " " + CoreTools.Translate("Share") },
                { IgnoreSelected,       CoreTools.Translate("Ignore selected packages") },
                { ManageIgnored,        CoreTools.Translate("Manage ignored updates") },
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
                { UpdateSelected,       "menu_updates" },
                { UpdateAsAdmin,        "runasadmin" },
                { UpdateSkipHash,       "checksum" },
                { UpdateInteractive,    "interactive" },
                { InstallationSettings, "options" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { IgnoreSelected,       "pin" },
                { ManageIgnored,        "clipboard_list" },
                { HelpButton,           "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);


            PackageDetails.Click += (s, e) => ShowDetailsForPackage(SelectedItem);
            HelpButton.Click += (s, e) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (s, e) => ShowInstallationOptionsForPackage(SelectedItem);
            ManageIgnored.Click += async (s, e) => await MainApp.Instance.MainWindow.NavigationPage.ManageIgnoredUpdatesDialog();
            IgnoreSelected.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    await package.AddToIgnoredUpdatesAsync();
                    PEInterface.UpgradablePackagesLoader.Remove(package);
                }
            };

            UpdateSelected.Click += (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
                }
            };

            UpdateAsAdmin.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, elevated: true);
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package, options));
                }
            };
            
            UpdateSkipHash.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, no_integrity: true);
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package, options));
                }
            };
            
            UpdateInteractive.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, interactive: true);
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package, options));
                }
            };

            SharePackage.Click += (s, e) => MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        protected override void WhenPackageCountUpdated()
        {
            try
            {
                MainApp.Instance.TooltipStatus.AvailableUpdates = Loader.Packages.Count();
            }
            catch (Exception)
            { }
        }

        public void UpdateAll()
        {
            foreach (IPackage package in Loader.Packages)
            {
                if (package.Tag != PackageTag.BeingProcessed && package.Tag != PackageTag.OnQueue)
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            }
        }

        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            List<Package> upgradablePackages = new();
            foreach (Package package in Loader.Packages)
            {
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    upgradablePackages.Add(package);
            }

            if (upgradablePackages.Count > 0)
            {
                string body;
                string title;
                string attribution = "";
                bool ShowButtons = false;
                if (Settings.Get("AutomaticallyUpdatePackages") || Environment.GetCommandLineArgs().Contains("--updateapps"))
                {
                    if (upgradablePackages.Count == 1)
                    {
                        title = CoreTools.Translate("An update was found!");
                        body = CoreTools.Translate("{0} is being updated to version {1}", upgradablePackages[0].Name, upgradablePackages[0].NewVersion);
                        attribution = CoreTools.Translate("You have currently version {0} installed", upgradablePackages[0].Version);
                    }
                    else
                    {
                        title = CoreTools.Translate("Updates found!");
                        body = CoreTools.Translate("{0} packages are being updated", upgradablePackages.Count); ;
                        foreach (IPackage package in upgradablePackages)
                        {
                            attribution += package.Name + ", ";
                        }
                        attribution = attribution.TrimEnd(' ').TrimEnd(',');
                    }
                    UpdateAll();
                }
                else
                {
                    if (upgradablePackages.Count == 1)
                    {
                        title = CoreTools.Translate("An update was found!");
                        body = CoreTools.Translate("{0} can be updated to version {1}", upgradablePackages[0].Name, upgradablePackages[0].NewVersion);
                        attribution = CoreTools.Translate("You have currently version {0} installed", upgradablePackages[0].Version);
                    }
                    else
                    {
                        title = CoreTools.Translate("Updates found!");
                        body = CoreTools.Translate("{0} packages can be updated", upgradablePackages.Count); ;
                        foreach (IPackage package in upgradablePackages)
                        {
                            attribution += package.Name + ", ";
                        }
                        attribution = attribution.TrimEnd(' ').TrimEnd(',');
                    }
                    ShowButtons = true;
                }

                if (!Settings.Get("DisableUpdatesNotifications") && !Settings.Get("DisableNotifications"))
                {
                    try
                    {

                        ToastContentBuilder toast = new();
                        toast.AddArgument("action", "openUniGetUIOnUpdatesTab");
                        toast.AddArgument("notificationId", CoreData.UpdatesAvailableNotificationId);
                        toast.AddText(title);
                        toast.AddText(body);
                        toast.AddAttributionText(attribution);
                        if (ShowButtons)
                        {
                            toast.AddButton(new ToastButton()
                                .SetContent(CoreTools.Translate("Open WingetUI"))
                                .AddArgument("action", "openUniGetUIOnUpdatesTab")
                                .SetBackgroundActivation());
                            toast.AddButton(new ToastButton()
                                .SetContent(upgradablePackages.Count == 1 ? CoreTools.Translate("Update") : CoreTools.Translate("Update all"))
                                .AddArgument("action", "updateAll")
                                .SetBackgroundActivation());
                        }
                        toast.Show();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("An error occurred when showing a toast notification regarding available updates");
                        Logger.Warn(ex);
                    }
                }
            }
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
        }

        private async void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, no_integrity: true)));
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, interactive: true)));
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, elevated: true)));
        }

        private void MenuUpdateAfterUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package, IgnoreParallelInstalls: true));
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, IgnoreParallelInstalls: true));
        }

        private void MenuUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package));
        }

        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            _ = package.AddToIgnoredUpdatesAsync();
            PEInterface.UpgradablePackagesLoader.Remove(package);
        }

        private void MenuSkipVersion_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            _ = package.AddToIgnoredUpdatesAsync((package).NewVersion);
            PEInterface.UpgradablePackagesLoader.Remove(package);
        }

        public void UpdatePackageForId(string id)
        {
            foreach (IPackage package in Loader.Packages)
            {
                if (package.Id == id)
                {
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
                    Logger.Info($"[WIDGETS] Updating package with id {id}");
                    break;
                }
            }
            Logger.Warn($"[WIDGETS] No package with id={id} was found");
        }

        public void UpdateAllPackagesForManager(string manager)
        {
            foreach (IPackage package in Loader.Packages)
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    if (package.Manager.Name == manager)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
        }
    }
}
