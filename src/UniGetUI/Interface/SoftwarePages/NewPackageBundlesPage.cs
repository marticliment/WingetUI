using ExternalLibraries.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using Windows.Media.Devices;

namespace UniGetUI.Interface.SoftwarePages
{
    namespace UniGetUI.Interface.SoftwarePages
    {
        public class NewPackageBundlesPage : AbstractPackagesPage
        {
            bool HasDoneBackup = false;

            BetterMenuItem? MenuAsAdmin;
            BetterMenuItem? MenuInteractive;
            BetterMenuItem? MenuSkipHash;

            public NewPackageBundlesPage()
            : base(new PackagesPageData()
            {
                DisableAutomaticPackageLoadOnStart = true,
                MegaQueryBlockEnabled = false,
                ShowLastLoadTime = false,
                PackagesAreCheckedByDefault = false,
                DisableSuggestedResultsRadio = true,
                PageName = "Bundles",

                Loader = PEInterface.PackageBundlesLoader,
                PageRole = OperationType.Uninstall,

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
                    IconName = "newversion",
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
                    Text = "Skip hash checks",
                    IconName = "checksum"
                };
                MenuSkipHash.Click += MenuRemoveData_Invoked;
                menu.Items.Add(MenuSkipHash);

                menu.Items.Add(new MenuFlyoutSeparator());

                BetterMenuItem menuRemoveFromList = new()
                {
                    Text = "Remove from list",
                    IconName = "trash"
                };
                menuRemoveFromList.Click += MenuRemoveFromList_Invoked;
                menu.Items.Add(menuRemoveFromList);
                menu.Items.Add(new MenuFlyoutSeparator());
                
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

                Dictionary<AppBarButton, string> Icons = new()
                {
                    { NewBundle,              "add_to" },
                    { InstallPackages,        "newversion" },
                    { InstallAsAdmin,       "runasadmin" },
                    { InstallInteractive,   "interactive" },
                    { InstallSkipHash,   "checksum" },
                    { OpenBundle,             "openfolder" },
                    { RemoveSelected,         "trash" },
                    { ExportBundle,           "save" },
                    { PackageDetails,         "info" },
                    { SharePackage,           "share" },
                    { HelpButton,             "help" }
                };

                foreach (AppBarButton toolButton in Icons.Keys)
                    toolButton.Icon = new LocalIcon(Icons[toolButton]);

                PackageDetails.Click += (s, e) =>
                {
                    Package? package = SelectedItem as Package;
                    if (package != null)
                        _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package, OperationType.None);
                };

                HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

                NewBundle.Click += (s, e) =>
                {
                    FilteredPackages.Clear();
                };

                RemoveSelected.Click += (s, e) =>
                {
                    foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                        Loader.Remove(package);
                };

                InstallPackages.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages());
                InstallSkipHash.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), skiphash: true);
                InstallInteractive.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), interactive: true);
                InstallAsAdmin.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), elevated: true);


                OpenBundle.Click += (s, e) =>
                {
                    FilteredPackages.Clear();
                    OpenFile();
                };

                ExportBundle.Click += (s, e) =>
                {
                    SaveFile();
                };

                SharePackage.Click += (s, e) =>
                {
                    Package? package = SelectedItem as Package;
                    if (package != null) MainApp.Instance.MainWindow.SharePackage(package);
                };

            }

            public async Task ImportAndInstallPackage(IEnumerable<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? skiphash = null)
            {
                MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Preparing packages, please wait..."));
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    ImportedPackage? imported = package as ImportedPackage;
                    if (imported != null) await imported.RegisterPackage();
                }

                MainApp.Instance.MainWindow.HideLoadingDialog();

                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    if (package is Package)
                    {
                        MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                            await InstallationOptions.FromPackageAsync(package, elevated, interactive, skiphash)));
                    }
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
                    string file = picker.Show(new List<string>() { "*.json", "*.yaml", "*.xml" });
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
                    string file = (new FileSavePicker(MainApp.Instance.MainWindow.GetWindowHandle())).Show(new List<string>() { "*.json", "*.yaml", "*.xml" }, CoreTools.Translate("Package bundle") + ".json");
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
                        Process.Start(new ProcessStartInfo()
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

                if (DeserializedData == null)
                    throw new Exception($"Deserialized data was null for content {content} and format {format}");

                List<IPackage> packages = new List<IPackage>();

                foreach (SerializablePackage_v1 DeserializedPackage in DeserializedData.packages)
                {
                    packages.Add(PackageFromSerializable(DeserializedPackage));
                }

                foreach (SerializableIncompatiblePackage_v1 DeserializedPackage in DeserializedData.incompatible_packages)
                {
                    packages.Add(PackageFromSerializable(DeserializedPackage));
                }

                PEInterface.PackageBundlesLoader.AddPackages(packages);
            }

            public static IPackage PackageFromSerializable(SerializablePackage_v1 raw_package)
            {
                IPackageManager? manager = null;
                IManagerSource? source = null;

                foreach (var possible_manager in PEInterface.Managers)
                {
                    if (possible_manager.Name == raw_package.ManagerName)
                    {
                        manager = possible_manager;
                        break;
                    }
                }

                if (manager?.Capabilities.SupportsCustomSources == true)
                    source = manager?.SourceProvider?.SourceFactory.GetSourceIfExists(raw_package.Source);
                else
                    source = manager?.DefaultSource;

                if (manager is null || source is null)
                {
                    return PackageFromSerializable(raw_package.GetInvalidEquivalent());
                }

                return new ImportedPackage(raw_package, manager, source);
            }

            public static IPackage PackageFromSerializable(SerializableIncompatiblePackage_v1 raw_package)
            {
                return new InvalidPackage(raw_package);
            }
        }


    }

}
