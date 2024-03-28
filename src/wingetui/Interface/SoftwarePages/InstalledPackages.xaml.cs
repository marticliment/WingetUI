using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Data;
using UniGetUI.Core;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{

    public partial class InstalledPackagesPage : Page
    {
        public ObservableCollection<Package> Packages = new();
        public SortableObservableCollection<Package> FilteredPackages = new() { SortingSelector = (a) => (a.Name) };
        protected List<PackageManager> UsedManagers = new();
        protected Dictionary<PackageManager, List<ManagerSource>> UsedSourcesForManager = new();
        protected Dictionary<PackageManager, TreeViewNode> RootNodeForManager = new();
        protected Dictionary<ManagerSource, TreeViewNode> NodesForSources = new();
        protected AppTools Tools = AppTools.Instance;

        protected TranslatedTextBlock MainTitle;
        protected TranslatedTextBlock MainSubtitle;
        public ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected MenuFlyout ContextMenu;

        private bool Initialized = false;
        private bool AllSelected = false;

        private bool HasDoneBackup = false;
        TreeViewNode LocalPackagesNode;
        int lastSavedWidth = 0;

        public string InstantSearchSettingString = "DisableInstantSearchInstalledTab";
        public InstalledPackagesPage()
        {
            InitializeComponent();
            QueryBothRadio.IsChecked = true;
            QueryOptionsGroup.SelectedIndex = 2;
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            LoadingProgressBar = __loading_progressbar;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LocalPackagesNode = new TreeViewNode() { Content = Tools.Translate("Local"), IsExpanded = false };
            Initialized = true;
            ReloadButton.Click += async (s, e) => { await LoadPackages(); };
            FindButton.Click += (s, e) => { FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += (s, e) => { if (InstantSearchCheckbox.IsChecked == true) FilterPackages(QueryBlock.Text); };
            QueryBlock.KeyUp += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) FilterPackages(QueryBlock.Text); };

            SourcesTreeView.Tapped += (s, e) =>
            {
                if (e.OriginalSource != null && (e.OriginalSource as FrameworkElement).DataContext != null)
                {
                    AppTools.Log(e);
                    AppTools.Log(e.OriginalSource);
                    AppTools.Log(e.OriginalSource as FrameworkElement);
                    AppTools.Log((e.OriginalSource as FrameworkElement).DataContext);
                    if ((e.OriginalSource as FrameworkElement).DataContext is TreeViewNode)
                    {
                        TreeViewNode node = (e.OriginalSource as FrameworkElement).DataContext as TreeViewNode;
                        if (node == null)
                            return;
                        if (SourcesTreeView.SelectedNodes.Contains(node))
                            SourcesTreeView.SelectedNodes.Remove(node);
                        else
                            SourcesTreeView.SelectedNodes.Add(node);
                        FilterPackages(QueryBlock.Text.Trim());
                    }
                    else
                    {
                        AppTools.Log((e.OriginalSource as FrameworkElement).DataContext.GetType());
                    }
                }
            };

            PackageList.DoubleTapped += (s, e) =>
            {
                _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Uninstall);
            };

            PackageList.RightTapped += (s, e) =>
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    try
                    {
                        if (element.DataContext != null && element.DataContext is Package package)
                        {
                            PackageList.SelectedItem = package;
                            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
                            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
                            MenuRemoveData.IsEnabled = package.Manager.Capabilities.CanRemoveDataOnUninstall;
                            PackageContextMenu.ShowAt(PackageList, e.GetPosition(PackageList));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppTools.Log(ex);
                    }
                }
            };

            PackageList.KeyUp += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && PackageList.SelectedItem != null)
                {
                    if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        if (await Tools.App.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Uninstall))
                            Tools.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as Package));
                    }
                    else if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                        ConfirmAndUninstall(PackageList.SelectedItem as Package, new InstallationOptions(PackageList.SelectedItem as Package));
                    else
                        _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Uninstall);
                }
                else if (e.Key == Windows.System.VirtualKey.A && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (AllSelected)
                        ClearItemSelection();
                    else
                        SelectAllItems();
                }
                else if (e.Key == Windows.System.VirtualKey.Space && PackageList.SelectedItem != null)
                {
                    (PackageList.SelectedItem as Package).IsChecked = !(PackageList.SelectedItem as Package).IsChecked;
                }
                else if (e.Key == Windows.System.VirtualKey.F5)
                {
                    _ = LoadPackages();
                }
                else if (e.Key == Windows.System.VirtualKey.F1)
                {
                    Tools.App.MainWindow.NavigationPage.ShowHelp();
                }
                else if (e.Key == Windows.System.VirtualKey.F && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    QueryBlock.Focus(FocusState.Programmatic);
                }
            };
            int width = 250;
            try
            {
                width = int.Parse(Tools.GetSettingsValue("SidepanelWidthInstalledPage"));
            }
            catch
            {
                Tools.SetSettingsValue("SidepanelWidthInstalledPage", "250");
            }
            BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);

            GenerateToolBar();
            LoadInterface();
            _ = LoadPackages();

            QueryBlock.PlaceholderText = Tools.Translate("Search for packages");
        }

        public void ReloadPackages()
        {
            _ = LoadPackages();
        }
        protected void AddPackageToSourcesList(Package package)
        {
            if (!Initialized)
                return;
            ManagerSource source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                TreeViewNode Node;
                Node = new TreeViewNode() { Content = source.Manager.Name + "                                                                                    .", IsExpanded = false };
                SourcesTreeView.RootNodes.Add(Node);
                SourcesTreeView.SelectedNodes.Add(Node);
                RootNodeForManager.Add(source.Manager, Node);
                UsedSourcesForManager.Add(source.Manager, new List<ManagerSource>());
                SourcesPlaceholderText.Visibility = Visibility.Collapsed;
                SourcesTreeViewGrid.Visibility = Visibility.Visible;
            }

            if ((!UsedSourcesForManager.ContainsKey(source.Manager) || !UsedSourcesForManager[source.Manager].Contains(source)) && source.Manager.Capabilities.SupportsCustomSources)
            {
                UsedSourcesForManager[source.Manager].Add(source);
                TreeViewNode item = new() { Content = source.Name + "                                                                                    ." };
                NodesForSources.Add(source, item);

                if (source.IsVirtualManager)
                {
                    LocalPackagesNode.Children.Add(item);
                    if (!SourcesTreeView.RootNodes.Contains(LocalPackagesNode))
                    {
                        SourcesTreeView.RootNodes.Add(LocalPackagesNode);
                        SourcesTreeView.SelectedNodes.Add(LocalPackagesNode);
                    }
                }
                else
                    RootNodeForManager[source.Manager].Children.Add(item);
            }
        }

        private void FilterOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            FilterPackages(QueryBlock.Text);
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            Tools.SetSettings(InstantSearchSettingString, InstantSearchCheckbox.IsChecked == false);
        }
        private void SourcesTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            FilterPackages(QueryBlock.Text);
        }

        /*
         * 
         * 
         *  DO NOT MODIFY THE UPPER PART OF THIS FILE
         * 
         * 
         */

        public async Task LoadPackages()
        {
            if (!Initialized)
                return;

            if (LoadingProgressBar.Visibility == Visibility.Visible)
                return; // If already loading, don't load again

            MainSubtitle.Text = Tools.AutoTranslated("Loading...");
            BackgroundText.Text = Tools.AutoTranslated("Loading...");
            BackgroundText.Visibility = Visibility.Visible;
            LoadingProgressBar.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Visibility = Visibility.Visible;
            SourcesTreeViewGrid.Visibility = Visibility.Collapsed;
            SourcesPlaceholderText.Text = Tools.AutoTranslated("Loading...");

            Packages.Clear();
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();
            LocalPackagesNode.Children.Clear();

            await Task.Delay(100);

            List<Task<Package[]>> tasks = new();

            foreach (PackageManager manager in Tools.App.PackageManagerList)
            {
                if (manager.IsEnabled() && manager.Status.Found)
                {
                    Task<Package[]> task = manager.GetInstalledPackages();
                    tasks.Add(task);
                }
            }

            while (tasks.Count > 0)
            {
                foreach (Task<Package[]> task in tasks.ToArray())
                {
                    if (!task.IsCompleted)
                        await Task.Delay(100);

                    if (task.IsCompleted)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            int InitialCount = Packages.Count;
                            foreach (Package package in task.Result)
                            {
                                Packages.Add(package);
                                AddPackageToSourcesList(package);

                                if (await package.HasUpdatesIgnoredAsync(Version: "*"))
                                    package.Tag = PackageTag.Pinned;
                                else if (package.GetUpgradablePackage() != null)
                                    package.Tag = PackageTag.IsUpgradable;
                                package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);
                            }
                            if (InitialCount < Packages.Count)
                                FilterPackages(QueryBlock.Text.Trim(), StillLoading: true);
                        }
                        tasks.Remove(task);
                    }
                }
            }


            FilterPackages(QueryBlock.Text);
            LoadingProgressBar.Visibility = Visibility.Collapsed;

            if(!HasDoneBackup)
            {
                if(Tools.GetSettings("EnablePackageBackup"))
                    _ = BackupPackages();
            }
        }

        public async Task BackupPackages()
        {

            try
            {
                AppTools.Log("Start backup");
                var packagestoExport = new List<BundledPackage>();
                foreach (var package in Packages)
                    packagestoExport.Add(await BundledPackage.FromPackageAsync(package));

                var BackupContents = await PackageBundlePage.GetBundleStringFromPackages(packagestoExport.ToArray(), BundleFormatType.JSON);

                var dirName = Tools.GetSettingsValue("ChangeBackupOutputDirectory");
                if (dirName == "")
                    dirName = CoreData.WingetUI_DefaultBackupDirectory;

                if(!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                var fileName = Tools.GetSettingsValue("ChangeBackupFileName");
                if (fileName == "")
                    fileName = Tools.Translate("{pcName} installed packages").Replace("{pcName}", Environment.MachineName);

                if (Tools.GetSettings("EnableBackupTimestamping"))
                    fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

                fileName += ".json";

                var filePath = Path.Combine(dirName, fileName);
                await File.WriteAllTextAsync(filePath, BackupContents);
                HasDoneBackup = true;
                AppTools.Log("Backup saved to " + filePath);
            } 
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        public void FilterPackages(string query, bool StillLoading = false)
        {
            if (!Initialized)
                return;

            FilteredPackages.Clear();
            List<ManagerSource> VisibleSources = new();
            List<PackageManager> VisibleManagers = new();

            if (SourcesTreeView.SelectedNodes.Count > 0)
            {
                foreach (TreeViewNode node in SourcesTreeView.SelectedNodes)
                {
                    if (NodesForSources.ContainsValue(node))
                        VisibleSources.Add(NodesForSources.First(x => x.Value == node).Key);
                    else if (RootNodeForManager.ContainsValue(node))
                        VisibleManagers.Add(RootNodeForManager.First(x => x.Value == node).Key);
                }
            }


            Package[] MatchingList;

            Func<string, string> CaseFunc;
            if (UpperLowerCaseCheckbox.IsChecked == true)
                CaseFunc = (x) => { return x; };
            else
                CaseFunc = (x) => { return x.ToLower(); };

            Func<string, string> CharsFunc;
            if (IgnoreSpecialCharsCheckbox.IsChecked == true)
                CharsFunc = (x) =>
                {
                    string temp_x = CaseFunc(x).Replace("-", "").Replace("_", "").Replace(" ", "").Replace("@", "").Replace("\t", "").Replace(".", "").Replace(",", "").Replace(":", "");
                    foreach (KeyValuePair<char, string> entry in new Dictionary<char, string>
                        {
                            {'a', "àáäâ"},
                            {'e', "èéëê"},
                            {'i', "ìíïî"},
                            {'o', "òóöô"},
                            {'u', "ùúüû"},
                            {'y', "ýÿ"},
                            {'c', "ç"},
                            {'ñ', "n"},
                        })
                    {
                        foreach (char InvalidChar in entry.Value)
                            x = x.Replace(InvalidChar, entry.Key);
                    }
                    return temp_x;
                };
            else
                CharsFunc = (x) => { return CaseFunc(x); };

            if (QueryIdRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Name).Contains(CharsFunc(query))).ToArray();
            else if (QueryNameRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Id).Contains(CharsFunc(query))).ToArray();
            else // QueryBothRadio.IsChecked == true
                MatchingList = Packages.Where(x => CharsFunc(x.Name).Contains(CharsFunc(query)) | CharsFunc(x.Id).Contains(CharsFunc(query))).ToArray();

            FilteredPackages.BlockSorting = true;
            int HiddenPackagesDueToSource = 0;
            foreach (Package match in MatchingList)
            {
                if ((VisibleManagers.Contains(match.Manager) && match.Manager != Tools.App.Winget) || VisibleSources.Contains(match.Source))
                    FilteredPackages.Add(match);
                else
                    HiddenPackagesDueToSource++;
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();
            UpdatePackageCount(StillLoading);
        }

        public void UpdatePackageCount(bool StillLoading = false)
        { 
            if (FilteredPackages.Count() == 0)
            {
                if (!StillLoading)
                {
                    if (Packages.Count() == 0)
                    {
                        BackgroundText.Text = SourcesPlaceholderText.Text = Tools.AutoTranslated("We couldn't find any package");
                        SourcesPlaceholderText.Text = Tools.AutoTranslated("No sources found");
                        MainSubtitle.Text = Tools.AutoTranslated("No packages found");
                    }
                    else
                    {
                        BackgroundText.Text = Tools.AutoTranslated("No results were found matching the input criteria");
                        SourcesPlaceholderText.Text = Tools.AutoTranslated("No packages were found");
                        MainSubtitle.Text = Tools.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (FilteredPackages.Count()).ToString());
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }

            }
            else
            {
                BackgroundText.Visibility = Visibility.Collapsed;
                MainSubtitle.Text = Tools.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (FilteredPackages.Count()).ToString());
            }
        }

        public void SortPackages(string Sorter)
        {
            if (!Initialized)
                return;

            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SortingSelector = (a) => (a.GetType().GetProperty(Sorter).GetValue(a));
            FilteredPackages.Sort();

            if (FilteredPackages.Count > 0)
                PackageList.ScrollIntoView(FilteredPackages[0]);
        }

        public void LoadInterface()
        {
            if (!Initialized)
                return;
            MainTitle.Text = Tools.AutoTranslated("Installed Packages");
            HeaderIcon.Glyph = "\uE977";
            CheckboxHeader.Content = " ";
            NameHeader.Content = Tools.Translate("Package Name");
            IdHeader.Content = Tools.Translate("Package ID");
            VersionHeader.Content = Tools.Translate("Version");
            SourceHeader.Content = Tools.Translate("Source");

            CheckboxHeader.Click += (s, e) => { SortPackages("IsCheckedAsString"); };
            NameHeader.Click += (s, e) => { SortPackages("Name"); };
            IdHeader.Click += (s, e) => { SortPackages("Id"); };
            VersionHeader.Click += (s, e) => { SortPackages("VersionAsFloat"); };
            SourceHeader.Click += (s, e) => { SortPackages("SourceAsString"); };
        }


        public void GenerateToolBar()
        {
            if (!Initialized)
                return;
            AppBarButton UninstallSelected = new();
            AppBarButton UninstallAsAdmin = new();
            AppBarButton UninstallInteractive = new();
            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

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
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
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
                { UninstallSelected,    Tools.Translate("Uninstall selected packages") },
                { UninstallAsAdmin,     " " + Tools.Translate("Uninstall as administrator") },
                { UninstallInteractive, " " + Tools.Translate("Interactive uninstall") },
                { InstallationSettings, " " + Tools.Translate("Installation options") },
                { PackageDetails,       " " + Tools.Translate("Package details") },
                { SharePackage,         " " + Tools.Translate("Share") },
                { SelectAll,            " " + Tools.Translate("Select all") },
                { SelectNone,           " " + Tools.Translate("Clear selection") },
                { IgnoreSelected,       Tools.Translate("Ignore selected packages") },
                { ManageIgnored,        Tools.Translate("Manage ignored updates") },
                { ExportSelection,      Tools.Translate("Add selection to bundle") },
                { HelpButton,           Tools.Translate("Help") }
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
                { SelectAll,              "selectall" },
                { SelectNone,             "selectnone" },
                { IgnoreSelected,         "pin" },
                { ManageIgnored,          "clipboard_list" },
                { ExportSelection,        "add_to" },
                { HelpButton,             "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) => { 
                if (PackageList.SelectedItem != null) 
                    _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Uninstall); 
            };
            
            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (s, e) => { Tools.App.MainWindow.NavigationPage.ShowHelp(); };


            InstallationSettings.Click += async (s, e) =>
            {
                if (PackageList.SelectedItem != null && await Tools.App.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Uninstall))
                    ConfirmAndUninstall(PackageList.SelectedItem as Package, new InstallationOptions(PackageList.SelectedItem as Package));
            };


            ManageIgnored.Click += async (s, e) => { await Tools.App.MainWindow.NavigationPage.ManageIgnoredUpdatesDialog(); };
            IgnoreSelected.Click += async (s, e) =>
            {
                foreach (Package package in FilteredPackages.ToArray()) if (package.IsChecked)
                        await package.AddToIgnoredUpdatesAsync();
            };

            UninstallSelected.Click += (s, e) => { ConfirmAndUninstall(FilteredPackages.Where(x => x.IsChecked).ToArray()); };
            UninstallAsAdmin.Click += (s, e) => { ConfirmAndUninstall(FilteredPackages.Where(x => x.IsChecked).ToArray(), AsAdmin: true); };
            UninstallInteractive.Click += (s, e) => { ConfirmAndUninstall(FilteredPackages.Where(x => x.IsChecked).ToArray(), Interactive: true); };

            SharePackage.Click += (s, e) => { 
                if(PackageList.SelectedItem != null) 
                    Tools.App.MainWindow.SharePackage(PackageList.SelectedItem as Package); 
            };

            SelectAll.Click += (s, e) => { SelectAllItems(); };
            SelectNone.Click += (s, e) => { ClearItemSelection(); };

        }

        private async void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            Tools.App.MainWindow.NavigationPage.BundlesNavButton.ForceClick();
            await Tools.App.MainWindow.NavigationPage.BundlesPage.AddPackages(FilteredPackages.ToArray().Where(x => x.IsChecked));
        }

        public async void ConfirmAndUninstall(Package package, InstallationOptions options)
        {
            ContentDialog dialog = new();

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = Tools.Translate("Are you sure?");
            dialog.PrimaryButtonText = Tools.Translate("No");
            dialog.SecondaryButtonText = Tools.Translate("Yes");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = Tools.Translate("Do you really want to uninstall {0}?").Replace("{0}", package.Name);

            if (await Tools.App.MainWindow.ShowDialogAsync(dialog) == ContentDialogResult.Secondary)
                Tools.AddOperationToList(new UninstallPackageOperation(package, options));

        }
        public async void ConfirmAndUninstall(Package[] packages, bool AsAdmin = false, bool Interactive = false, bool RemoveData = false)
        {
            if (packages.Length == 0)
                return;
            if (packages.Length == 1)
            {
                ConfirmAndUninstall(packages[0], new InstallationOptions(packages[0]) { RunAsAdministrator = AsAdmin, InteractiveInstallation = Interactive, RemoveDataOnUninstall = RemoveData });
                return;
            }

            ContentDialog dialog = new();

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = Tools.Translate("Are you sure?");
            dialog.PrimaryButtonText = Tools.Translate("No");
            dialog.SecondaryButtonText = Tools.Translate("Yes");
            dialog.DefaultButton = ContentDialogButton.Primary;

            StackPanel p = new();
            p.Children.Add(new TextBlock() { Text = Tools.Translate("Do you really want to uninstall the following {0} packages?").Replace("{0}", packages.Length.ToString()), Margin = new Thickness(0, 0, 0, 5) });

            string pkgList = "";
            foreach (Package package in packages)
                pkgList += " ● " + package.Name + "\x0a";

            TextBlock PackageListTextBlock = new() { FontFamily = new FontFamily("Consolas"), Text = pkgList };
            p.Children.Add(new ScrollView() { Content = PackageListTextBlock, MaxHeight = 200 });

            dialog.Content = p;

            if (await Tools.App.MainWindow.ShowDialogAsync(dialog) == ContentDialogResult.Secondary)
                foreach (Package package in packages)
                    Tools.AddOperationToList(new UninstallPackageOperation(package, new InstallationOptions(package)
                    {
                        RunAsAdministrator = AsAdmin,
                        InteractiveInstallation = Interactive,
                        RemoveDataOnUninstall = RemoveData
                    }));
        }

        private void MenuUninstall_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            ConfirmAndUninstall((PackageList.SelectedItem as Package),
                new InstallationOptions((PackageList.SelectedItem as Package)));
        }

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            ConfirmAndUninstall((PackageList.SelectedItem as Package),
                new InstallationOptions((PackageList.SelectedItem as Package)) { RunAsAdministrator = true });
        }

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            ConfirmAndUninstall((PackageList.SelectedItem as Package),
                new InstallationOptions((PackageList.SelectedItem as Package)) { InteractiveInstallation = true });
        }

        private void MenuRemoveData_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            ConfirmAndUninstall((PackageList.SelectedItem as Package),
                new InstallationOptions((PackageList.SelectedItem as Package)) { RemoveDataOnUninstall = true });
        }

        private void MenuReinstall_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.AddOperationToList(new InstallPackageOperation((PackageList.SelectedItem as Package)));
        }

        private void MenuUninstallThenReinstall_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.AddOperationToList(new UninstallPackageOperation((PackageList.SelectedItem as Package)));
            Tools.AddOperationToList(new InstallPackageOperation((PackageList.SelectedItem as Package)));

        }
        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = (PackageList.SelectedItem as Package).AddToIgnoredUpdatesAsync();
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.App.MainWindow.SharePackage((PackageList.SelectedItem as Package));
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Uninstall);
        }


        private void SelectAllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectAll();
        }

        private void ClearSourceSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectedItems.Clear();
            FilterPackages(QueryBlock.Text.Trim());
        }

        private async void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            if (PackageList.SelectedItem as Package != null
                && await Tools.App.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Uninstall))
            {
                ConfirmAndUninstall(PackageList.SelectedItem as Package, new InstallationOptions(PackageList.SelectedItem as Package));
            }
        }

        private void SelectAllItems()
        {
            foreach (Package package in FilteredPackages.ToArray())
                package.IsChecked = true;
            AllSelected = true;
        }

        private void ClearItemSelection()
        {
            foreach (Package package in FilteredPackages.ToArray())
                package.IsChecked = false;
            AllSelected = false;
        }

        public void RemoveCorrespondingPackages(Package foreignPackage)
        {
            foreach (Package package in Packages.ToArray())
                if (package == foreignPackage || package.Equals(foreignPackage))
                {
                    Packages.Remove(package);
                    if (FilteredPackages.Contains(package))
                        FilteredPackages.Remove(package);
                }
            UpdatePackageCount();
        }
        public void AddInstalledPackage(Package foreignPackage)
        {
            foreach (Package package in Packages.ToArray())
                if (package == foreignPackage || package.Equals(foreignPackage))
                    return;
            Packages.Add(foreignPackage);
            UpdatePackageCount();
        }
        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
                return;

            lastSavedWidth = ((int)(e.NewSize.Width / 10));
            Tools.SetSettingsValue("SidepanelWidthInstalledPage", ((int)e.NewSize.Width).ToString());
            foreach (var control in SidePanelGrid.Children)
            {
                control.Visibility = e.NewSize.Width > 20 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

    }
}
