using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Language;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Serializable;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstallOptionsPage : Page
    {
        public SerializableInstallationOptions_v1 Options;
        public IPackage Package;
        public event EventHandler? Close;

        public InstallOptionsPage(IPackage package, SerializableInstallationOptions_v1 options) : this(package, OperationType.None, options) { }
        public InstallOptionsPage(IPackage package, OperationType Operation, SerializableInstallationOptions_v1 options)
        {
            Package = package;
            InitializeComponent();

            Options = options;

            AdminCheckBox.IsChecked = Options.RunAsAdministrator;
            AdminCheckBox.IsEnabled = Package.Manager.Capabilities.CanRunAsAdmin;

            InteractiveCheckBox.IsChecked = Options.InteractiveInstallation;
            InteractiveCheckBox.IsEnabled = Package.Manager.Capabilities.CanRunInteractively;

            HashCheckbox.IsChecked = Options.SkipHashCheck;
            HashCheckbox.IsEnabled = Operation != OperationType.Uninstall && Package.Manager.Capabilities.CanSkipIntegrityChecks;

            ArchitectureComboBox.IsEnabled = Operation != OperationType.Uninstall && Package.Manager.Capabilities.SupportsCustomArchitectures;
            ArchitectureComboBox.Items.Add(CoreTools.Translate("Default"));
            ArchitectureComboBox.SelectedIndex = 0;

            if (Package.Manager.Capabilities.SupportsCustomArchitectures)
            {
                foreach (Architecture arch in Package.Manager.Capabilities.SupportedCustomArchitectures)
                {
                    ArchitectureComboBox.Items.Add(CommonTranslations.ArchNames[arch]);
                    if (Options.Architecture == CommonTranslations.ArchNames[arch])
                    {
                        ArchitectureComboBox.SelectedValue = CommonTranslations.ArchNames[arch];
                    }
                }
            }

            VersionComboBox.IsEnabled = (Operation == OperationType.Install || Operation == OperationType.None) && (Package.Manager.Capabilities.SupportsCustomVersions || Package.Manager.Capabilities.SupportsPreRelease);
            VersionComboBox.SelectionChanged += (s, e) =>
            {
                IgnoreUpdatesCheckbox.IsChecked =
                    !(new []
                    {
                        CoreTools.Translate("Latest"), 
                        CoreTools.Translate("PreRelease"), 
                        ""
                    }.Contains(VersionComboBox.SelectedValue.ToString()));
            };
            
            VersionComboBox.Items.Add(CoreTools.Translate("Latest"));
            VersionComboBox.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsPreRelease)
            {
                VersionComboBox.Items.Add(CoreTools.Translate("PreRelease"));
                if (Options.PreRelease)
                {
                    VersionComboBox.SelectedValue = CoreTools.Translate("PreRelease");
                }
            }

            if (Package.Manager.Capabilities.SupportsCustomVersions)
            {
                _ = LoadVersions();
            }
            else
            {
                VersionProgress.Visibility = Visibility.Collapsed;
            }

            ScopeCombo.IsEnabled = Package.Manager.Capabilities.SupportsCustomScopes;
            ScopeCombo.Items.Add(CoreTools.Translate("Default"));
            ScopeCombo.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsCustomScopes)
            {
                ScopeCombo.Items.Add(CoreTools.Translate(CommonTranslations.ScopeNames[PackageScope.Local]));
                if (Options.InstallationScope == CommonTranslations.ScopeNames[PackageScope.Local])
                {
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Local];
                }

                ScopeCombo.Items.Add(CoreTools.Translate(CommonTranslations.ScopeNames[PackageScope.Global]));
                if (Options.InstallationScope == CommonTranslations.ScopeNames[PackageScope.Global])
                {
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Global];
                }
            }

            ResetDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            SelectDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            CustomInstallLocation.Text = Options.CustomInstallLocation;

            if (Options.CustomParameters != null)
            {
                CustomParameters.Text = string.Join(' ', Options.CustomParameters);
            }

            LoadIgnoredUpdates();
        }

        private async void LoadIgnoredUpdates()
        {
            IgnoreUpdatesCheckbox.IsChecked = await Package.GetIgnoredUpdatesVersionAsync() == "*";
        }

        private async Task LoadVersions()
        {
            IgnoreUpdatesCheckbox.IsChecked = await Package.HasUpdatesIgnoredAsync();

            string[] versions = await Package.Manager.GetPackageVersions(Package);

            foreach (string ver in versions)
            {
                VersionComboBox.Items.Add(ver);
                if (Options.Version == ver)
                {
                    VersionComboBox.SelectedValue = ver;
                }
            }

            VersionProgress.Visibility = Visibility.Collapsed;
        }

        public async Task<SerializableInstallationOptions_v1> GetUpdatedOptions()
        {
            Options.RunAsAdministrator = AdminCheckBox?.IsChecked ?? false;
            Options.InteractiveInstallation = InteractiveCheckBox?.IsChecked ?? false;
            Options.SkipHashCheck = HashCheckbox?.IsChecked ?? false;

            if (CommonTranslations.InvertedArchNames.ContainsKey(ArchitectureComboBox.SelectedValue.ToString() ?? ""))
            {
                Options.Architecture = ArchitectureComboBox.SelectedValue.ToString() ?? "";
            }
            else
            {
                Options.Architecture = "";
            }

            if (CommonTranslations.InvertedScopeNames.ContainsKey(ScopeCombo.SelectedValue.ToString() ?? ""))
            {
                Options.InstallationScope = ScopeCombo.SelectedValue.ToString() ?? "";
            }
            else
            {
                Options.InstallationScope = "";
            }

            Options.CustomInstallLocation = CustomInstallLocation.Text;
            Options.CustomParameters = CustomParameters.Text.Split(' ').ToList();
            Options.PreRelease = VersionComboBox.SelectedValue.ToString() == CoreTools.Translate("PreRelease");

            if (VersionComboBox.SelectedValue.ToString() != CoreTools.Translate("PreRelease") && VersionComboBox.SelectedValue.ToString() != CoreTools.Translate("Latest"))
            {
                Options.Version = VersionComboBox.SelectedValue.ToString() ?? "";
            }
            else
            {
                Options.Version = "";
            }

            if (IgnoreUpdatesCheckbox?.IsChecked ?? false)
            {
                await Package.AddToIgnoredUpdatesAsync(version: "*");
            }
            else
            {
                if (await Package.GetIgnoredUpdatesVersionAsync() == "*")
                {
                    await Package.RemoveFromIgnoredUpdatesAsync();
                }
            }
            return Options;
        }

        private void SelectDir_Click(object sender, RoutedEventArgs e)
        {
            ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != string.Empty)
            {
                CustomInstallLocation.Text = folder;
            }
        }

        private void ResetDir_Click(object sender, RoutedEventArgs e)
        {
            CustomInstallLocation.Text = "";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }
    }
}
