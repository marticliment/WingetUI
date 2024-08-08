using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HelpDialog : Page
    {
        private bool Initialized;
        public HelpDialog()
        {
            InitializeComponent();
            _ = InitializeWebView();

            WebView.NavigationStarting += (s, e) =>
            {
                ProgressBar.Visibility = Visibility.Visible;
                if (e.Uri.ToString().Contains("marticliment.com") && !e.Uri.ToString().Contains("isWingetUIIframe"))
                {
                    e.Cancel = true;
                    if (e.Uri.ToString().Contains('?'))
                    {
                        WebView.Source = new Uri(e.Uri.ToString() + "&isWingetUIIframe");
                    }
                    else
                    {
                        WebView.Source = new Uri(e.Uri.ToString() + "?isWingetUIIframe");
                    }
                }
            };
            WebView.NavigationCompleted += (s, e) => { ProgressBar.Visibility = Visibility.Collapsed; };
        }

        private async Task InitializeWebView()
        {
            await WebView.EnsureCoreWebView2Async();
            Initialized = true;
            WebView.Source = new Uri("https://marticliment.com/unigetui/help");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Initialized && WebView.CanGoBack)
            {
                WebView.GoBack();
            }
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {

            if (Initialized && WebView.CanGoForward)
            {
                WebView.GoForward();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {

            if (Initialized)
            {
                WebView.Source = new Uri("https://marticliment.com/unigetui/help");
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (Initialized)
            {
                WebView.Reload();
            }
        }

        private void BrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (Initialized)
            {
                Process.Start(new ProcessStartInfo { FileName = WebView.Source.ToString().Replace("?isWingetUIIframe", "").Replace("&isWingetUIIframe", ""), UseShellExecute = true });
            }
        }
    }
}
