using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using System;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReleaseNotes : Page
    {
        public ReleaseNotes()
        {
            InitializeComponent();
            _ = InitializeWebView();

            WebView.NavigationStarting += (s, e) => { ProgressBar.Visibility = Visibility.Visible; };
            WebView.NavigationCompleted += (s, e) => { ProgressBar.Visibility = Visibility.Collapsed; };
        }

        private async Task InitializeWebView()
        {
            await WebView.EnsureCoreWebView2Async();
            WebView.Source = new Uri("https://github.com/marticliment/WingetUI/releases/tag/" + CoreData.VersionName);
        }

        public void Dispose()
        {
            WebView.Close();
        }


    }
}
