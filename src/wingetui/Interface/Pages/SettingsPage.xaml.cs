using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using ModernWindow.Clipboard;
using ModernWindow.Core.Data;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.Structures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsInterface : Page
    {
        private AppTools Tools = AppTools.Instance;
        HyperlinkButton ResetBackupDirectory;
        HyperlinkButton OpenBackupDirectory;
        TextBlock BackupDirectoryLabel;


        public SettingsInterface()
        {
            InitializeComponent();

            // General Settings Section
            Dictionary<string, string> lang_dict = LanguageData.LanguageList;

            foreach(var key in lang_dict.Keys)
            {
                if(LanguageData.TranslatedPercentages.ContainsKey(key) && key != "en")
                    lang_dict[key] = lang_dict[key] + " (" + LanguageData.TranslatedPercentages[key] + ")";
            }

            bool isFirst = true;
            foreach (KeyValuePair<string, string> entry in lang_dict)
            {
                LanguageSelector.AddItem(entry.Value, entry.Key.ToString(), isFirst);
                isFirst = false;
            }
            LanguageSelector.ShowAddedItems();



            Dictionary<string, string> updates_dict = new()
            {
                {Tools.Translate("{0} minutes", 10), "600"},
                {Tools.Translate("{0} minutes", 30), "1800"},
                {Tools.Translate("1 hour"), "3600"},
                {Tools.Translate("{0} hours", 2), "7200"},
                {Tools.Translate("{0} hours", 4), "14400"},
                {Tools.Translate("{0} hours", 8), "28800"},
                {Tools.Translate("{0} hours", 12), "43200"},
                {Tools.Translate("1 day"), "86400"},
                {Tools.Translate("{0} days", 2), "172800"},
                {Tools.Translate("{0} days", 3), "259200"},
                {Tools.Translate("1 week"), "604800"}
            };

            foreach (KeyValuePair<string, string> entry in updates_dict)
            {
                UpdatesCheckIntervalSelector.AddItem(entry.Key, entry.Value.ToString(), false);
            }
            UpdatesCheckIntervalSelector.ShowAddedItems();

            if (Tools.GetSettingsValue("PreferredTheme") == "")
                Tools.SetSettingsValue("PreferredTheme", "auto");

            ThemeSelector.AddItem(Tools.AutoTranslated("Light"), "light");
            ThemeSelector.AddItem(Tools.AutoTranslated("Dark"), "dark");
            ThemeSelector.AddItem(Tools.AutoTranslated("Follow system color scheme"), "auto");
            ThemeSelector.ShowAddedItems();

            // Backup Section
            BackupDirectoryLabel = (TextBlock)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(0));
            ResetBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(1));
            OpenBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(2));
            if (!Tools.GetSettings("ChangeBackupOutputDirectory"))
            {
                BackupDirectoryLabel.Text = CoreData.WingetUI_DefaultBackupDirectory;
                ResetBackupDirectory.IsEnabled = false;
            }
            else
            {
                BackupDirectoryLabel.Text = Tools.GetSettingsValue("ChangeBackupOutputDirectory");
                ResetBackupDirectory.IsEnabled = true;
            }

            ResetBackupDirectory.Content = Tools.Translate("Reset");

            OpenBackupDirectory.Content = Tools.Translate("Open");

            // Admin Settings Section
            int index = 2;
            foreach (PackageManager manager in Tools.App.PackageManagerList)
            {
            }

            // Experimental Settings Section
            ExperimentalSettingsExpander.HideRestartRequiredBanner();


            // Package Manager banners;
            Dictionary<PackageManager, SettingsEntry> PackageManagerExpanders = new();
            Dictionary<PackageManager, List<SettingsCard>> ExtraSettingsCards = new();

            foreach (PackageManager Manager in Tools.App.PackageManagerList)
            {
                ExtraSettingsCards.Add(Manager, new List<SettingsCard>());
            }


            ButtonCard Winget_ResetSources = new() { Text = Tools.AutoTranslated("Reset Winget sources (might help if no packages are listed)"), ButtonText = Tools.AutoTranslated("Reset") };
            Winget_ResetSources.Click += (s, e) =>
            {
                AppTools.LaunchBatchFile(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Utilities", "reset_winget_sources.cmd"), Tools.Translate("Resetting Winget sources - WingetUI"), RunAsAdmin: true);
            };

            ExtraSettingsCards[Tools.App.Winget].Add(Winget_ResetSources);

            ButtonCard Scoop_Install = new() { Text = Tools.AutoTranslated("Install Scoop"), ButtonText = Tools.AutoTranslated("Install") };
            Scoop_Install.Click += (s, e) =>
            {
                AppTools.LaunchBatchFile(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Utilities", "install_scoop.cmd"), Tools.Translate("Scoop Installer - WingetUI"));
                PackageManagerExpanders[Tools.App.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_Uninstall = new() { Text = Tools.AutoTranslated("Uninstall Scoop (and its packages)"), ButtonText = Tools.AutoTranslated("Uninstall") };
            Scoop_Uninstall.Click += (s, e) =>
            {
                AppTools.LaunchBatchFile(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Utilities", "uninstall_scoop.cmd"), Tools.Translate("Scoop Uninstaller - WingetUI"));
                PackageManagerExpanders[Tools.App.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_ResetAppCache = new() { Text = Tools.AutoTranslated("Run cleanup and clear cache"), ButtonText = Tools.AutoTranslated("Run") };
            Scoop_ResetAppCache.Click += (s, e) =>
            {
                AppTools.LaunchBatchFile(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"), Tools.Translate("Clearing Scoop cache - WingetUI"), RunAsAdmin: true);
            };

            ExtraSettingsCards[Tools.App.Scoop].Add(Scoop_Install);
            ExtraSettingsCards[Tools.App.Scoop].Add(Scoop_Uninstall);
            ExtraSettingsCards[Tools.App.Scoop].Add(Scoop_ResetAppCache);

            CheckboxCard Chocolatey_SystemChoco = new() { Text = Tools.AutoTranslated("Use system Chocolatey"), SettingName = "UseSystemChocolatey" };
            Chocolatey_SystemChoco.StateChanged += (s, e) =>
            {
                PackageManagerExpanders[Tools.App.Choco].ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[Tools.App.Choco].Add(Chocolatey_SystemChoco);



            foreach (PackageManager Manager in Tools.App.PackageManagerList)
            {

                SettingsEntry ManagerExpander = new()
                {
                    Text = Manager.Name,
                    Description = Manager.Properties.Description.Replace("<br>", "\n").Replace("<b>", "").Replace("</b>", "")
                };
                PackageManagerExpanders.Add(Manager, ManagerExpander);

                InfoBar ManagerStatus = new();

                TextBlock LongVersion = new();
                HyperlinkButton ShowVersionButton = new();
                ShowVersionButton.Content = Tools.Translate("Expand version");
                ShowVersionButton.Visibility = Visibility.Collapsed;
                ManagerStatus.ActionButton = ShowVersionButton;
                ShowVersionButton.Click += (s, e) => { SetManagerStatus(Manager, true); };

                LongVersion.TextWrapping = TextWrapping.Wrap;
                LongVersion.Text = Manager.Status.Version + "\n";
                LongVersion.FontFamily = new FontFamily("Consolas");
                LongVersion.Visibility = Visibility.Collapsed;
                ManagerStatus.Content = LongVersion;

                void SetManagerStatus(PackageManager Manager, bool ShowVersion = false)
                {
                    ShowVersionButton.Visibility = Visibility.Collapsed;
                    LongVersion.Visibility = Visibility.Collapsed;
                    if (Manager.IsEnabled() && Manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Success;
                        ManagerStatus.Title = Tools.Translate("{pm} is enabled and ready to go", new Dictionary<string, object>{ { "pm", Manager.Name } });
                        if (!Manager.Status.Version.Contains("\n"))
                            ManagerStatus.Message = Tools.Translate("{pm} version:", new Dictionary<string, object>{ { "pm", Manager.Name } }) + " " + Manager.Status.Version;
                        else if (ShowVersion)
                        {
                            ManagerStatus.Message = Tools.Translate("{pm} version:", new Dictionary<string, object>{ { "pm", Manager.Name } });
                            LongVersion.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ManagerStatus.Message = "";
                            ShowVersionButton.Visibility = Visibility.Visible;
                        }

                    }
                    else if (Manager.IsEnabled() && !Manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Error;
                        ManagerStatus.Title = Tools.Translate("{pm} was not found!", new Dictionary<string, object>{ { "pm", Manager.Name } });
                        ManagerStatus.Message = Tools.Translate("You may need to install {pm} in order to use it with WingetUI.", new Dictionary<string, object>{ { "pm", Manager.Name } });
                    }
                    else if (!Manager.IsEnabled())
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Informational;
                        ManagerStatus.Title = Tools.Translate("{pm} is disabled", new Dictionary<string, object>{ { "pm", Manager.Name } });
                        ManagerStatus.Message = Tools.Translate("Enable it to install packages from {pm}.", new Dictionary<string, object>{ { "pm", Manager.Name } });
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
                    Tools.SetSettings("Disable" + Manager.Name, !ManagerSwitch.IsOn);
                    SetManagerStatus(Manager);
                    EnableOrDisableEntries();
                };

                ManagerExpander.Content = ManagerSwitch;

                void EnableOrDisableEntries()
                {
                    if (ExtraSettingsCards.ContainsKey(Manager))
                        foreach (SettingsCard card in ExtraSettingsCards[Manager])
                        {
                            if (ManagerSwitch.IsOn)
                                card.Visibility = Visibility.Visible;
                            else
                                card.Visibility = Visibility.Collapsed;
                        }
                }

                
                index = 0;

                SettingsCard ManagerPath = new() { Description = Manager.Status.ExecutablePath + " " + Manager.Properties.ExecutableCallArgs, IsClickEnabled = true };
                ManagerPath.ActionIcon = new SymbolIcon(Symbol.Copy);
                ManagerPath.Click += (s, e) =>
                {
                    WindowsClipboard.SetText(ManagerPath.Description.ToString());
                };
                ExtraSettingsCards[Manager].Insert(index++, ManagerPath);

                CheckboxCard AdminCard = new()
                {
                    Text = Tools.AutoTranslated("Always run {pm} operations with administrator rights"),
                    SettingName = "AlwaysElevate" + Manager.Name,
                };
                AdminCard._checkbox.Content = AdminCard._checkbox.Content.ToString().Replace("{pm}", Manager.Name);
                ExtraSettingsCards[Manager].Insert(index++, AdminCard);

                CheckboxCard ParallelCard = new()
                {
                    Text = Tools.AutoTranslated("Allow {pm} operations to be performed in parallel"),
                    SettingName = "AllowParallelInstallsForManager" + Manager.Name,
                };
                ParallelCard._checkbox.Content = ParallelCard._checkbox.Content.ToString().Replace("{pm}", Manager.Name);
                ExtraSettingsCards[Manager].Insert(index++, ParallelCard);


                if (Manager is PackageManagerWithSources)
                {
                    SettingsCard SourceManagerCard = new();
                    SourceManagerCard.Resources["SettingsCardLeftIndention"] = 10;
                    SourceManager SourceManager = new(Manager as PackageManagerWithSources);
                    SourceManagerCard.Description = SourceManager;
                    ExtraSettingsCards[Manager].Insert(index++, SourceManagerCard);
                }

                if (ExtraSettingsCards.ContainsKey(Manager))
                    foreach (SettingsCard card in ExtraSettingsCards[Manager])
                    {
                        ManagerExpander.Items.Add(card);
                    }

                SetManagerStatus(Manager);
                EnableOrDisableEntries();

                MainLayout.Children.Add(ManagerExpander);
            }
        }

        public MainWindow GetWindow()
        {
            return Tools.App.MainWindow;
        }
        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(GetWindow());
        }

        private void OpenWelcomeWizard(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
        }

        private void ImportSettings(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            var picker = new Pickers.FileOpenPicker(Tools.App.MainWindow.GetWindowHandle());
            var file = picker.Show(new List<string> { "*.json" });

            if (file != string.Empty)
            {
                ResetWingetUI(sender, e);
                var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                foreach(var entry in settings)
                    Tools.SetSettingsValue(entry.Key, entry.Value);

                GeneralSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private async void ExportSettings(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            try
            {
                var picker = new Pickers.FileSavePicker(Tools.App.MainWindow.GetWindowHandle());
                var file = picker.Show(new List<string> { "*.json" }, Tools.Translate("WingetUI Settings") + ".json");

                if (file != String.Empty)
                {
                    Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Please wait..."));

                    var IgnoredSettings = new string[] { "OperationHistory", "CurrentSessionToken", "OldWindowGeometry" };

                    Dictionary<string, string> settings = new();
                    foreach (var path in Directory.EnumerateFiles(CoreData.WingetUIDataDirectory))
                    {
                        if (Path.GetFileName(path).Contains('.') || IgnoredSettings.Contains(Path.GetFileName(path)))
                            continue;
                        settings.Add(Path.GetFileName(path), await File.ReadAllTextAsync(path));
                    }

                    await File.WriteAllTextAsync(file, JsonConvert.SerializeObject(settings));

                    Tools.App.MainWindow.HideLoadingDialog();
                }
            }
            catch (Exception ex)
            {
                Tools.App.MainWindow.HideLoadingDialog();
                AppTools.Log(ex);
            }

        }

        private void ResetWingetUI(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(CoreData.WingetUIDataDirectory))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void LanguageSelector_ValueChanged(object sender, Interface.Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void UpdatesCheckIntervalSelector_ValueChanged(object sender, Interface.Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ThemeSelector_ValueChanged(object sender, Interface.Widgets.ComboCardEventArgs e)
        {
            ((MainApp)Application.Current).MainWindow.ApplyTheme();
        }

        private void ResetBackupPath_Click(object sender, dynamic e)
        {
            BackupDirectoryLabel.Text = CoreData.WingetUI_DefaultBackupDirectory;
            Tools.SetSettings("ChangeBackupOutputDirectory", false);
            ResetBackupDirectory.IsEnabled = false;
        }

        private void ChangeBackupDirectory_Click(object sender, dynamic e)
        {

            var openPicker = new Pickers.FolderPicker(Tools.App.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != String.Empty)
            {
                Tools.SetSettingsValue("ChangeBackupOutputDirectory", folder);
                BackupDirectoryLabel.Text = folder;
                ResetBackupDirectory.IsEnabled = true;
            }
            else
            {
                ResetBackupPath_Click(sender, e);
            }

        }

        private void OpenBackupPath_Click(object sender, RoutedEventArgs e)
        {
            string directory = Tools.GetSettingsValue("ChangeBackupOutputDirectory");
            if (directory == "")
                directory = CoreData.WingetUI_DefaultBackupDirectory;

            directory = directory.Replace("/", "\\");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            Process.Start("explorer.exe", directory);

        }

        private void DoCacheAdminRights_StateChanged(object sender, Interface.Widgets.CheckBoxEventArgs e)
        {
            if (!e.IsChecked)
            {
                AdminSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private void UseSystemGSudo_StateChanged(object sender, Interface.Widgets.CheckBoxEventArgs e)
        { AdminSettingsExpander.ShowRestartRequiredBanner(); }

        private void DisableWidgetsApi_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }


        private void UseSystemWinget_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }


        private void DisableDownloadingNewTranslations_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void ForceArmWinget_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void TextboxCard_ValueChanged(object sender, TextboxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void ResetIconCache_Click(object sender, ButtonCardEventArgs e)
        {
            try
            {
                Directory.Delete(CoreData.WingetUICacheDirectory_Icons, true);
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
            ExperimentalSettingsExpander.ShowRestartRequiredBanner();
        }

        private async void DoBackup_Click(object sender, ButtonCardEventArgs e)
        {
            Tools.App.MainWindow.ShowLoadingDialog(Tools.Translate("Performing backup, please wait..."));
            await Tools.App.MainWindow.NavigationPage.InstalledPage.BackupPackages();
            Tools.App.MainWindow.HideLoadingDialog();
        }

        private void EditAutostartSettings_Click(object sender, ButtonCardEventArgs e)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c start ms-settings:startupapps",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            p.Start();
        }
    }
}
