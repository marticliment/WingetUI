using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public class SourceItem
    {
        public SourceManager Parent;
        public ManagerSource Source;

        public SourceItem(SourceManager Parent, ManagerSource Source)
        {
            this.Parent = Parent;
            this.Source = Source;
        }
        public void Remove(object sender, RoutedEventArgs e)
        {
            RemoveSourceOperation op = new(Source);
            Parent.Tools.AddOperationToList(op);
            op.OperationSucceeded += (sender, e) => { Parent.RemoveSourceItem(this); };
        }
    }
    public sealed partial class SourceManager : UserControl
    {
        private PackageManagerWithSources Manager { get; set; }
        private ObservableCollection<SourceItem> Sources = new();
        public AppTools Tools = AppTools.Instance;

        private ListView _datagrid { get; set; }
        public SourceManager(PackageManagerWithSources Manager)
        {
            InitializeComponent();
            Header.Text = Tools.Translate("Manage {0} sources", Manager.Properties.Name);
            AddSourceButton.Content = Tools.Translate("Add source");
            AddSourceButton.Click += async (sender, e) =>
            {
                try
                {

                    ContentDialog d = new();
                    d.Title = Tools.Translate("Add source");

                    ComboBox SourcesCombo = new();
                    Dictionary<string, ManagerSource> NameSourceRef = new();
                    foreach (ManagerSource source in Manager.KnownSources)
                    {
                        SourcesCombo.Items.Add(source.Name);
                        NameSourceRef.Add(source.Name, source);
                    }

                    d.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                    StackPanel p = new();
                    p.Spacing = 8;
                    p.Children.Add(new TextBlock() { Text = Tools.Translate("Select the source you want to add:") });
                    p.Children.Add(SourcesCombo);

                    TextBox SourceNameTextBox = new() { HorizontalAlignment = HorizontalAlignment.Stretch, Width = 400 };
                    TextBox SourceUrlTextBox = new() { HorizontalAlignment = HorizontalAlignment.Stretch };

                    StackPanel p1 = new() { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
                    p1.Children.Add(new TextBlock() { Text = Tools.Translate("Source name:"), VerticalAlignment = VerticalAlignment.Center });
                    p1.Children.Add(SourceNameTextBox);


                    StackPanel p2 = new() { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
                    p2.Children.Add(new TextBlock() { Text = Tools.Translate("Source URL:"), VerticalAlignment = VerticalAlignment.Center });
                    p2.Children.Add(SourceUrlTextBox);

                    p.Children.Add(p1);
                    p.Children.Add(p2);

                    SourcesCombo.Items.Add(Tools.Translate("Other"));
                    SourcesCombo.HorizontalAlignment = HorizontalAlignment.Stretch;
                    SourcesCombo.SelectionChanged += (sender, e) =>
                    {
                        if (SourcesCombo.SelectedValue.ToString() == Tools.Translate("Other"))
                        {
                            SourceUrlTextBox.IsEnabled = SourceNameTextBox.IsEnabled = true;
                            SourceUrlTextBox.Text = SourceNameTextBox.Text = "";
                        }
                        else
                        {
                            SourceUrlTextBox.IsEnabled = SourceNameTextBox.IsEnabled = false;
                            SourceUrlTextBox.Text = NameSourceRef[SourcesCombo.SelectedValue.ToString()].Url.ToString();
                            SourceNameTextBox.Text = NameSourceRef[SourcesCombo.SelectedValue.ToString()].Name;
                        }
                    };
                    SourcesCombo.SelectedIndex = 0;

                    d.XamlRoot = XamlRoot;
                    d.Content = p;
                    d.PrimaryButtonText = Tools.Translate("Add");
                    d.SecondaryButtonText = Tools.Translate("Cancel");
                    d.DefaultButton = ContentDialogButton.Primary;

                    if (await Tools.App.MainWindow.ShowDialogAsync(d) == ContentDialogResult.Primary)
                    {
                        AddSourceOperation op;
                        if (Tools.Translate("Other") != SourcesCombo.SelectedValue.ToString())
                            op = new AddSourceOperation(NameSourceRef[SourcesCombo.SelectedValue.ToString()]);
                        else
                            op = new AddSourceOperation(new ManagerSource(this.Manager, SourceNameTextBox.Text, new Uri(SourceUrlTextBox.Text)));
                        Tools.AddOperationToList(op);
                        op.OperationSucceeded += (sender, e) => { LoadSources(); };

                    }
                }
                catch (Exception ex)
                {
                    ContentDialog d = new();
                    d.XamlRoot = XamlRoot;
                    d.Title = Tools.Translate("An error occurred");
                    d.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                    d.Content = Tools.Translate("An error occurred when adding the source: ") + ex.Message;
                    _ = Tools.App.MainWindow.ShowDialogAsync(d, HighPriority: true);
                    d.PrimaryButtonText = Tools.Translate("Close");
                    AppTools.Log(ex);
                }
            };
            this.Manager = Manager;
            _datagrid = DataList;
            DataList.ItemTemplate = (DataTemplate)Resources["ManagerSourceTemplate"];
            LoadSources();
        }

        public async void LoadSources()
        {
            if (!Manager.Status.Found)
                return;

            LoadingBar.Visibility = Visibility.Visible;
            Sources.Clear();
            foreach (ManagerSource Source in await Manager.GetSources())
            {
                Sources.Add(new SourceItem(this, Source));
            }
            if (Sources.Count > 0)
                _datagrid.SelectedIndex = 0;

            LoadingBar.Visibility = Visibility.Collapsed;
        }

        public void RemoveSourceItem(SourceItem Item)
        {
            Sources.Remove(Item);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSources();
        }
    }
}
