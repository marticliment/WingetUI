using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModernWindow.Essentials;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.Core;
using YamlDotNet.Serialization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    public partial class PackageBundlePage : Page
    {
        public ObservableCollection<BundledPackage> Packages = new();
        public SortableObservableCollection<BundledPackage> FilteredPackages = new() { SortingSelector = (a) => (a.Package.Name) };
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
        TreeViewNode LocalPackagesNode;
        int lastSavedWidth = 0;

        public string InstantSearchSettingString = "DisableInstantSearchInstalledTab";
        public PackageBundlePage()
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
            ReloadButton.Visibility = Visibility.Collapsed;
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
                _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
            };

            PackageList.RightTapped += (s, e) =>
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    try
                    {
                        if (element.DataContext != null && element.DataContext is BundledPackage package)
                        {
                            PackageList.SelectedItem = package;
                            MenuInstallSettings.IsEnabled = MenuDetails.IsEnabled = MenuShare.IsEnabled = package.IsValid;
                            PackageContextMenu.ShowAt(PackageList, e.GetPosition(PackageList));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppTools.Log(ex);
                    }
                }
            };

            PackageList.KeyUp += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && PackageList.SelectedItem != null)
                {
                    if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                        (PackageList.SelectedItem as BundledPackage).ShowOptions(s, e);
                    else
                        _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
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
                    ((PackageList.SelectedItem as BundledPackage).Package).IsChecked = !((PackageList.SelectedItem as BundledPackage).Package).IsChecked;
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
                width = int.Parse(Tools.GetSettingsValue("SidepanelWidthBundlesPage"));
            }
            catch
            {
                Tools.SetSettingsValue("SidepanelWidthBundlesPage", "250");
            }
            BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);


            GenerateToolBar();
            LoadInterface();
            QueryBlock.PlaceholderText = Tools.Translate("Search for packages");
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

        public void ClearList()
        {
            Packages.Clear();
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();
            LocalPackagesNode.Children.Clear();
            AppTools.Instance.App.MainWindow.NavigationPage.BundleBadge.Visibility = Visibility.Collapsed;
            FilterPackages("");
            BackgroundText.Text = Tools.AutoTranslated("No packages have been added yet");
            BackgroundText.Visibility = Visibility.Visible;
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

            BundledPackage[] MatchingList;

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
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Name).Contains(CharsFunc(query))).ToArray();
            else if (QueryNameRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Id).Contains(CharsFunc(query))).ToArray();
            else // QueryBothRadio.IsChecked == true
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Name).Contains(CharsFunc(query)) | CharsFunc(x.Package.Id).Contains(CharsFunc(query))).ToArray();

            FilteredPackages.BlockSorting = true;
            int HiddenPackagesDueToSource = 0;
            foreach (BundledPackage match in MatchingList)
            {
                if ((VisibleManagers.Contains(match.Package.Manager) && match.Package.Manager != Tools.App.Winget) || VisibleSources.Contains(match.Package.Source))
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
                    if (Packages.Count() == 0)
                    {
                        BackgroundText.Text = SourcesPlaceholderText.Text = Tools.AutoTranslated("We couldn't find any package");
                        SourcesPlaceholderText.Text = Tools.AutoTranslated("No packages found");
                        MainSubtitle.Text = Tools.AutoTranslated("No packages found");
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

            if(FilteredPackages.Count > 0)
                PackageList.ScrollIntoView(FilteredPackages[0]);
        }

        public void LoadInterface()
        {
            if (!Initialized)
                return;
            MainTitle.Text = Tools.AutoTranslated("Package Bundles");
            HeaderIcon.Glyph = "\uF133";
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

        public void UpdateCount()
        {
            BackgroundText.Visibility = Packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Tools.App.MainWindow.NavigationPage.BundleBadge.Value = Packages.Count;
            Tools.App.MainWindow.NavigationPage.BundleBadge.Visibility = Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public void GenerateToolBar()
        {
            if (!Initialized)
                return;
            AppBarButton InstallPackages = new();
            AppBarButton OpenBundle = new();
            AppBarButton NewBundle = new();

            AppBarButton RemoveSelected = new();

            AppBarButton ExportBundle = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(NewBundle);
            ToolBar.PrimaryCommands.Add(OpenBundle);
            ToolBar.PrimaryCommands.Add(ExportBundle);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallPackages);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(new AppBarElementContainer() { Content = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Stretch } });
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
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
                { NewBundle,              Tools.Translate("New bundle") },
                { InstallPackages,        Tools.Translate("Install selection") },
                { OpenBundle,             Tools.Translate("Open existing bundle") },
                { RemoveSelected,         Tools.Translate("Remove selection from bundle") },
                { ExportBundle,           Tools.Translate("Save bundle as") },
                { PackageDetails,         " " + Tools.Translate("Package details") },
                { SharePackage,           " " + Tools.Translate("Share") },
                { SelectAll,              " " + Tools.Translate("Select all") },
                { SelectNone,             " " + Tools.Translate("Clear selection") },
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
                { NewBundle,              "add_to" },
                { InstallPackages,        "newversion" },
                { OpenBundle,             "openfolder" },
                { RemoveSelected,         "trash" },
                { ExportBundle,           "save" },
                { PackageDetails,         "info" },
                { SharePackage,           "share" },
                { SelectAll,              "selectall" },
                { SelectNone,             "selectnone" },
                { HelpButton,             "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
            };

            HelpButton.Click += (s, e) => { Tools.App.MainWindow.NavigationPage.ShowHelp(); };

            NewBundle.Click += (s, e) =>
            {
                ClearList();
            };

            RemoveSelected.Click += (s, e) =>
            {
                foreach (BundledPackage package in FilteredPackages.ToArray())
                    if (package.IsChecked)
                    {
                        FilteredPackages.Remove(package);
                        Packages.Remove(package);
                    }
                UpdateCount();
            };

            InstallPackages.Click += async (s, e) =>
            {
                Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Preparing packages, please wait..."));
                foreach (BundledPackage package in FilteredPackages.ToArray())
                    if (package.IsChecked && package.IsValid)
                    {
                        // Actually import settings
                        package.InstallOptions.SaveOptionsToDisk();

                        if (package.UpdateOptions.IgnoredVersion != "")
                            await package.Package.AddToIgnoredUpdatesAsync(package.UpdateOptions.IgnoredVersion);
                        else
                            await package.Package.RemoveFromIgnoredUpdatesAsync();
                    }


                Tools.App.MainWindow.HideLoadingDialog();

                foreach (BundledPackage package in FilteredPackages.ToArray())
                    if (package.IsChecked && package.IsValid)
                        Tools.AddOperationToList(new InstallPackageOperation(package.Package));

            };

            OpenBundle.Click += (s, e) =>
            {
                ClearList();
                OpenFile();
            };

            ExportBundle.Click += (s, e) =>
            {
                SaveFile();
            };

            SharePackage.Click += (s, e) => {
                if (PackageList.SelectedItem as BundledPackage != null)
                    Tools.App.MainWindow.SharePackage((PackageList.SelectedItem as BundledPackage).Package); };

            SelectAll.Click += (s, e) => { SelectAllItems(); };
            SelectNone.Click += (s, e) => { ClearItemSelection(); };

        }
        private void MenuRemoveFromList_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;

            (PackageList.SelectedItem as BundledPackage).RemoveFromList(sender, package);

        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null || !(PackageList.SelectedItem as BundledPackage).IsValid)
                return;
            Tools.App.MainWindow.SharePackage(((PackageList.SelectedItem as BundledPackage).Package));
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null || !(PackageList.SelectedItem as BundledPackage).IsValid)
                return;
            _ = Tools.App.MainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
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

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            if ((PackageList.SelectedItem as BundledPackage).Package != null)
                (PackageList.SelectedItem as BundledPackage).ShowOptions(sender, e);
        }

        private void SelectAllItems()
        {
            foreach (BundledPackage package in FilteredPackages.ToArray())
                package.IsChecked = true;
            AllSelected = true;
        }

        private void ClearItemSelection()
        {
            foreach (BundledPackage package in FilteredPackages.ToArray())
                package.IsChecked = false;
            AllSelected = false;
        }

        public async Task AddPackages(IEnumerable<Package> packages)
        {
            Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Preparing packages, please wait..."));
            var bundled = new List<BundledPackage>();
            foreach (var pkg in packages)
            {
                if (pkg.Source.IsVirtualManager)
                    bundled.Add(new InvalidBundledPackage(pkg));
                else
                    bundled.Add(await BundledPackage.FromPackageAsync(pkg));
            }

            foreach (var pkg in bundled)
                AddPackage(pkg);
            Tools.App.MainWindow.HideLoadingDialog();

        }

        public void AddPackage(BundledPackage package)
        {
            Packages.Add(package);
            AddPackageToSourcesList(package.Package);
            BackgroundText.Visibility = Packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Tools.App.MainWindow.NavigationPage.BundleBadge.Value = Packages.Count;
            Tools.App.MainWindow.NavigationPage.BundleBadge.Visibility = Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            FilterPackages(QueryBlock.Text.Trim());
        }


        public async void OpenFile()
        {
            try
            {
                // Select file
                var picker = new Pickers.FileOpenPicker(Tools.App.MainWindow.GetWindowHandle());
                var file = picker.Show(new List<string>() {"*.json", "*.yaml", "*.xml" });
                if (file == String.Empty)
                    return;

                Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Loading packages, please wait..."));

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
                await AddPackagesFromBundleString(fileContent, formatType);

                Tools.App.MainWindow.HideLoadingDialog();

            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
                Tools.App.MainWindow.HideLoadingDialog();
            }
        }
        public async Task AddPackagesFromBundleString(string content, BundleFormatType format)
        {
            // Deserialize data
            SerializableBundle_v1 DeserializedData;
            if (format == BundleFormatType.JSON)
                DeserializedData = JsonSerializer.Deserialize<SerializableBundle_v1>(content);
            else if (format == BundleFormatType.YAML)
            {
                IDeserializer deserializer = new DeserializerBuilder()
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

            // Load individual packages
            Dictionary<DeserializedPackageStatus, List<string>> InvalidPackages = new()
            {
                {DeserializedPackageStatus.ManagerNotFound, new List<string>() },
                {DeserializedPackageStatus.ManagerNotEnabled, new List<string>() },
                {DeserializedPackageStatus.ManagerNotReady, new List<string>() },
                {DeserializedPackageStatus.SourceNotFound, new List<string>() },
            };

            // Get a list of all managers
            Dictionary<string, PackageManager> ManagerSourceReference = new();
            foreach (PackageManager manager in AppTools.Instance.App.PackageManagerList)
            {
                ManagerSourceReference.Add(manager.Name, manager);
            }

            foreach (SerializableValidPackage_v1 DeserializedPackage in DeserializedData.packages)
            {
                // Check if the manager exists
                if (!ManagerSourceReference.ContainsKey(DeserializedPackage.ManagerName))
                {
                    AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                    continue;
                }
                PackageManager PackageManager = ManagerSourceReference[DeserializedPackage.ManagerName];

                // Handle a disabled manager
                if (!PackageManager.IsEnabled())
                {
                    AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                    continue;
                }
                // Handle a nonworking manager
                if (!PackageManager.Status.Found)
                {
                    AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                    continue;
                }

                ManagerSource Source = PackageManager.GetMainSource();

                if (PackageManager.Capabilities.SupportsCustomSources && PackageManager is PackageManagerWithSources)
                {
                    // Check if the source exists
                    string SourceName = DeserializedPackage.Source.Split(':')[^1].Trim();
                    Source = (PackageManager as PackageManagerWithSources).SourceFactory.GetSourceIfExists(SourceName);

                    if (Source == null)
                    {
                        AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                        continue;
                    }
                }

                Package package = new(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, Source, PackageManager);

                InstallationOptions InstallOptions = InstallationOptions.FromSerialized(DeserializedPackage.InstallationOptions, package);
                SerializableUpdatesOptions_v1 UpdateOptions = DeserializedPackage.Updates;

                BundledPackage newPackage = new(package, InstallOptions, UpdateOptions);
                AddPackage(newPackage);
            }
        }

        public async static Task<string> GetBundleStringFromPackages(BundledPackage[] packages, BundleFormatType formatType = BundleFormatType.JSON)
        {
            SerializableBundle_v1 exportable = new();
            foreach (BundledPackage package in packages)
                if (!package.IsValid)
                    exportable.incompatible_packages.Add(package.AsSerializable_Incompatible());
                else
                    exportable.packages.Add(package.AsSerializable());

            AppTools.Log("Finished loading serializable objects. Serializing with format " + formatType.ToString());
            string ExportableData;

            if (formatType == BundleFormatType.JSON)
                ExportableData = JsonSerializer.Serialize<SerializableBundle_v1>(exportable, new JsonSerializerOptions() { WriteIndented = true });
            else if (formatType == BundleFormatType.YAML)
            {
                ISerializer serializer = new SerializerBuilder()
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

            AppTools.Log("Finished serializing");

            return ExportableData;
        }
        public async void SaveFile()
        {
            try
            {
                // Get file 
                // Save file
                var file = (new Pickers.FileSavePicker(Tools.App.MainWindow.GetWindowHandle())).Show(new List<string>() { "*.json", "*.yaml", "*.xml" }, Tools.Translate("Package bundle") + ".json") ;
                if (file != String.Empty)
                {
                    // Loading dialog
                    Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Saving packages, please wait..."));

                    List<BundledPackage> packages = new();
                    foreach (BundledPackage package in Packages)
                        packages.Add(package);

                    // Select appropriate format
                    BundleFormatType formatType;
                    if (file.Split(':')[^1].ToLower() == "yaml")
                        formatType = BundleFormatType.YAML;
                    else if (file.Split(':')[^1].ToLower() == "xml")
                        formatType = BundleFormatType.XML;
                    else
                        formatType = BundleFormatType.JSON;

                    // Save serialized data
                    await File.WriteAllTextAsync(file, await GetBundleStringFromPackages(packages.ToArray(), formatType));

                    Tools.App.MainWindow.HideLoadingDialog();

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
                Tools.App.MainWindow.HideLoadingDialog();
                AppTools.Log(ex);
            }
        }
        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
                return;

            lastSavedWidth = ((int)(e.NewSize.Width / 10));
            Tools.SetSettingsValue("SidepanelWidthBundlesPage", ((int)e.NewSize.Width).ToString());
            foreach (var control in SidePanelGrid.Children)
            {
                control.Visibility = e.NewSize.Width > 20 ? Visibility.Visible : Visibility.Collapsed;
            }
        }


    }
}
