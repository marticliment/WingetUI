using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Core;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstallOptionsPage : Page
    {
        AppTools Tools = AppTools.Instance;
        public InstallationOptions Options;
        public Package Package;

        public InstallOptionsPage(Package package, OperationType Operation) : this(package, Operation, new InstallationOptions(package)) { }
        public InstallOptionsPage(Package package, InstallationOptions options) : this(package, OperationType.None, options) { }
        public InstallOptionsPage(Package package, OperationType Operation, InstallationOptions options)
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
            ArchitectureComboBox.Items.Add(Tools.Translate("Default"));
            ArchitectureComboBox.SelectedIndex = 0;


            if (Package.Manager.Capabilities.SupportsCustomArchitectures)
                foreach (Architecture arch in Package.Manager.Capabilities.SupportedCustomArchitectures)
                {
                    ArchitectureComboBox.Items.Add(CommonTranslations.ArchNames[arch]);
                    if (Options.Architecture == arch)
                        ArchitectureComboBox.SelectedValue = CommonTranslations.ArchNames[arch];
                }

            VersionComboBox.IsEnabled = (Operation == OperationType.Install || Operation == OperationType.None) && (Package.Manager.Capabilities.SupportsCustomVersions || Package.Manager.Capabilities.SupportsPreRelease);
            VersionComboBox.SelectionChanged += (s, e) =>
              { IgnoreUpdatesCheckbox.IsChecked = !new string[] { Tools.Translate("Latest"), Tools.Translate("PreRelease"), "" }.Contains(VersionComboBox.SelectedValue.ToString()); };
            VersionComboBox.Items.Add(Tools.Translate("Latest"));
            VersionComboBox.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsPreRelease)
            {
                VersionComboBox.Items.Add(Tools.Translate("PreRelease"));
                if (Options.PreRelease)
                    VersionComboBox.SelectedValue = Tools.Translate("PreRelease");
            }

            if (Package.Manager.Capabilities.SupportsCustomVersions)
                _ = LoadVersions();
            else
                VersionProgress.Visibility = Visibility.Collapsed;

            ScopeCombo.IsEnabled = Package.Manager.Capabilities.SupportsCustomScopes;
            ScopeCombo.Items.Add(Tools.Translate("Default"));
            ScopeCombo.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsCustomScopes)
            {
                ScopeCombo.Items.Add(Tools.Translate(CommonTranslations.ScopeNames[PackageScope.Local]));
                if (Options.InstallationScope == PackageScope.Local)
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Local];
                ScopeCombo.Items.Add(Tools.Translate(CommonTranslations.ScopeNames[PackageScope.Global]));
                if (Options.InstallationScope == PackageScope.Global)
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Global];
            }


            ResetDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            SelectDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            CustomInstallLocation.Text = Options.CustomInstallLocation;

            if (Options.CustomParameters != null)
                CustomParameters.Text = String.Join(' ', Options.CustomParameters);

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
                    VersionComboBox.SelectedValue = ver;
            }

            VersionProgress.Visibility = Visibility.Collapsed;
        }

        public async Task<InstallationOptions> GetUpdatedOptions()
        {
            Options.RunAsAdministrator = AdminCheckBox.IsChecked.Value;
            Options.InteractiveInstallation = InteractiveCheckBox.IsChecked.Value;
            Options.SkipHashCheck = HashCheckbox.IsChecked.Value;

            if (CommonTranslations.InvertedArchNames.ContainsKey(ArchitectureComboBox.SelectedValue.ToString()))
                Options.Architecture = CommonTranslations.InvertedArchNames[ArchitectureComboBox.SelectedValue.ToString()];
            else
                Options.Architecture = null;

            if (CommonTranslations.InvertedScopeNames.ContainsKey(ScopeCombo.SelectedValue.ToString()))
                Options.InstallationScope = CommonTranslations.InvertedScopeNames[ScopeCombo.SelectedValue.ToString()];
            else
                Options.InstallationScope = null;

            Options.CustomInstallLocation = CustomInstallLocation.Text;
            Options.CustomParameters = CustomParameters.Text.Split(' ').ToList();
            Options.PreRelease = VersionComboBox.SelectedValue.ToString() == Tools.Translate("PreRelease");

            if (VersionComboBox.SelectedValue.ToString() != Tools.Translate("PreRelease") && VersionComboBox.SelectedValue.ToString() != Tools.Translate("Latest"))
                Options.Version = VersionComboBox.SelectedValue.ToString();
            else
                Options.Version = "";

            if (IgnoreUpdatesCheckbox.IsChecked.Value)
                await Package.AddToIgnoredUpdatesAsync(version: "*");
            else
            {
                if (await Package.GetIgnoredUpdatesVersionAsync() == "*")
                    await Package.RemoveFromIgnoredUpdatesAsync();
            }
            return Options;
        }

        public async void SaveToDisk()
        {

            (await GetUpdatedOptions()).SaveOptionsToDisk();
        }

        private void SelectDir_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new UniGetUI.ExternalLibraries.Pickers.FolderPicker(Tools.App.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != String.Empty)
                CustomInstallLocation.Text = folder;
        }

        private void ResetDir_Click(object sender, RoutedEventArgs e)
        {
            CustomInstallLocation.Text = "";
        }
    }
}
