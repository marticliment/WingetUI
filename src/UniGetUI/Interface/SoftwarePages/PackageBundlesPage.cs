using System.Diagnostics;
using System.Text.Json;
using System.Xml.Serialization;
using ExternalLibraries.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class PackageBundlesPage : AbstractPackagesPage
    {
        BetterMenuItem? MenuAsAdmin;
        BetterMenuItem? MenuInteractive;
        BetterMenuItem? MenuSkipHash;

        public PackageBundlesPage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = true,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = false,
            PackagesAreCheckedByDefault = false,
            DisableSuggestedResultsRadio = true,
            PageName = "Bundles",

            Loader = PEInterface.PackageBundlesLoader,
            PageRole = OperationType.Install,

            NoPackages_BackgroundText = CoreTools.Translate("Add packages or open an existing package bundle"),
            NoPackages_SourcesText = CoreTools.Translate("Add packages to start"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("The current bundle has no packages. Add some packages to get started"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Package Bundles"),
            Glyph = "\uF133"
        }) 
        {
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();
            BetterMenuItem menuUninstall = new()
            {
                Text = "Install",
                IconName = IconType.Download,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuUninstall.Click += MenuUninstall_Invoked;
            menu.Items.Add(menuUninstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            BetterMenuItem menuInstallSettings = new()
            {
                Text = "Installation options",
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(menuInstallSettings);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuAsAdmin = new BetterMenuItem
            {
                Text = "Install as administrator",
                IconName = IconType.UAC
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = "Interactive installation",
                IconName = IconType.Interactive
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuSkipHash = new BetterMenuItem
            {
                Text = "Skip hash checks",
                IconName = IconType.Checksum
            };
            MenuSkipHash.Click += MenuSkipHash_Invoked;
            menu.Items.Add(MenuSkipHash);

            menu.Items.Add(new MenuFlyoutSeparator());

            BetterMenuItem menuRemoveFromList = new()
            {
                Text = "Remove from list",
                IconName = IconType.Delete
            };
            menuRemoveFromList.Click += MenuRemoveFromList_Invoked;
            menu.Items.Add(menuRemoveFromList);
            menu.Items.Add(new MenuFlyoutSeparator());

            BetterMenuItem menuShare = new()
            {
                Text = "Share this package",
                IconName = IconType.Share
            };
            menuShare.Click += MenuShare_Invoked;
            menu.Items.Add(menuShare);

            BetterMenuItem menuDetails = new()
            {
                Text = "Package details",
                IconName = IconType.Info_Round,
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(menuDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            AppBarButton OpenBundle = new();
            AppBarButton NewBundle = new();
            AppBarButton InstallPackages = new();
            AppBarButton InstallAsAdmin = new();
            AppBarButton InstallInteractive = new();
            AppBarButton InstallSkipHash = new();
            AppBarButton RemoveSelected = new();
            AppBarButton ExportBundle = new();
            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();
            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(NewBundle);
            ToolBar.PrimaryCommands.Add(OpenBundle);
            ToolBar.PrimaryCommands.Add(ExportBundle);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallPackages);
            ToolBar.PrimaryCommands.Add(InstallAsAdmin);
            ToolBar.PrimaryCommands.Add(InstallInteractive);
            ToolBar.PrimaryCommands.Add(InstallSkipHash);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(RemoveSelected);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
                { // Entries with a trailing space are collapsed
                  // Their texts will be used as the tooltip
                    { NewBundle,              CoreTools.Translate("New bundle") },
                    { InstallPackages,        CoreTools.Translate("Install selection") },
                    { InstallAsAdmin,     " " + CoreTools.Translate("Uninstall as administrator") },
                    { InstallInteractive, " " + CoreTools.Translate("Interactive uninstall") },
                    { InstallSkipHash, " " + CoreTools.Translate("Skip integrity checks") },
                    { OpenBundle,             CoreTools.Translate("Open existing bundle") },
                    { RemoveSelected,         CoreTools.Translate("Remove selection from bundle") },
                    { ExportBundle,           CoreTools.Translate("Save bundle as") },
                    { PackageDetails,         " " + CoreTools.Translate("Package details") },
                    { SharePackage,           " " + CoreTools.Translate("Share") },
                    { HelpButton,             CoreTools.Translate("Help") }
                };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = Labels[toolButton].Trim();
            }

            Dictionary<AppBarButton, IconType> Icons = new()
                {
                    { NewBundle,              IconType.AddTo },
                    { InstallPackages,        IconType.Download },
                    { InstallAsAdmin,         IconType.UAC },
                    { InstallInteractive,     IconType.Interactive },
                    { InstallSkipHash,        IconType.Checksum },
                    { OpenBundle,             IconType.OpenFolder },
                    { RemoveSelected,         IconType.Delete},
                    { ExportBundle,           IconType.SaveAs },
                    { PackageDetails,         IconType.Info_Round },
                    { SharePackage,           IconType.Share },
                    { HelpButton,             IconType.Help }
                };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) =>
            {
                IPackage? package = SelectedItem as IPackage;
                if (package != null)
                    _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package, OperationType.None);
            };

            HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

            NewBundle.Click += (s, e) =>
            {
                Loader.ClearPackages();
            };

            RemoveSelected.Click += (s, e) => PEInterface.PackageBundlesLoader.RemoveRange(FilteredPackages.GetCheckedPackages());

            InstallPackages.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages());
            InstallSkipHash.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), skiphash: true);
            InstallInteractive.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), interactive: true);
            InstallAsAdmin.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), elevated: true);


            OpenBundle.Click += async (s, e) =>
            {
                Loader.ClearPackages();
                await OpenFile();
            };

            ExportBundle.Click += async (s, e) =>
            {
                await SaveFile();
            };

            SharePackage.Click += (s, e) =>
            {
                IPackage? package = SelectedItem as IPackage;
                if (package != null) MainApp.Instance.MainWindow.SharePackage(package);
            };

        }

        public async Task ImportAndInstallPackage(IEnumerable<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? skiphash = null)
        {
            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Preparing packages, please wait..."));
            List<Package> packages_to_install = new();
            foreach (IPackage package in packages)
            {
                if(package is ImportedPackage imported)
                { 
                    Logger.ImportantInfo($"Registering package {imported.Id} from manager {imported.Source.AsString}");
                    packages_to_install.Add(await imported.RegisterAndGetPackageAsync());
                }
                else
                {
                    Logger.Warn($"Attempted to install an invalid/incompatible package with Id={package.Id}");
                }
            }

            MainApp.Instance.MainWindow.HideLoadingDialog();

            foreach (Package package in packages_to_install)
            {
               MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                    await InstallationOptions.FromPackageAsync(package, elevated, interactive, skiphash)));
            }
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
                Logger.Error("Menu items are null on InstalledPackagesTab");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuSkipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
        }

        private async void MenuUninstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            await ImportAndInstallPackage([package]);
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;
            await ImportAndInstallPackage([package], elevated: true);
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            await ImportAndInstallPackage([package], interactive: true);

        }
        private async void MenuSkipHash_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            await ImportAndInstallPackage([package], skiphash: true);
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
            if (package is ImportedPackage imported)
            {
                await MainApp.Instance.MainWindow.NavigationPage.ShowInstallOptionsDialog_ImportedPackage(imported);
            }
        }

        private void MenuRemoveFromList_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;
            Loader.Remove(package);
        }


        public async Task OpenFile()
        {
            try
            {
                // Select file
                FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                string file = picker.Show(new List<string> { "*.json", "*.yaml", "*.xml" });
                if (file == String.Empty)
                    return;

                MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Loading packages, please wait..."));

                // Read file
                BundleFormatType formatType;
                if (file.Split('.')[^1].ToLower() == "yaml")
                    formatType = BundleFormatType.YAML;
                else if (file.Split('.')[^1].ToLower() == "xml")
                    formatType = BundleFormatType.XML;
                else
                    formatType = BundleFormatType.JSON;

                string fileContent = await File.ReadAllTextAsync(file);

                // Import packages to list
                await AddFromBundle(fileContent, formatType);

                MainApp.Instance.MainWindow.HideLoadingDialog();

            }
            catch (Exception ex)
            {
                Logger.Error("Could not load packages from a file");
                Logger.Error(ex);
                MainApp.Instance.MainWindow.HideLoadingDialog();
            }
        }

        public async Task SaveFile()
        {
            try
            {
                // Get file 
                // Save file
                string file = (new FileSavePicker(MainApp.Instance.MainWindow.GetWindowHandle())).Show(new List<string> { "*.json", "*.yaml", "*.xml" }, CoreTools.Translate("Package bundle") + ".json");
                if (file != String.Empty)
                {
                    // Loading dialog
                    MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Saving packages, please wait..."));

                    // Select appropriate format
                    BundleFormatType formatType;
                    if (file.Split('.')[^1].ToLower() == "yaml")
                        formatType = BundleFormatType.YAML;
                    else if (file.Split('.')[^1].ToLower() == "xml")
                        formatType = BundleFormatType.XML;
                    else
                        formatType = BundleFormatType.JSON;

                    // Save serialized data
                    await File.WriteAllTextAsync(file, await CreateBundle(Loader.Packages, formatType));

                    MainApp.Instance.MainWindow.HideLoadingDialog();

                    // Launch file
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = @$"/select, ""{file}"""
                    });

                }
            }
            catch (Exception ex)
            {
                MainApp.Instance.MainWindow.HideLoadingDialog();
                Logger.Error("An error occurred when saving packages to a file");
                Logger.Error(ex);
            }
        }

        public static async Task<string> CreateBundle(IEnumerable<IPackage> packages, BundleFormatType formatType = BundleFormatType.JSON)
        {
            SerializableBundle_v1 exportable = new();
            foreach (IPackage package in packages)
                if (package is Package && !package.Source.IsVirtualManager)
                    exportable.packages.Add(await package.AsSerializable());
                else
                    exportable.incompatible_packages.Add(package.AsSerializable_Incompatible());

            Logger.Debug("Finished loading serializable objects. Serializing with format " + formatType.ToString());
            string ExportableData;

            if (formatType == BundleFormatType.JSON)
                ExportableData = JsonSerializer.Serialize<SerializableBundle_v1>(exportable, new JsonSerializerOptions { WriteIndented = true });
            else if (formatType == BundleFormatType.YAML)
            {
                YamlDotNet.Serialization.ISerializer serializer = new YamlDotNet.Serialization.SerializerBuilder()
                    .Build();
                ExportableData = serializer.Serialize(exportable);
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                StreamWriter writer = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                serializer.Serialize(writer, exportable);
                writer.Close();
                ExportableData = await File.ReadAllTextAsync(tempfile);
                File.Delete(tempfile);
            }

            Logger.Debug("Serialization finished successfully");

            return ExportableData;
        }

        public async Task AddFromBundle(string content, BundleFormatType format)
        {
            // Deserialize data
            SerializableBundle_v1? DeserializedData;
            if (format == BundleFormatType.JSON)
            {
                DeserializedData = JsonSerializer.Deserialize<SerializableBundle_v1>(content);
            }
            else if (format == BundleFormatType.YAML)
            {
                YamlDotNet.Serialization.IDeserializer deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .Build();
                DeserializedData = deserializer.Deserialize<SerializableBundle_v1>(content);
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempfile, content);
                StreamReader reader = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                DeserializedData = serializer.Deserialize(reader) as SerializableBundle_v1;
                reader.Close();
                File.Delete(tempfile);
            }

            if (DeserializedData is null)
            {
                throw new InvalidDataException($"Deserialized data was null for content {content} and format {format}");
            }

            List<IPackage> packages = new List<IPackage>();

            foreach (SerializablePackage_v1 DeserializedPackage in DeserializedData.packages)
            {
                packages.Add(PackageFromSerializable(DeserializedPackage));
            }

            foreach (SerializableIncompatiblePackage_v1 DeserializedPackage in DeserializedData.incompatible_packages)
            {
                packages.Add(InvalidPackageFromSerializable(DeserializedPackage, NullSource.Instance));
            }

            await PEInterface.PackageBundlesLoader.AddPackagesAsync(packages);
        }

        public static IPackage PackageFromSerializable(SerializablePackage_v1 raw_package)
        {
            IPackageManager? manager = null;
            IManagerSource? source;

            foreach (var possible_manager in PEInterface.Managers)
            {
                if (possible_manager.Name == raw_package.ManagerName)
                {
                    manager = possible_manager;
                    break;
                }
            }

            if (manager?.Capabilities.SupportsCustomSources == true)
            {
                source = manager?.SourceProvider?.SourceFactory.GetSourceIfExists(raw_package.Source);
            }
            else
                source = manager?.DefaultSource;

            if (manager is null || source is null)
            {
                return InvalidPackageFromSerializable(raw_package.GetInvalidEquivalent(), NullSource.Instance);
            }

            return new ImportedPackage(raw_package, manager, source);
        }

        public static IPackage InvalidPackageFromSerializable(SerializableIncompatiblePackage_v1 raw_package, IManagerSource source)
        {
            return new InvalidImportedPackage(raw_package, source);
        }
    }
}


