using CommunityToolkit.WinUI.Notifications;
using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using UniGetUI.Core.Data;
using UniGetUI.Interface;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation.Collections;


namespace UniGetUI.Interface
{
    public sealed partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [System.Runtime.InteropServices.InterfaceType(
            System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
                [System.Runtime.InteropServices.In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }

        TaskbarIcon TrayIcon;
        private bool RecentlyActivated = false;

        static readonly Guid _dtm_iid =
            new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        AppTools Tools = AppTools.Instance;
        public MainView NavigationPage;
        public Grid ContentRoot;
        public bool BlockLoading = false;
        ContentDialog LoadingSthDalog;

        private int LoadingDialogCount = 0;

        public List<ContentDialog> DialogQueue = new();

        public List<NavButton> NavButtonList = new();
        public MainWindow()
        {
            InitializeComponent();
            LoadTrayMenu();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(__content_root);
            ContentRoot = __content_root;
            ApplyTheme();

            AppWindow.SetIcon(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Images", "icon.ico"));
            if (Tools.IsAdministrator())
            {
                Title = "WingetUI " + Tools.Translate("[RAN AS ADMINISTRATOR]");
                AppTitle.Text = Title;
            }

            LoadingSthDalog = new ContentDialog();
            LoadingSthDalog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            LoadingSthDalog.Title = Tools.Translate("Please wait");
            LoadingSthDalog.Content = new ProgressBar() { IsIndeterminate = true, Width = 300 };
        }

        public void HandleNotificationActivation(ToastArguments args, ValueSet input)
        {
            if (args.Contains("action") && args["action"] == "updateAll")
                NavigationPage.UpdatesPage.UpdateAll();
            else if (args.Contains("action") && args["action"] == "openWingetUIOnUpdatesTab")
            {
                NavigationPage.UpdatesNavButton.ForceClick();

                if (NavigationPage != null && NavigationPage.InstalledPage != null)
                    NavigationPage.InstalledPage.ReloadPackages();
                Activate();
            }
            else
            {

                if (NavigationPage != null && NavigationPage.InstalledPage != null)
                    NavigationPage.InstalledPage.ReloadPackages();
                Activate();
            }
            AppTools.Log("Notification activated: " + args.ToString() + " " + input.ToString());
        }


        /// <summary>
        /// Handle the window closing event, and divert it when the window must be hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public async void HandleClosingEvent(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (!Tools.GetSettings("DisableSystemTray"))
            {
                args.Cancel = true;
                RecentlyActivated = false;
                try
                {
                    this.Hide(enableEfficiencyMode: true);
                }
                catch (Exception ex)
                {
                    // Somewhere, Sometimes, MS Window Efficiency mode just crashes
                    AppTools.Log(ex);
                    this.Hide(enableEfficiencyMode: false);
                }
            }
            else
            {
                if (Tools.OperationQueue.Count > 0)
                {
                    args.Cancel = true;
                    ContentDialog d = new ContentDialog();
                    d.XamlRoot = NavigationPage.XamlRoot;
                    d.Title = Tools.Translate("Operation in progress");
                    d.Content = Tools.Translate("There are ongoing operations. Quitting WingetUI may cause them to fail. Do you want to continue?");
                    d.PrimaryButtonText = Tools.Translate("Quit");
                    d.SecondaryButtonText = Tools.Translate("Cancel");
                    d.DefaultButton = ContentDialogButton.Secondary;

                    var result = await ShowDialogAsync(d);
                    if (result == ContentDialogResult.Primary)
                        Tools.App.DisposeAndQuit();
                }
            }
        }

        public new void Activate()
        {
            if (NavigationPage != null && NavigationPage.InstalledPage != null)
                NavigationPage.InstalledPage.ReloadPackages();

            (this as Window).Activate();
        }

        public void HideWindow()
        {
            this.Hide();
        }

        private void LoadTrayMenu()
        {
            MenuFlyout TrayMenu = new();

            XamlUICommand DiscoverPackages = new();
            XamlUICommand AvailableUpdates = new();
            XamlUICommand InstalledPackages = new();
            XamlUICommand AboutWingetUI = new();
            XamlUICommand ShowWingetUI = new();
            XamlUICommand QuitWingetUI = new();

            Dictionary<XamlUICommand, string> Labels = new()
            {
                { DiscoverPackages, "Discover Packages" },
                { AvailableUpdates, "Available Updates" },
                { InstalledPackages, "Installed Packages" },
                { AboutWingetUI, "WingetUI Version {0}" },
                { ShowWingetUI, "Show WingetUI" },
                { QuitWingetUI, "Quit" },
            };

            foreach (KeyValuePair<XamlUICommand, string> item in Labels)
            {
                item.Key.Label = Tools.Translate(item.Value);
            }

            Dictionary<XamlUICommand, string> Icons = new()
            {
                { DiscoverPackages,  "\uF6FA"},
                { AvailableUpdates,  "\uE977"},
                { InstalledPackages,  "\uE895"},
                { AboutWingetUI,  "\uE946"},
                { ShowWingetUI,  "\uE8A7"},
                { QuitWingetUI,  "\uE711"},
            };

            foreach (KeyValuePair<XamlUICommand, string> item in Icons)
            {
                item.Key.IconSource = new FontIconSource { Glyph = item.Value };
            }

            DiscoverPackages.ExecuteRequested += (s, e) => { NavigationPage.DiscoverNavButton.ForceClick(); Activate(); };
            AvailableUpdates.ExecuteRequested += (s, e) => { NavigationPage.UpdatesNavButton.ForceClick(); Activate(); };
            InstalledPackages.ExecuteRequested += (s, e) => { NavigationPage.InstalledNavButton.ForceClick(); Activate(); };
            AboutWingetUI.Label = Tools.Translate("WingetUI Version {0}").Replace("{0}", CoreData.VersionName);
            ShowWingetUI.ExecuteRequested += (s, e) => { Activate(); };
            QuitWingetUI.ExecuteRequested += (s, e) => { Tools.App.DisposeAndQuit(); };

            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = DiscoverPackages });
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = AvailableUpdates });
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = InstalledPackages });
            TrayMenu.Items.Add(new MenuFlyoutSeparator());
            MenuFlyoutItem _about = new() { Command = AboutWingetUI };
            _about.IsEnabled = false;
            TrayMenu.Items.Add(_about);
            TrayMenu.Items.Add(new MenuFlyoutSeparator());
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = ShowWingetUI });
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = QuitWingetUI });


            TrayMenu.AreOpenCloseAnimationsEnabled = false;

            TrayIcon = new TaskbarIcon();
            __content_root.Children.Add(TrayIcon);
            TrayIcon.ContextMenuMode = H.NotifyIcon.ContextMenuMode.PopupMenu;

            XamlUICommand ShowHideCommand = new();
            ShowHideCommand.ExecuteRequested += async (s, e) =>
            {
                if (!RecentlyActivated)
                {
                    Activate();
                    RecentlyActivated = true;
                    await Task.Delay(5000);
                    RecentlyActivated = false;
                }
                else
                {
                    RecentlyActivated = false;
                    this.Hide();
                }
            };

            TrayIcon.LeftClickCommand = ShowHideCommand;
            TrayIcon.DoubleClickCommand = ShowHideCommand;
            TrayIcon.NoLeftClickDelay = true;
            TrayIcon.ContextFlyout = TrayMenu;
            UpdateSystemTrayStatus();
        }

        public void UpdateSystemTrayStatus()
        {
            string modifier = "_empty";
            string tooltip = Tools.Translate("Everything is up to date") + " - " + Title;

            if (Tools.TooltipStatus.OperationsInProgress > 0)
            {
                modifier = "_blue";
                tooltip = Tools.Translate("Operation in progress") + " - " + Title;
            }
            else if (Tools.TooltipStatus.ErrorsOccurred > 0)
            {
                modifier = "_orange";
                tooltip = Tools.Translate("Attention required") + " - " + Title ;
            }
            else if (Tools.TooltipStatus.RestartRequired)
            {
                modifier = "_turquoise";
                tooltip = Tools.Translate("Restart required") + " - " + Title;
            }
            else if (Tools.TooltipStatus.AvailableUpdates > 0)
            {
                modifier = "_green";
                if (Tools.TooltipStatus.AvailableUpdates == 1)
                    tooltip = Tools.Translate("1 update is available") + " - " + Title;
                else
                    tooltip = Tools.Translate("{0} updates are available").Replace("{0}", Tools.TooltipStatus.AvailableUpdates.ToString()) + " - " + Title;
            }

            TrayIcon.ToolTipText = tooltip;

            ApplicationTheme theme = ApplicationTheme.Light;
            string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            string RegistryValueName = "SystemUsesLightTheme";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            object registryValueObject = key?.GetValue(RegistryValueName);
            if (registryValueObject != null)
            {
                int registryValue = (int)registryValueObject;
                theme = registryValue > 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
            if (theme == ApplicationTheme.Light)
                modifier += "_black";
            else
                modifier += "_white";


            string FullIconPath = Path.Join(Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString(), "\\Assets\\Images\\tray" + modifier + ".ico");

            TrayIcon.SetValue(TaskbarIcon.IconSourceProperty, new BitmapImage() { UriSource = new Uri(FullIconPath) });
        }


        public void SwitchToInterface()
        {
            SetTitleBar(__app_titlebar);
            ContentRoot = __content_root;


            NavigationPage = new MainView();
            Grid.SetRow(NavigationPage, 3);
            Grid.SetColumn(NavigationPage, 0);
            MainContentGrid.Children.Add(NavigationPage);

            ColumnDefinition ContentColumn = __content_root.ColumnDefinitions[1];
            ContentColumn.Width = new GridLength(1, GridUnitType.Star);

            ColumnDefinition SpashScreenColumn = __content_root.ColumnDefinitions[0];
            SpashScreenColumn.Width = new GridLength(0, GridUnitType.Pixel);
        }

        public void ApplyTheme()
        {
            string preferredTheme = Tools.GetSettingsValue("PreferredTheme");
            if (preferredTheme == "dark")
            {
                Tools.ThemeListener.CurrentTheme = ApplicationTheme.Dark;
                ContentRoot.RequestedTheme = ElementTheme.Dark;
            }
            else if (preferredTheme == "light")
            {
                Tools.ThemeListener.CurrentTheme = ApplicationTheme.Light;
                ContentRoot.RequestedTheme = ElementTheme.Light;
            }
            else
            {
                if (ContentRoot.ActualTheme == ElementTheme.Dark)
                    Tools.ThemeListener.CurrentTheme = ApplicationTheme.Dark;
                else
                    Tools.ThemeListener.CurrentTheme = ApplicationTheme.Light;
                ContentRoot.RequestedTheme = ElementTheme.Default;
            }

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                if (Tools.ThemeListener.CurrentTheme == ApplicationTheme.Light)
                    AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                else
                    AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
            }
            else
            {
                AppTools.Log("Taskbar foreground color customization is not available");
            }



        }

        public void ShowLoadingDialog(string text)
        {
            if(LoadingDialogCount == 0 && DialogQueue.Count == 0)
            {
                LoadingSthDalog.Title = text;
                LoadingSthDalog.XamlRoot = NavigationPage.XamlRoot;
                _ = LoadingSthDalog.ShowAsync();
            }
            LoadingDialogCount++;
        }

        public void HideLoadingDialog()
        {
            LoadingDialogCount--;
            if(LoadingDialogCount <= 0)
            {
                LoadingSthDalog.Hide();
            }
            if (LoadingDialogCount < 0)
                LoadingDialogCount = 0;
        }

        public void SharePackage(Package package)
        {
            if (package == null)
                return;

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            IDataTransferManagerInterop interop =
            Windows.ApplicationModel.DataTransfer.DataTransferManager.As
                <IDataTransferManagerInterop>();

            IntPtr result = interop.GetForWindow(hWnd, _dtm_iid);
            DataTransferManager dataTransferManager = WinRT.MarshalInterface
                <Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);

            dataTransferManager.DataRequested += (sender, args) =>
            {
                DataRequest dataPackage = args.Request;
                Uri ShareUrl = new("https://marticliment.com/wingetui/share?pid=" + System.Web.HttpUtility.UrlEncode(package.Id) + "&pname=" + System.Web.HttpUtility.UrlEncode(package.Name) + "&psource=" + System.Web.HttpUtility.UrlEncode(package.Source.ToString()));
                dataPackage.Data.SetWebLink(ShareUrl);
                dataPackage.Data.Properties.Title = "Sharing " + package.Name;
                dataPackage.Data.Properties.ApplicationName = "WingetUI";
                dataPackage.Data.Properties.ContentSourceWebLink = ShareUrl;
                dataPackage.Data.Properties.Description = "Share " + package.Name + " with your friends";
                dataPackage.Data.Properties.PackageFamilyName = "WingetUI";
            };

            interop.ShowShareUIForWindow(hWnd);

        }

        public IntPtr GetWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(this);
        }

        public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, bool HighPriority = false)
        {
            try
            {
                if (HighPriority && DialogQueue.Count >= 1)
                    DialogQueue.Insert(1, dialog);
                else
                    DialogQueue.Add(dialog);

                while (DialogQueue[0] != dialog)
                    await Task.Delay(100);
                dialog.RequestedTheme = ContentRoot.RequestedTheme;
                ContentDialogResult result = await dialog.ShowAsync();
                DialogQueue.Remove(dialog);
                return result;
            }
            catch (Exception e)
            {
                AppTools.Log(e);
                if (DialogQueue.Contains(dialog))
                    DialogQueue.Remove(dialog);
                return ContentDialogResult.None;
            }
        }

        public async Task DoEntryTextAnimationAsync()
        {
            InAnimation_Border.Start();
            InAnimation_Text.Start();
            await Task.Delay(700);
            LoadingIndicator.Visibility = Visibility.Visible;
        }

        public async Task DoExitTextAnimationAsync()
        {
            await Task.Delay(1000);
            LoadingIndicator.Visibility = Visibility.Collapsed;
            OutAnimation_Text.Start();
            OutAnimation_Border.Start();
            await Task.Delay(400);
        }
    }
}
