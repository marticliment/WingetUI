using System.Text.Json.Nodes;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class IgnoredUpdatesManager : Page
    {
        public event EventHandler? Close;

        public IgnoredUpdatesManager()
        {
            InitializeComponent();
            IgnoredUpdatesList.DoubleTapped += IgnoredUpdatesList_DoubleTapped;
        }

        public async Task UpdateData()
        {
            Dictionary<string, PackageManager> ManagerNameReference = [];

            foreach (PackageManager Manager in PEInterface.Managers)
            {
                ManagerNameReference.Add(Manager.Name.ToLower(), Manager);
            }

            if (JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) is not JsonObject IgnoredUpdatesJson)
            {
                Logger.Warn("Ignored updates JSON database was null after deserialization");
                return;
            }

            IgnoredUpdatesList.Items.Clear();

            foreach (KeyValuePair<string, JsonNode?> keypair in IgnoredUpdatesJson)
            {
                PackageManager manager = PEInterface.WinGet; // Manager by default
                if (ManagerNameReference.ContainsKey(keypair.Key.Split("\\")[0]))
                {
                    manager = ManagerNameReference[keypair.Key.Split("\\")[0]];
                }

                IgnoredUpdatesList.Items.Add(new IgnoredPackage(keypair.Key.Split("\\")[^1], keypair.Value?.ToString() ?? "", manager, IgnoredUpdatesList));
            }

        }

        private async void IgnoredUpdatesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (IgnoredUpdatesList.SelectedItem is IgnoredPackage package)
            {
                await package.RemoveFromIgnoredUpdates();
            }
        }

        public async void ManageIgnoredUpdates_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            foreach (IgnoredPackage package in IgnoredUpdatesList.Items.ToArray())
            {
                await package.RemoveFromIgnoredUpdates();
            }
        }

        private void CloseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }
    }

    public class IgnoredPackage
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public PackageManager Manager { get; }
        private ListView List { get; }
        public IgnoredPackage(string id, string version, PackageManager manager, ListView list)
        {
            Id = id;
            Name = CoreTools.FormatAsName(id);
            if (version == "*")
            {
                Version = CoreTools.Translate("All versions");
            }
            else
            {
                Version = version;
            }

            Manager = manager;
            List = list;
        }

        public async Task RemoveFromIgnoredUpdates()
        {
            string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            if (JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) is JsonObject IgnoredUpdatesJson && IgnoredUpdatesJson.ContainsKey(IgnoredId))
            {
                IgnoredUpdatesJson.Remove(IgnoredId);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
            }

            foreach (Package package in PEInterface.InstalledPackagesLoader.Packages)
            {
                if (package.Id == Id && Manager == package.Manager)
                {
                    package.SetTag(PackageTag.Default);
                    break;
                }
            }

            List.Items.Remove(this);
        }
    }
}
