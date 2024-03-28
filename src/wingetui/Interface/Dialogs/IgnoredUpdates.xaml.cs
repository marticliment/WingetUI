using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class IgnoredUpdatesManager : Page
    {
        AppTools Tools = AppTools.Instance;
        public IgnoredUpdatesManager()
        {
            InitializeComponent();
            IgnoredUpdatesList.DoubleTapped += IgnoredUpdatesList_DoubleTapped;
        }

        public async Task UpdateData()
        {

            Dictionary<string, PackageManager> ManagerNameReference = new();

            foreach (PackageManager Manager in Tools.App.PackageManagerList)
            {
                ManagerNameReference.Add(Manager.Name.ToLower(), Manager);
            }

            JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

            IgnoredUpdatesList.Items.Clear();

            foreach (KeyValuePair<string, JsonNode> keypair in IgnoredUpdatesJson)
            {
                PackageManager manager = Tools.App.Winget; // Manager by default
                if (ManagerNameReference.ContainsKey(keypair.Key.Split("\\")[0]))
                    manager = ManagerNameReference[keypair.Key.Split("\\")[0]];

                IgnoredUpdatesList.Items.Add(new IgnoredPackage(keypair.Key.Split("\\")[^1], keypair.Value.ToString(), manager, IgnoredUpdatesList));
            }

        }

        private async void IgnoredUpdatesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (IgnoredUpdatesList.SelectedItem != null)
                await (IgnoredUpdatesList.SelectedItem as IgnoredPackage).RemoveFromIgnoredUpdates();
        }

        public async void ManageIgnoredUpdates_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            foreach (IgnoredPackage package in IgnoredUpdatesList.Items.ToArray())
                await package.RemoveFromIgnoredUpdates();
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
            Name = AppTools.Instance.FormatAsName(id);
            if (version == "*")
                Version = AppTools.Instance.Translate("All versions");
            else
                Version = version;
            Manager = manager;
            List = list;
        }
        public async Task RemoveFromIgnoredUpdates()
        {
            string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
            if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
            {
                IgnoredUpdatesJson.Remove(IgnoredId);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
            }

            foreach(var package in AppTools.Instance.App.MainWindow.NavigationPage.InstalledPage.Packages)
                if(package.Id == Id && Manager == package.Manager) {
                    package.SetTag(PackageTag.Default);
                    break;
                }

            List.Items.Remove(this);
        }
    }
}
