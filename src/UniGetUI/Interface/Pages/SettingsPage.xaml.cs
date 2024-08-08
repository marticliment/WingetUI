using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsInterface : Page
    {
        private readonly HyperlinkButton ResetBackupDirectory;
        private readonly HyperlinkButton OpenBackupDirectory;
        private readonly TextBlock BackupDirectoryLabel;

        public SettingsInterface()
        {
            InitializeComponent();

            // General Settings Section
            Dictionary<string, string> lang_dict = new(LanguageData.LanguageReference.AsEnumerable());

            foreach (string key in lang_dict.Keys)
            {
                if (key != "en" && LanguageData.TranslationPercentages.TryGetValue(key, out var translationPercentage))
                {
                    lang_dict[key] = lang_dict[key] + " (" + translationPercentage + ")";
                }
            }

            bool isFirst = true;
            foreach (KeyValuePair<string, string> entry in lang_dict)
            {
                LanguageSelector.AddItem(entry.Value, entry.Key.ToString(), isFirst);
                isFirst = false;
            }
            LanguageSelector.ShowAddedItems();

            NotificationSettingsEntry.IsEnabled = DisableSystemTray.Checked;

            Dictionary<string, string> updates_dict = new()
            {
                {CoreTools.Translate("{0} minutes", 10), "600"},
                {CoreTools.Translate("{0} minutes", 30), "1800"},
                {CoreTools.Translate("1 hour"), "3600"},
                {CoreTools.Translate("{0} hours", 2), "7200"},
                {CoreTools.Translate("{0} hours", 4), "14400"},
                {CoreTools.Translate("{0} hours", 8), "28800"},
                {CoreTools.Translate("{0} hours", 12), "43200"},
                {CoreTools.Translate("1 day"), "86400"},
                {CoreTools.Translate("{0} days", 2), "172800"},
                {CoreTools.Translate("{0} days", 3), "259200"},
                {CoreTools.Translate("1 week"), "604800"}
            };

            foreach (KeyValuePair<string, string> entry in updates_dict)
            {
                UpdatesCheckIntervalSelector.AddItem(entry.Key, entry.Value.ToString(), false);
            }
            UpdatesCheckIntervalSelector.ShowAddedItems();

            if (Settings.GetValue("PreferredTheme") == "")
            {
                Settings.SetValue("PreferredTheme", "auto");
            }

            ThemeSelector.AddItem(CoreTools.AutoTranslated("Light"), "light");
            ThemeSelector.AddItem(CoreTools.AutoTranslated("Dark"), "dark");
            ThemeSelector.AddItem(CoreTools.AutoTranslated("Follow system color scheme"), "auto");
            ThemeSelector.ShowAddedItems();

            // Backup Section
            BackupDirectoryLabel = (TextBlock)((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(0);
            ResetBackupDirectory = (HyperlinkButton)((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(1);
            OpenBackupDirectory = (HyperlinkButton)((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(2);

            EnablePackageBackupUI(Settings.Get("EnablePackageBackup"));

            ResetBackupDirectory.Content = CoreTools.Translate("Reset");

            OpenBackupDirectory.Content = CoreTools.Translate("Open");

            // Experimental Settings Section
            ExperimentalSettingsExpander.HideRestartRequiredBanner();

            // Package Manager banners;
            Dictionary<PackageManager, SettingsEntry> PackageManagerExpanders = [];
            Dictionary<PackageManager, List<SettingsCard>> ExtraSettingsCards = [];

            foreach (PackageManager Manager in PEInterface.Managers)
            {
                ExtraSettingsCards.Add(Manager, []);
            }

            ButtonCard Winget_ResetSources = new() { Text = CoreTools.AutoTranslated("Reset Winget sources (might help if no packages are listed)"), ButtonText = CoreTools.AutoTranslated("Reset") };
            Winget_ResetSources.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "reset_winget_sources.cmd"), CoreTools.Translate("Resetting Winget sources - WingetUI"), RunAsAdmin: true);
            };

            CheckboxCard Winget_DisableCOM = new()
            {
                Text = CoreTools.Translate("Use the WinGet PowerShell Module instead of the WinGet COM API"),
                SettingName = "ForceUsePowerShellModules",
            };
            Winget_DisableCOM.StateChanged += (s, e) => PackageManagerExpanders[PEInterface.WinGet].ShowRestartRequiredBanner();
            Winget_DisableCOM.IsEnabled = !Settings.Get("ForceLegacyBundledWinGet");

            CheckboxCard Winget_UseBundled = new()
            {
                Text = $"{CoreTools.Translate("Use bundled WinGet instead of system WinGet")} ({CoreTools.Translate("This may help if WinGet packages are not shown")})",
                SettingName = "ForceLegacyBundledWinGet"
            };
            Winget_UseBundled.StateChanged += (s, e) =>
            {
                PackageManagerExpanders[PEInterface.WinGet].ShowRestartRequiredBanner();
                Winget_DisableCOM.IsEnabled = !Settings.Get("ForceLegacyBundledWinGet");
            };

            ExtraSettingsCards[PEInterface.WinGet].Add(Winget_UseBundled);
            ExtraSettingsCards[PEInterface.WinGet].Add(Winget_DisableCOM);
            ExtraSettingsCards[PEInterface.WinGet].Add(Winget_ResetSources);

            ButtonCard Scoop_Install = new() { Text = CoreTools.AutoTranslated("Install Scoop"), ButtonText = CoreTools.AutoTranslated("Install") };
            Scoop_Install.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "install_scoop.cmd"), CoreTools.Translate("Scoop Installer - WingetUI"));
                PackageManagerExpanders[PEInterface.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_Uninstall = new() { Text = CoreTools.AutoTranslated("Uninstall Scoop (and its packages)"), ButtonText = CoreTools.AutoTranslated("Uninstall") };
            Scoop_Uninstall.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "uninstall_scoop.cmd"), CoreTools.Translate("Scoop Uninstaller - WingetUI"));
                PackageManagerExpanders[PEInterface.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_ResetAppCache = new() { Text = CoreTools.AutoTranslated("Run cleanup and clear cache"), ButtonText = CoreTools.AutoTranslated("Run") };
            Scoop_ResetAppCache.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"), CoreTools.Translate("Clearing Scoop cache - WingetUI"), RunAsAdmin: true);
            };

            ExtraSettingsCards[PEInterface.Scoop].Add(Scoop_Install);
            ExtraSettingsCards[PEInterface.Scoop].Add(Scoop_Uninstall);
            ExtraSettingsCards[PEInterface.Scoop].Add(Scoop_ResetAppCache);

            CheckboxCard Chocolatey_SystemChoco = new() { Text = CoreTools.AutoTranslated("Use system Chocolatey"), SettingName = "UseSystemChocolatey" };
            Chocolatey_SystemChoco.StateChanged += (s, e) =>
            {
                PackageManagerExpanders[PEInterface.Chocolatey].ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[PEInterface.Chocolatey].Add(Chocolatey_SystemChoco);

            foreach (PackageManager Manager in PEInterface.Managers)
            {

                SettingsEntry ManagerExpander = new()
                {
                    Text = Manager.DisplayName,
                    Description = Manager.Properties.Description.Replace("<br>", "\n").Replace("<b>", "").Replace("</b>", "")
                };
                PackageManagerExpanders.Add(Manager, ManagerExpander);

                InfoBar ManagerStatus = new();

                TextBlock LongVersion = new();
                HyperlinkButton ShowVersionButton = new()
                {
                    Content = CoreTools.Translate("Expand version"),
                    Visibility = Visibility.Collapsed
                };
                ManagerStatus.ActionButton = ShowVersionButton;
                ShowVersionButton.Click += (s, e) => { SetManagerStatus(Manager, true); };

                LongVersion.TextWrapping = TextWrapping.Wrap;
                LongVersion.Text = Manager.Status.Version + "\n";
                LongVersion.FontFamily = new FontFamily("Consolas");
                LongVersion.Visibility = Visibility.Collapsed;
                ManagerStatus.Content = LongVersion;

                void SetManagerStatus(PackageManager manager, bool ShowVersion = false)
                {
                    ShowVersionButton.Visibility = Visibility.Collapsed;
                    LongVersion.Visibility = Visibility.Collapsed;
                    if (manager.IsEnabled() && manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Success;
                        ManagerStatus.Title = CoreTools.Translate("{pm} is enabled and ready to go", new Dictionary<string, object?> { { "pm", manager.Name } });
                        if (!manager.Status.Version.Contains('\n'))
                        {
                            ManagerStatus.Message = CoreTools.Translate("{pm} version:", new Dictionary<string, object?> { { "pm", manager.Name } }) + " " + manager.Status.Version;
                        }
                        else if (ShowVersion)
                        {
                            ManagerStatus.Message = CoreTools.Translate("{pm} version:", new Dictionary<string, object?> { { "pm", manager.Name } });
                            LongVersion.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ManagerStatus.Message = "";
                            ShowVersionButton.Visibility = Visibility.Visible;
                        }

                    }
                    else if (manager.IsEnabled() && !manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Error;
                        ManagerStatus.Title = CoreTools.Translate("{pm} was not found!", new Dictionary<string, object?> { { "pm", manager.Name } });
                        ManagerStatus.Message = CoreTools.Translate("You may need to install {pm} in order to use it with WingetUI.", new Dictionary<string, object?> { { "pm", manager.Name } });
                    }
                    else if (!manager.IsEnabled())
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Informational;
                        ManagerStatus.Title = CoreTools.Translate("{pm} is disabled", new Dictionary<string, object?> { { "pm", manager.Name } });
                        ManagerStatus.Message = CoreTools.Translate("Enable it to install packages from {pm}.", new Dictionary<string, object?> { { "pm", manager.Name } });
                    }
                }

                ManagerStatus.IsClosable = false;
                ManagerStatus.IsOpen = true;
                ManagerStatus.CornerRadius = new CornerRadius(0);
                ManagerStatus.BorderThickness = new Thickness(0, 1, 0, 0);
                ManagerExpander.ItemsFooter = ManagerStatus;

                ManagerExpander.HeaderIcon = new LocalIcon(Manager.Properties.IconId);

                ToggleSwitch ManagerSwitch = new()
                {
                    IsOn = Manager.IsEnabled()
                };
                ManagerSwitch.Toggled += (s, e) =>
                {
                    Settings.Set("Disable" + Manager.Name, !ManagerSwitch.IsOn);
                    SetManagerStatus(Manager);
                    EnableOrDisableEntries();
                };

                ManagerExpander.Content = ManagerSwitch;

                void EnableOrDisableEntries()
                {
                    if (ExtraSettingsCards.TryGetValue(Manager, out var settingsCard))
                    {
                        foreach (SettingsCard card in settingsCard)
                        {
                            if (ManagerSwitch.IsOn)
                            {
                                card.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                card.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }

                int index = 0;
                SettingsCard ManagerPath = new()
                {
                    Description = Manager.Status.ExecutablePath + " " + Manager.Properties.ExecutableCallArgs,
                    IsClickEnabled = true,
                    ActionIcon = new SymbolIcon(Symbol.Copy)
                };
                ManagerPath.Click += (s, e) =>
                {
                    WindowsClipboard.SetText(ManagerPath.Description.ToString() ?? "");
                };
                ExtraSettingsCards[Manager].Insert(index++, ManagerPath);

                CheckboxCard AdminCard = new()
                {
                    Text = CoreTools.AutoTranslated("Always run {pm} operations with administrator rights"),
                    SettingName = "AlwaysElevate" + Manager.Name,
                };
                AdminCard._checkbox.Content = (AdminCard._checkbox.Content.ToString() ?? "").Replace("{pm}", Manager.DisplayName);
                ExtraSettingsCards[Manager].Insert(index++, AdminCard);

                CheckboxCard ParallelCard = new()
                {
                    Text = CoreTools.AutoTranslated("Allow {pm} operations to be performed in parallel"),
                    SettingName = "AllowParallelInstallsForManager" + Manager.Name,
                };
                ParallelCard._checkbox.Content = (ParallelCard._checkbox.Content.ToString() ?? "").Replace("{pm}", Manager.DisplayName);
                ExtraSettingsCards[Manager].Insert(index++, ParallelCard);

                if (Manager.Capabilities.SupportsCustomSources)
                {
                    SettingsCard SourceManagerCard = new();
                    SourceManagerCard.Resources["SettingsCardLeftIndention"] = 10;
                    SourceManager SourceManager = new(Manager);
                    SourceManagerCard.Description = SourceManager;
                    ExtraSettingsCards[Manager].Insert(index++, SourceManagerCard);
                }

                if (ExtraSettingsCards.TryGetValue(Manager, out var extraSettingsCard))
                {
                    foreach (SettingsCard card in extraSettingsCard)
                    {
                        ManagerExpander.Items.Add(card);
                    }
                }

                SetManagerStatus(Manager);
                EnableOrDisableEntries();

                MainLayout.Children.Add(ManagerExpander);
            }
        }

        public MainWindow GetWindow()
        {
            return MainApp.Instance.MainWindow;
        }
        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(GetWindow());
        }

        private void OpenWelcomeWizard(object sender, EventArgs e)
        {
        }

        private void ImportSettings(object sender, EventArgs e)
        {
            ExternalLibraries.Pickers.FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string file = picker.Show(["*.json"]);

            if (file != string.Empty)
            {
                ResetWingetUI(sender, e);
                Dictionary<string, string> settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
                foreach (KeyValuePair<string, string> entry in settings)
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, entry.Key), entry.Value);
                }

                GeneralSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private async void ExportSettings(object sender, EventArgs e)
        {
            try
            {
                ExternalLibraries.Pickers.FileSavePicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                string file = picker.Show(["*.json"], CoreTools.Translate("WingetUI Settings") + ".json");

                if (file != string.Empty)
                {
                    MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Please wait..."));

                    string[] IgnoredSettings = ["OperationHistory", "CurrentSessionToken", "OldWindowGeometry"];

                    Dictionary<string, string> settings = [];
                    foreach (string path in Directory.EnumerateFiles(CoreData.UniGetUIDataDirectory))
                    {
                        if (IgnoredSettings.Contains(Path.GetFileName(path)))
                        {
                            continue;
                        }

                        settings.Add(Path.GetFileName(path), await File.ReadAllTextAsync(path));
                    }

                    await File.WriteAllTextAsync(file, JsonConvert.SerializeObject(settings));

                    MainApp.Instance.MainWindow.HideLoadingDialog();
                }
            }
            catch (Exception ex)
            {
                MainApp.Instance.MainWindow.HideLoadingDialog();
                Logger.Error("An error occurred when exporting settings");
                Logger.Error(ex);
            }

        }

        private void ResetWingetUI(object sender, EventArgs e)
        {
            try
            {
                foreach (string path in Directory.EnumerateFiles(CoreData.UniGetUIDataDirectory))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred when resetting UniGetUI");
                Logger.Error(ex);
            }
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void LanguageSelector_ValueChanged(object sender, EventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void UpdatesCheckIntervalSelector_ValueChanged(object sender, EventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ThemeSelector_ValueChanged(object sender, EventArgs e)
        {
            ((MainApp)Application.Current).MainWindow.ApplyTheme();
        }

        private void ResetBackupPath_Click(object sender, RoutedEventArgs e)
        {
            BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
            Settings.Set("ChangeBackupOutputDirectory", false);
            ResetBackupDirectory.IsEnabled = false;
        }

        private void ChangeBackupDirectory_Click(object sender, EventArgs e)
        {

            ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != string.Empty)
            {
                Settings.SetValue("ChangeBackupOutputDirectory", folder);
                BackupDirectoryLabel.Text = folder;
                ResetBackupDirectory.IsEnabled = true;
            }

        }

        private void OpenBackupPath_Click(object sender, RoutedEventArgs e)
        {
            string directory = Settings.GetValue("ChangeBackupOutputDirectory");
            if (directory == "")
            {
                directory = CoreData.UniGetUI_DefaultBackupDirectory;
            }

            directory = directory.Replace("/", "\\");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Process.Start("explorer.exe", directory);

        }

        private void DoCacheAdminRights_StateChanged(object sender, EventArgs e)
        {
            _ = CoreTools.ResetUACForCurrentProcess();
        }

        private void UseSystemGSudo_StateChanged(object sender, EventArgs e)
        {
            AdminSettingsExpander.ShowRestartRequiredBanner();
        }

        private void DisableWidgetsApi_StateChanged(object sender, EventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void UseSystemWinget_StateChanged(object sender, EventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void DisableDownloadingNewTranslations_StateChanged(object sender, EventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void ForceArmWinget_StateChanged(object sender, EventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void TextboxCard_ValueChanged(object sender, EventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void ResetIconCache_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.Delete(CoreData.UniGetUICacheDirectory_Icons, true);
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while deleting icon cache");
                Logger.Error(ex);
            }
            ExperimentalSettingsExpander.ShowRestartRequiredBanner();
        }

        private async void DoBackup_Click(object sender, EventArgs e)
        {
            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Performing backup, please wait..."));
            await MainApp.Instance.MainWindow.NavigationPage.InstalledPage.BackupPackages();
            MainApp.Instance.MainWindow.HideLoadingDialog();
        }

        private void EditAutostartSettings_Click(object sender, EventArgs e)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ms-settings:startupapps",
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
        }

        private void DisableSystemTray_StateChanged(object sender, EventArgs e)
        {
            MainApp.Instance.MainWindow.UpdateSystemTrayStatus();
            if (NotificationSettingsEntry is not null)
            {
                NotificationSettingsEntry.IsEnabled = DisableSystemTray.Checked;
            }
        }

        private void EnablePackageBackupCheckBox_StateChanged(object sender, EventArgs e)
        {
            EnablePackageBackupUI(EnablePackageBackupCheckBox.Checked);
        }

        public void EnablePackageBackupUI(bool enabled)
        {
            if (BackupNowButton == null)
            {
                return; // This could happen when this event is triggered but the SettingsPage
            }
            // hasn't finished initializing yet.
            EnableBackupTimestampingCheckBox.IsEnabled = enabled;
            ChangeBackupFileNameTextBox.IsEnabled = enabled;
            ChangeBackupDirectory.IsEnabled = enabled;
            BackupNowButton.IsEnabled = enabled;

            if (enabled)
            {
                if (!Settings.Get("ChangeBackupOutputDirectory"))
                {
                    BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
                    ResetBackupDirectory.IsEnabled = false;
                }
                else
                {
                    BackupDirectoryLabel.Text = Settings.GetValue("ChangeBackupOutputDirectory");
                    ResetBackupDirectory.IsEnabled = true;
                }
            }
        }

        private void UseUserGSudoToggle_StateChanged(object sender, EventArgs e)
        {
            ExperimentalSettingsExpander.ShowRestartRequiredBanner();
        }
    }
}
