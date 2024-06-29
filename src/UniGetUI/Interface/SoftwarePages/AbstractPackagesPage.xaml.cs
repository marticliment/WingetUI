using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public abstract partial class AbstractPackagesPage : Page, IPageWithKeyboardShortcuts
    {

        protected struct PackagesPageData
        {
            public bool DisableAutomaticPackageLoadOnStart;
            public bool MegaQueryBlockEnabled;
            public bool PackagesAreCheckedByDefault;
            public bool ShowLastLoadTime;
            public bool DisableSuggestedResultsRadio;

            public OperationType PageRole;
            public AbstractPackageLoader Loader;

            public string PageName;
            public string PageTitle;
            public string Glyph;

            public string NoPackages_BackgroundText;
            public string NoPackages_SourcesText;
            public string NoPackages_SubtitleText_Base;
            public string MainSubtitle_StillLoading;
            public string NoMatches_BackgroundText;
        }

        protected enum ReloadReason
        {
            FirstRun,
            Automated,
            Manual,
            External
        }

        protected readonly bool DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START = false;
        protected readonly bool MEGA_QUERY_BOX_ENABLED = false;
        protected readonly bool SHOW_LAST_CHECKED_TIME = false;
        public readonly string INSTANT_SEARCH_SETTING_NAME;
        public readonly string SIDEPANEL_WIDTH_SETTING_NAME;
        protected readonly string PAGE_NAME;
        public readonly bool RoleIsUpdateLike;
        protected DateTime LastPackageLoadTime { get; private set; }
        protected readonly OperationType PAGE_ROLE;

        protected IPackage? SelectedItem
        {
            get => (PackageList.SelectedItem as PackageWrapper)?.Package;
        }

        protected AbstractPackageLoader Loader;
        public ObservablePackageCollection FilteredPackages = new();
        protected List<IPackageManager> UsedManagers = new();
        protected Dictionary<IPackageManager, List<IManagerSource>> UsedSourcesForManager = new();
        protected Dictionary<IPackageManager, TreeViewNode> RootNodeForManager = new();
        protected Dictionary<IManagerSource, TreeViewNode> NodesForSources = new();
        private readonly TreeViewNode LocalPackagesNode;
        public InfoBadge? ExternalCountBadge;

        public readonly int NewVersionLabelWidth;
        public readonly int NewVersionIconWidth;        
        private readonly bool AllSelected = true;

        protected abstract void WhenPackagesLoaded(ReloadReason reason);
        protected abstract void WhenPackageCountUpdated();
        protected abstract void WhenShowingContextMenu(IPackage package);
        public abstract void GenerateToolBar();
        public abstract BetterMenu GenerateContextMenu();

        protected readonly string NoPackages_BackgroundText;
        protected readonly string NoPackages_SourcesText; 
        protected readonly string MainSubtitle_StillLoading;
        protected readonly string NoPackages_SubtitleText_Base;
        protected readonly string NoMatches_BackgroundText;
        
        protected Func<int, int, string> FoundPackages_SubtitleText_Base = (a, b) => CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.", a, b);
        
        protected string NoPackages_SubtitleText
        {
            get => NoPackages_SubtitleText_Base + 
                (SHOW_LAST_CHECKED_TIME ? " " + CoreTools.Translate("(Last checked: {0})", LastPackageLoadTime.ToString()) : "");
        }
        protected string NoMatches_SubtitleText
        {
            get => FoundPackages_SubtitleText_Base(Loader.Packages.Count(), FilteredPackages.Count()) +
               (SHOW_LAST_CHECKED_TIME? " " + CoreTools.Translate("(Last checked: {0})", LastPackageLoadTime.ToString()): "");
        }
        protected string FoundPackages_SubtitleText { get => NoMatches_SubtitleText; }

        protected AbstractPackagesPage(PackagesPageData data)
        {
            InitializeComponent();
            
            Loader = data.Loader;

            DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START = data.DisableAutomaticPackageLoadOnStart;
            MEGA_QUERY_BOX_ENABLED = data.MegaQueryBlockEnabled;
            SHOW_LAST_CHECKED_TIME = data.ShowLastLoadTime;

            PAGE_ROLE = data.PageRole;
            RoleIsUpdateLike = PAGE_ROLE == OperationType.Update;
            NewVersionLabelWidth = RoleIsUpdateLike ? 125 : 0;
            NewVersionIconWidth = RoleIsUpdateLike ? 24 : 0;

            Loader = data.Loader;

            PAGE_NAME = data.PageName;
            INSTANT_SEARCH_SETTING_NAME = $"DisableInstantSearch{PAGE_NAME}Tab";
            SIDEPANEL_WIDTH_SETTING_NAME = $"SidepanelWidth{PAGE_NAME}Page";

            MainTitle.Text = data.PageTitle;
            HeaderIcon.Glyph = data.Glyph;

            NoPackages_BackgroundText = data.NoPackages_BackgroundText;
            NoPackages_SourcesText = data.NoPackages_SourcesText;
            NoPackages_SubtitleText_Base = data.NoPackages_SubtitleText_Base;
            MainSubtitle_StillLoading = data.MainSubtitle_StillLoading;

            NoMatches_BackgroundText = data.NoMatches_BackgroundText;

            SelectAllCheckBox.IsChecked = data.PackagesAreCheckedByDefault;
            QuerySimilarResultsRadio.IsEnabled = !data.DisableSuggestedResultsRadio;
            QueryOptionsGroup.SelectedIndex = 1;
            QueryOptionsGroup.SelectedIndex = 2;
            QueryOptionsGroup.SelectedItem = QueryBothRadio;

            Loader.StartedLoading += Loader_StartedLoading;
            Loader.FinishedLoading += Loader_FinishedLoading;
            Loader.PackagesChanged += Loader_PackagesChanged;

            if (Loader.IsLoading)
            {
                Loader_StartedLoading(this, EventArgs.Empty);
            }
            else
            {
                Loader_FinishedLoading(this, EventArgs.Empty);
                FilterPackages();
            }

            LastPackageLoadTime = DateTime.Now;
            LocalPackagesNode = new TreeViewNode { 
                Content = CoreTools.Translate("Local"), 
                IsExpanded = false 
            };
            
            ReloadButton.Click += async (s, e) => await LoadPackages();
            
            // Handle Find Button click on the Query Block
            FindButton.Click += (s, e) => { 
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                FilterPackages();
            };
            
            // Handle Enter pressed on the QueryBlock
            QueryBlock.KeyUp += (s, e) =>
            {
                if (e.Key != VirtualKey.Enter) return;

                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                FilterPackages();

            };

            // Handle showing the MegaQueryBlock
            QueryBlock.TextChanged += (s, e) =>
            {
                if (InstantSearchCheckbox.IsChecked == true) FilterPackages();

                if (!MEGA_QUERY_BOX_ENABLED || QueryBlock.Text.Trim() != "")
                    return;

                MegaQueryBlockGrid.Visibility = Visibility.Visible;
                BackgroundText.Visibility = Visibility.Collapsed;
                ClearPackageList();
                UpdatePackageCount();
                MegaQueryBlock.Focus(FocusState.Programmatic);
                MegaQueryBlock.Text = "";
            };

            // Handle the Enter Pressed event on the MegaQueryBlock
            MegaQueryBlock.KeyUp += (s, e) =>
            {
                if (e.Key != VirtualKey.Enter)
                    return;
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                QueryBlock.Text = MegaQueryBlock.Text.Trim();
                FilterPackages();
            };

            // Hande the MegaQueryBlock search button click
            MegaFindButton.Click += (s, e) =>
            {
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                QueryBlock.Text = MegaQueryBlock.Text.Trim();
                FilterPackages();
            };

            // Handle when a source is clicked
            SourcesTreeView.Tapped += (s, e) =>
            {
                TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                if (node == null) return;

                if (SourcesTreeView.SelectedNodes.Contains(node)) SourcesTreeView.SelectedNodes.Remove(node);
                else SourcesTreeView.SelectedNodes.Add(node);
                FilterPackages();
            };

            // Handle when a source is double-clicked
            SourcesTreeView.RightTapped += (s, e) =>
            {
                TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                if (node == null) return;

                SourcesTreeView.SelectedNodes.Clear();
                SourcesTreeView.SelectedNodes.Add(node);
                FilterPackages();
            };

            // Handle when a key is pressed on the package list
            PackageList.KeyUp += (s, e) =>
            {
                // TODO: Check if needed
            };

            if (MEGA_QUERY_BOX_ENABLED)
            {
                MegaQueryBlockGrid.Visibility = Visibility.Visible;
                MegaQueryBlock.Focus(FocusState.Programmatic);
                BackgroundText.Visibility = Visibility.Collapsed;
            }

            int width = 250;
            try
            {
                width = int.Parse(Settings.GetValue(SIDEPANEL_WIDTH_SETTING_NAME));
            }
            catch
            {
                Settings.SetValue(SIDEPANEL_WIDTH_SETTING_NAME, "250");
            }

            BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);
            QueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
            MegaQueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
            InstantSearchCheckbox.IsChecked = !Settings.Get(INSTANT_SEARCH_SETTING_NAME);

            HeaderIcon.FontWeight = new Windows.UI.Text.FontWeight(700);
            NameHeader.Content = CoreTools.Translate("Package Name");
            IdHeader.Content = CoreTools.Translate("Package ID");
            VersionHeader.Content = CoreTools.Translate("Version");
            NewVersionHeader.Content = CoreTools.Translate("New version");
            SourceHeader.Content = CoreTools.Translate("Source");

            NameHeader.Click += (s, e) => SortPackagesBy(ObservablePackageCollection.Sorter.Name);
            IdHeader.Click += (s, e) => SortPackagesBy(ObservablePackageCollection.Sorter.Id);
            VersionHeader.Click += (s, e) => SortPackagesBy(ObservablePackageCollection.Sorter.Version);
            NewVersionHeader.Click += (s, e) => SortPackagesBy(ObservablePackageCollection.Sorter.NewVersion);
            SourceHeader.Click += (s, e) => SortPackagesBy(ObservablePackageCollection.Sorter.Source);

            GenerateToolBar();
            PackageList.ContextFlyout = GenerateContextMenu();
        }

        private void Loader_PackagesChanged(object? sender, EventArgs e)
        {
            // Ensure we are in the UI thread
            if(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
            {
                DispatcherQueue.TryEnqueue(() => Loader_PackagesChanged(sender, e));
                return;
            }

            if (Loader.Packages.Count == 0)
            {
                ClearPackageList();
            }
            else
            {
                foreach (IPackage package in Loader.Packages)
                    AddPackageToSourcesList(package);
            }
            FilterPackages();
        }

        private void Loader_FinishedLoading(object? sender, EventArgs e)
        {
            // Ensure we are in the UI thread
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
            {
                DispatcherQueue.TryEnqueue(() => Loader_FinishedLoading(sender, e));
                return;
            }
            
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LastPackageLoadTime = DateTime.Now;
            WhenPackagesLoaded(ReloadReason.External);
            FilterPackages();
        }

        private void Loader_StartedLoading(object? sender, EventArgs e)
        {
            // Ensure we are in the UI thread
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
            {
                DispatcherQueue.TryEnqueue(() => Loader_StartedLoading(sender, e));
                return;
            }
            LoadingProgressBar.Visibility = Visibility.Visible;
            UpdatePackageCount();
        }
        public void SearchTriggered()
        {
            QueryBlock.Focus(FocusState.Pointer);
        }
        public void ReloadTriggered()
        {
            _ = LoadPackages(ReloadReason.Manual);
        }
        public void SelectAllTriggered()
        {
            if (AllSelected)
                FilteredPackages.SelectAll();
            else
                FilteredPackages.ClearSelection();
        }
        protected void AddPackageToSourcesList(IPackage package)
        {
            IManagerSource source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                TreeViewNode Node;
                Node = new TreeViewNode { Content = source.Manager.Name + "                                                                                    .", IsExpanded = false };
                SourcesTreeView.RootNodes.Add(Node);

                // Smart way to decide whether to check a source or not.
                // - Always check a source by default if no sources are present
                // - Otherwise, Check a source only if half of the sources have already been checked
                if(SourcesTreeView.RootNodes.Count == 0)
                    SourcesTreeView.SelectedNodes.Add(Node);
                else if (SourcesTreeView.SelectedNodes.Count >= SourcesTreeView.RootNodes.Count/2)
                    SourcesTreeView.SelectedNodes.Add(Node);


                RootNodeForManager.Add(source.Manager, Node);
                UsedSourcesForManager.Add(source.Manager, new List<IManagerSource>());
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
            if (QueryBothRadio == null) return; 
            FilterPackages(); 
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        { Settings.Set(INSTANT_SEARCH_SETTING_NAME, InstantSearchCheckbox.IsChecked == false); }
        private void SourcesTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        { FilterPackages(); }

        public virtual async Task LoadPackages()
        { await LoadPackages(ReloadReason.External); }

        protected void ClearPackageList()
        {
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();
        }

        /// <summary>
        /// Reload the packages for this Page
        /// Calling this method will trigger a reload on the associated PackageLoader, unless it is already loading packages.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected async Task LoadPackages(ReloadReason reason)
        {
            if(!(Loader.IsLoading) && (!Loader.IsLoaded || reason == ReloadReason.External || reason == ReloadReason.Manual || reason == ReloadReason.Automated))
            {
                Loader.ClearPackages();
                await Loader.ReloadPackages();
            }
            Loader_PackagesChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Will filter the packages with the query on QueryBlock.Text and put the 
        /// resulting packages on the ItemsView
        /// </summary>
        public void FilterPackages()
        {
            FilteredPackages.Clear();

            List<IManagerSource> VisibleSources = new();
            List<IPackageManager> VisibleManagers = new();

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

            IEnumerable<IPackage> MatchingList;

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

            string treatedQuery = CharsFunc(QueryBlock.Text.Trim());

            if (QueryIdRadio.IsChecked == true)
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Name).Contains(treatedQuery));
            else if (QueryNameRadio.IsChecked == true)
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Id).Contains(treatedQuery));
            else if (QueryBothRadio.IsChecked == true)
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Name).Contains(treatedQuery) | CharsFunc(x.Id).Contains(treatedQuery));
            else if (QueryExactMatch.IsChecked == true)
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Name) == treatedQuery | CharsFunc(x.Id) == treatedQuery);
            else // QuerySimilarResultsRadio == true
                MatchingList = Loader.Packages;

            FilteredPackages.BlockSorting = true;
            foreach (IPackage match in MatchingList)
            {
                if (VisibleManagers.Contains(match.Manager) || VisibleSources.Contains(match.Source))
                {
                    FilteredPackages.Add(match);
                }
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();

            UpdatePackageCount();
        }

        /// <summary>
        /// Updates the UI to reflect the current amount of packages
        /// </summary>
        public void UpdatePackageCount()
        {
            if (FilteredPackages.Count() == 0)
            {
                if(LoadingProgressBar.Visibility == Visibility.Collapsed)
                {
                    if (Loader.Packages.Count() == 0)
                    {
                        BackgroundText.Text = NoPackages_BackgroundText;
                        SourcesPlaceholderText.Text = NoPackages_SourcesText;
                        SourcesPlaceholderText.Visibility = Visibility.Visible;
                        MainSubtitle.Text = NoPackages_SubtitleText;
                    }
                    else
                    {
                        BackgroundText.Text = NoMatches_BackgroundText;
                        SourcesPlaceholderText.Visibility = Visibility.Collapsed;
                        MainSubtitle.Text = NoMatches_SubtitleText;
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }
                else
                {
                    BackgroundText.Visibility = FilteredPackages.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    BackgroundText.Text = MainSubtitle_StillLoading;
                    SourcesPlaceholderText.Visibility = Loader.Packages.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    SourcesPlaceholderText.Text = MainSubtitle_StillLoading;
                    MainSubtitle.Text = MainSubtitle_StillLoading;
                }
            }
            else
            {
                BackgroundText.Text = NoPackages_BackgroundText;
                BackgroundText.Visibility = Loader.Packages.Count > 0? Visibility.Collapsed: Visibility.Visible;
                MainSubtitle.Text = FoundPackages_SubtitleText;
            }

            if (ExternalCountBadge != null)
            {
                ExternalCountBadge.Visibility = Loader.Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                ExternalCountBadge.Value = Loader.Packages.Count;
            }

            if (MegaQueryBlockGrid.Visibility == Visibility.Visible)
                BackgroundText.Visibility = Visibility.Collapsed;
            
            WhenPackageCountUpdated();
        }

        /// <summary>
        /// Changes how the packages are sorted
        /// </summary>
        /// <param name="sorter">The information with which to sort the packages</param>
        public void SortPackagesBy(ObservablePackageCollection.Sorter sorter)
        {
            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SetSorter(sorter);
            FilteredPackages.Sort();
        }

        protected void SelectAllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectAll();
        }

        protected void ClearSourceSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectedItems.Clear();
            FilterPackages();
        }
        
        protected async void ShowDetailsForPackage(IPackage? package)
        {
            if (package == null) return;

            Logger.Warn(PAGE_ROLE.ToString());
            await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package, PAGE_ROLE);
        }
        
        protected void SharePackage(IPackage? package)
        {
            if (package == null) return;

            MainApp.Instance.MainWindow.SharePackage(package);
        }
        
        protected async void ShowInstallationOptionsForPackage(IPackage? package)
        {
            if(package == null)return;

            if (await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(package, PAGE_ROLE))
                PerformMainPackageAction(package);
        }

        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
                return;

            Settings.SetValue(SIDEPANEL_WIDTH_SETTING_NAME, ((int)e.NewSize.Width).ToString());
            foreach (UIElement control in SidePanelGrid.Children)
            {
                control.Visibility = e.NewSize.Width > 20 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        protected void PerformMainPackageAction(IPackage? package)
        {
            if(package == null) return;

            if (PAGE_ROLE == OperationType.Install)
                MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
            else if (PAGE_ROLE == OperationType.Update)
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            else // if (PageRole == OperationType.Uninstall)
                MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package));
        }

        public void FocusPackageList()
        { PackageList.Focus(FocusState.Programmatic); }

        private void PackageItemContainer_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            PackageItemContainer? container = (sender as PackageItemContainer);
            if (container is null) return;
            if (container.Package is null) return;
            PackageList.Select(container.Wrapper.Index);
            WhenShowingContextMenu(container.Package);
        }

        private void PackageItemContainer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PackageItemContainer? container = (sender as PackageItemContainer);
            if (container is null) return;
            PackageList.Select(container.Wrapper.Index);
            ShowDetailsForPackage(container.Package);
        }

        private void PackageItemContainer_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            IPackage? package = (sender as PackageItemContainer)?.Package;
            
            bool IS_CONTROL_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool IS_SHIFT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool IS_ALT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftMenu).HasFlag(CoreVirtualKeyStates.Down);
            IS_ALT_PRESSED |= InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightMenu).HasFlag(CoreVirtualKeyStates.Down);
            

            if (e.Key == VirtualKey.Enter && package is not null)
            {
                if (IS_ALT_PRESSED) ShowInstallationOptionsForPackage(package);
                else if (IS_CONTROL_PRESSED) PerformMainPackageAction(package);
                else ShowDetailsForPackage(package);
            }
            else if (e.Key == VirtualKey.Space && package is not null)
            {
                package.IsChecked = !package.IsChecked;
            }
        }

        private void SelectAllCheckBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked == true) 
                FilteredPackages.SelectAll();
            else 
                FilteredPackages.ClearSelection();
        }
    }
}
