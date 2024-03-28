using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.ExternalLibraries.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{

    public partial class DiscoverPackagesPage : Page
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
        private string LastCalledQuery = "";
        private bool AllSelected = false;
        int lastSavedWidth = 0;

        public string InstantSearchSettingString = "DisableInstantSearchDiscoverTab";
        public DiscoverPackagesPage()
        {
            InitializeComponent();
            QueryBothRadio.IsChecked = true;
            QueryOptionsGroup.SelectedIndex = 2;
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            LoadingProgressBar = __loading_progressbar;
            Initialized = true;
            ReloadButton.Click += async (s, e) => { LastCalledQuery = ""; await __load_packages(); };
            FindButton.Click += async (s, e) => { await FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += async (s, e) => { if (InstantSearchCheckbox.IsChecked == true) await FilterPackages(QueryBlock.Text); };
            QueryBlock.KeyUp += async (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) await FilterPackages(QueryBlock.Text); };

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
                        FilterPackages_SortOnly(QueryBlock.Text.Trim());
                    }
                    else
                    {
                        AppTools.Log((e.OriginalSource as FrameworkElement).DataContext.GetType());
                    }
                }
            };

            PackageList.DoubleTapped += (s, e) =>
            {
                _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Install);
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
                            MenuskipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
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
                        if (await Tools.App.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Install))
                            Tools.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as Package));
                    }
                    else if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                        Tools.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as Package));
                    else
                        _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Install);
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
                width = int.Parse(Tools.GetSettingsValue("SidepanelWidthDiscoverPage"));
            }
            catch
            {
                Tools.SetSettingsValue("SidepanelWidthDiscoverPage", "250");
            }
            BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);
                
            GenerateToolBar();
            LoadInterface();
            _ = __load_packages();

            QueryBlock.PlaceholderText = Tools.Translate("Search for packages");

        }

        protected async Task __load_packages()
        {
            if (!Initialized)
                return;
            await FilterPackages(QueryBlock.Text);
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
                RootNodeForManager[source.Manager].Children.Add(item);

            }
        }

        private void PackageContextMenu_AboutToShow(object sender, Package package)
        {
            if (!Initialized)
                return;
            PackageList.SelectedItem = package;
        }

        private void FilterOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            FilterPackages_SortOnly(QueryBlock.Text);
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            Tools.SetSettings(InstantSearchSettingString, InstantSearchCheckbox.IsChecked == false);
        }
        private void SourcesTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            FilterPackages_SortOnly(QueryBlock.Text);
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

            if (QueryBlock.Text == null || QueryBlock.Text.Length < 3)
            {
                MainSubtitle.Text = Tools.AutoTranslated("Found packages: ") + Packages.Count().ToString();
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                Packages.Clear();
                FilteredPackages.Clear();
                UsedManagers.Clear();
                SourcesTreeView.RootNodes.Clear();
                UsedSourcesForManager.Clear();
                RootNodeForManager.Clear();
                NodesForSources.Clear();
                return;
            }


            if (LastCalledQuery.Trim() != QueryBlock.Text.Trim())
            {
                LastCalledQuery = QueryBlock.Text.Trim();
                string InitialQuery = QueryBlock.Text.Trim();

                await Task.Delay(250);

                if (InitialQuery != QueryBlock.Text.Trim())
                    return;

                MainSubtitle.Text = Tools.AutoTranslated("Loading...");
                BackgroundText.Text = Tools.AutoTranslated("Loading...");
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



                List<Task<Package[]>> tasks = new();

                foreach (PackageManager manager in Tools.App.PackageManagerList)
                {
                    if (manager.IsEnabled() && manager.Status.Found)
                    {
                        Task<Package[]> task = manager.FindPackages(QueryBlock.Text);
                        tasks.Add(task);
                    }
                }

                while (tasks.Count > 0)
                {
                    foreach (Task<Package[]> task in tasks.ToArray())
                    {
                        if (!task.IsCompleted)
                            await Task.Delay(100);

                        if (InitialQuery != QueryBlock.Text)
                            return;

                        if (task.IsCompleted)
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                int InitialCount = Packages.Count;
                                foreach (Package package in task.Result)
                                {
                                    Packages.Add(package);
                                    AddPackageToSourcesList(package);

                                    if (package.GetUpgradablePackage() != null)
                                        package.SetTag(PackageTag.IsUpgradable);
                                    else if (package.GetInstalledPackage() != null)
                                        package.SetTag(PackageTag.AlreadyInstalled);
                                }
                                if (InitialCount < Packages.Count)
                                    FilterPackages_SortOnly(QueryBlock.Text.Trim(), StillLoading: true);
                            }
                            tasks.Remove(task);
                        }
                    }
                }
            }

            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        public async Task FilterPackages(string query, bool StillLoading = false)
        {
            if (!Initialized)
                return;
            await LoadPackages();
            FilterPackages_SortOnly(query, StillLoading);
        }

        public void FilterPackages_SortOnly(string query, bool StillLoading = false)
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
            else if (QueryBothRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Name).Contains(CharsFunc(query)) | CharsFunc(x.Id).Contains(CharsFunc(query))).ToArray();
            else if (QueryExactMatch.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Name) == CharsFunc(query) | CharsFunc(x.Id) == CharsFunc(query)).ToArray();
            else // QuerySimilarResultsRadio == true
                MatchingList = Packages.ToArray();

            FilteredPackages.BlockSorting = true;
            int HiddenPackagesDueToSource = 0;
            foreach (Package match in MatchingList)
            {
                if (VisibleManagers.Contains(match.Manager) || VisibleSources.Contains(match.Source))
                    FilteredPackages.Add(match);
                else
                    HiddenPackagesDueToSource++;
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();

            if (MatchingList.Count() == 0)
            {
                if (!StillLoading)
                {
                    if (QueryBlock.Text == "")
                        BackgroundText.Text = SourcesPlaceholderText.Text = Tools.AutoTranslated("Search for packages to start");
                    else if (QueryBlock.Text.Length < 3)
                    {
                        BackgroundText.Text = Tools.AutoTranslated("Please enter at least 3 characters");
                        SourcesPlaceholderText.Text = Tools.AutoTranslated("Search for packages to start");
                    }
                    else
                    {
                        BackgroundText.Text = Tools.AutoTranslated("No results were found matching the input criteria");
                        SourcesPlaceholderText.Text = Tools.AutoTranslated("No packages were found");
                        MainSubtitle.Text = Tools.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (MatchingList.Length - HiddenPackagesDueToSource).ToString());
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                BackgroundText.Visibility = Visibility.Collapsed;
                MainSubtitle.Text = Tools.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (MatchingList.Length - HiddenPackagesDueToSource).ToString());
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
            MainTitle.Text = Tools.AutoTranslated("Discover Packages");
            HeaderIcon.Glyph = "\uF6FA";
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
            AppBarButton InstallSelected = new();
            AppBarButton InstallAsAdmin = new();
            AppBarButton InstallSkipHash = new();
            AppBarButton InstallInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

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
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(ExportSelection);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { InstallSelected,        Tools.Translate("Install selected packages") },
                { InstallAsAdmin,         " " + Tools.Translate("Install as administrator") },
                { InstallSkipHash,        " " + Tools.Translate("Skip integrity checks") },
                { InstallInteractive,     " " + Tools.Translate("Interactive installation") },
                { InstallationSettings,   Tools.Translate("Installation options") },
                { PackageDetails,         " " + Tools.Translate("Package details") },
                { SharePackage,           " " + Tools.Translate("Share") },
                { SelectAll,              " " + Tools.Translate("Select all") },
                { SelectNone,             " " + Tools.Translate("Clear selection") },
                { ExportSelection,        Tools.Translate("Add selection to bundle") },
                { HelpButton,             Tools.Translate("Help") }
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
                { SelectAll,            "selectall" },
                { SelectNone,           "selectnone" },
                { ExportSelection,      "add_to" },
                { HelpButton,           "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) => { 
                if (PackageList.SelectedItem != null) 
                    _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Install);
            };
            
            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (s, e) => { Tools.App.MainWindow.NavigationPage.ShowHelp(); };

            InstallationSettings.Click += async (s, e) =>
            {
                if (PackageList.SelectedItem != null && await Tools.App.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Install))
                    Tools.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as Package));
            };

            InstallSelected.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        Tools.AddOperationToList(new InstallPackageOperation(package));
            };

            InstallAsAdmin.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        Tools.AddOperationToList(new InstallPackageOperation(package,
                            new InstallationOptions(package) { RunAsAdministrator = true }));
            };

            InstallSkipHash.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        Tools.AddOperationToList(new InstallPackageOperation(package,
                            new InstallationOptions(package) { SkipHashCheck = true }));
            };

            InstallInteractive.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        Tools.AddOperationToList(new InstallPackageOperation(package,
                            new InstallationOptions(package) { InteractiveInstallation = true }));
            };

            SharePackage.Click += (s, e) => {
                if (PackageList.SelectedItem != null) 
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

        private void MenuDetails_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Install);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.App.MainWindow.SharePackage(PackageList.SelectedItem as Package);
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as Package));
        }

        private void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as Package,
                new InstallationOptions(PackageList.SelectedItem as Package) { SkipHashCheck = true }));
        }

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as Package,
                new InstallationOptions(PackageList.SelectedItem as Package) { InteractiveInstallation = true }));
        }

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            Tools.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as Package,
                new InstallationOptions(PackageList.SelectedItem as Package) { RunAsAdministrator = true }));
        }

        private void SelectAllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectAll();
        }

        private void ClearSourceSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectedItems.Clear();
            FilterPackages_SortOnly(QueryBlock.Text.Trim());
        }

        private async void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            if (await Tools.App.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Install))
                Tools.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as Package));
        }

        private void SelectAllItems()
        {
            foreach (Package package in FilteredPackages)
                package.IsChecked = true;
            AllSelected = true;
        }

        private void ClearItemSelection()
        {
            foreach (Package package in FilteredPackages)
                package.IsChecked = false;
            AllSelected = false;
        }


        public void ShowSharedPackage_ThreadSafe(string pId, string pSource)
        {
            Tools.App.MainWindow.DispatcherQueue.TryEnqueue(() => { ShowSharedPackage(pId, pSource); });
        }

        private async void ShowSharedPackage(string pId, string pSource)
        {
            if (!Initialized)
                return;

            AppTools.Log("Showing shared package...");

            Tools.App.MainWindow.Activate();

            Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Please wait...").Replace("{0}", pId));
            QueryIdRadio.IsChecked = true;
            QueryBlock.Text = pId;
            await FilterPackages(pId);
            QueryBothRadio.IsChecked = true;
            Tools.App.MainWindow.HideLoadingDialog();
            if (FilteredPackages.Count == 1)
            {
                AppTools.Log("Only one package was found for pId=" + pId + ", showing it.");
                await Tools.App.MainWindow.NavigationPage.ShowPackageDetails(FilteredPackages[0], OperationType.Install);
            }
            else if (FilteredPackages.Count > 1)
            {
                var managerName = pSource.Contains(':')? pSource.Split(':')[0] : pSource;
                foreach (var match in FilteredPackages)
                    if (match.Source.Manager.Name == managerName)
                    {
                        AppTools.Log("Equivalent package for id=" + pId + " and source=" + pSource + " found: " + match.ToString());
                        await Tools.App.MainWindow.NavigationPage.ShowPackageDetails(match, OperationType.Install);
                        return;
                    }
                AppTools.Log("No package found with the exact same manager, showing the first one.");
                await Tools.App.MainWindow.NavigationPage.ShowPackageDetails(FilteredPackages[0], OperationType.Install);
            }
            else
            {
                AppTools.Log("No packages were found matching the given pId=" + pId);
                var c = new ContentDialog();
                c.XamlRoot = this.XamlRoot;
                c.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                c.Title = Tools.Translate("Package not found");
                c.Content = Tools.Translate("The package {0} from {1} was not found.").Replace("{0}", pId).Replace("{1}", pSource);
                c.PrimaryButtonText = Tools.Translate("OK");
                c.DefaultButton = ContentDialogButton.Primary;
                await Tools.App.MainWindow.ShowDialogAsync(c);
            }
        }

        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
                return;

            lastSavedWidth = ((int)(e.NewSize.Width / 10));
            Tools.SetSettingsValue("SidepanelWidthDiscoverPage", ((int)e.NewSize.Width).ToString());
            foreach(var control in SidePanelGrid.Children)
            {
                control.Visibility = e.NewSize.Width > 20 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
