using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Interface;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Managers;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using YamlDotNet.Serialization;

namespace UniGetUI
{
    public partial class MainApp : Application
    {
        public Scoop Scoop;
        public Winget Winget;
        public Chocolatey Choco;
        public Pip Pip;
        public Npm Npm;
        public Dotnet Dotnet;
        public PowerShell PowerShell;

        public bool RaiseExceptionAsFatal = true;

        public readonly List<PackageManager> PackageManagerList = new();

        public Interface.SettingsInterface settings;
        public MainWindow MainWindow;

        private const string WebViewFolder = "WingetUI\\WebView";
        private readonly string _webViewPath = Path.Join(Path.GetTempPath(), WebViewFolder);
        private BackgroundApiRunner BackgroundApi = new BackgroundApiRunner();

        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Managers initialization

        public MainApp()
        {
            try
            {
                InitializeComponent();

                string preferredTheme = AppTools.GetSettingsValue_Static("PreferredTheme");
                if (preferredTheme == "dark")
                {
                    RequestedTheme = ApplicationTheme.Dark;
                }
                else if (preferredTheme == "light")
                {
                    RequestedTheme = ApplicationTheme.Light;
                }


                RegisterErrorHandling();
                SetUpWebViewUserDataFolder();
                InitializeMainWindow();
                ClearNotificationHistory_Safe();
                RegisterNotificationActivationEvent_Safe();

                LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        private void RegisterErrorHandling()
        {
            UnhandledException += (sender, e) =>
            {
                var message = $"Unhandled Exception raised: {e.Message}";
                var stackTrace = $"Stack Trace: \n{e.Exception.StackTrace}";
                AppTools.Log(" -");
                AppTools.Log(" -");
                AppTools.Log("  ⚠️⚠️⚠️ START OF UNHANDLED ERROR TRACE ⚠️⚠️⚠️");
                AppTools.Log(message);
                AppTools.Log(stackTrace);
                AppTools.Log("  ⚠️⚠️⚠️  END OF UNHANDLED ERROR TRACE  ⚠️⚠️⚠️");
                AppTools.Log(" -");
                AppTools.Log(" -");
                if (Environment.GetCommandLineArgs().Contains("--report-all-errors") || RaiseExceptionAsFatal || MainWindow == null)
                    AppTools.ReportFatalException(e.Exception);
                else
                {
                    MainWindow.ErrorBanner.Title = AppTools.Instance.Translate("Something went wrong");
                    MainWindow.ErrorBanner.Message = AppTools.Instance.Translate("An interal error occurred. Please view the log for further details.");
                    MainWindow.ErrorBanner.IsOpen = true;
                    var button = new Button()
                    {
                        Content = AppTools.Instance.Translate("WingetUI log"),
                    };
                    button.Click += (sender, args) =>
                    {
                        MainWindow.NavigationPage.WingetUILogs_Click(sender, args);
                    };
                    MainWindow.ErrorBanner.ActionButton = button;
                    e.Handled = true;
                }
            };
        }

        private void SetUpWebViewUserDataFolder()
        {
            try
            {
                if (!Directory.Exists(_webViewPath))
                    Directory.CreateDirectory(_webViewPath);
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", _webViewPath);
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }
        }

        /// <summary>
        /// Initialize the main window
        /// </summary>
        private void InitializeMainWindow()
        {
            MainWindow = new MainWindow
            {
                BlockLoading = true
            };
            MainWindow.Closed += (sender, args) => DisposeAndQuit(0);
            
            var hWnd = MainWindow.GetWindowHandle();
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if(appWindow != null)
                appWindow.Closing += MainWindow.HandleClosingEvent;
        }


        /// <summary>
        /// Clear the notification history, if possible
        /// </summary>
        private void ClearNotificationHistory_Safe()
        {
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Register the notification activation event
        /// </summary>
        private void RegisterNotificationActivationEvent_Safe()
        {
            try
            {
                ToastNotificationManagerCompat.OnActivated += toastArgs =>
                {
                    var args = ToastArguments.Parse(toastArgs.Argument);
                    var userInput = toastArgs.UserInput;

                    MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow.HandleNotificationActivation(args, userInput);
                    });
                };
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Background component loader 
        /// </summary>
        /// <returns></returns>
        private async Task LoadComponentsAsync()
        {
            try
            { 
                InitializePackageManagers();

                // Run other initializations asynchronously
                AppTools.Instance.UpdateWingetUIIfPossible();
                _ = CoreData.LoadIconAndScreenshotsDatabase();
                if(!AppTools.Instance.GetSettings("DisableApi"))
                    _ = BackgroundApi.Start();

                _ = MainWindow.DoEntryTextAnimationAsync();

                await InitializeAllManagersAsync();

                AppTools.Log("LoadComponentsAsync finished executing. All managers loaded. Proceeding to interface.");
                MainWindow.SwitchToInterface();
                RaiseExceptionAsFatal = false;

                if (Environment.GetCommandLineArgs().Contains("--load-and-quit"))
                    DisposeAndQuit(0);
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        /// <summary>
        /// Constructs Package Manager objects
        /// </summary>
        private void InitializePackageManagers()
        {
            Winget = new Winget();
            Scoop = new Scoop();
            Choco = new Chocolatey();
            Pip = new Pip();
            Npm = new Npm();
            Dotnet = new Dotnet();
            PowerShell = new PowerShell();

            PackageManagerList.AddRange(new PackageManager[]
            {
                Winget,
                Scoop,
                Choco,
                Pip,
                Npm,
                Dotnet,
                PowerShell
            });
        }

        /// <summary>
        /// Initializes Package Manager objects (asynchronously)
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAllManagersAsync()
        {
            var initializeTasks = new List<Task>();

            foreach (var manager in PackageManagerList)
            {
                initializeTasks.Add(manager.InitializeAsync());
            }


            var ManagersMetaTask = Task.WhenAll(initializeTasks);
            try
            {
                await ManagersMetaTask.WaitAsync(TimeSpan.FromMilliseconds(ManagerLoadTimeout));
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }
            if (ManagersMetaTask.IsCompletedSuccessfully == false)
                AppTools.Log("Timeout: Not all package managers have finished initializing.");
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!CoreData.IsDaemon)
            {
                await ShowMainWindowFromRedirectAsync();
            }

            CoreData.IsDaemon = false;
        }

        public async Task ShowMainWindowFromRedirectAsync()
        {
            while (MainWindow == null)
            {
                await Task.Delay(100);
            }

            MainWindow.DispatcherQueue.TryEnqueue(MainWindow.Activate);
        }

        public void DisposeAndQuit(int outputCode = 0)
        {
            AppTools.Log("Quitting...");
            MainWindow?.Close();
            BackgroundApi?.Stop();
            Environment.Exit(outputCode);
        }
    }
}
