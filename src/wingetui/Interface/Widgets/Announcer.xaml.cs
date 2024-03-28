using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using UniGetUI.Core;
using System;
using Windows.ApplicationModel.Email;
using Windows.UI.Text;
using Windows.Web.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class Announcer : UserControl
    {
        AppTools binder = AppTools.Instance;
        public Uri Url
        {
            get => (Uri)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        DependencyProperty UrlProperty;


        private static HttpClient NetClient = new();


        public Announcer()
        {
            UrlProperty = DependencyProperty.Register(
            nameof(UrlProperty),
            typeof(Uri),
            typeof(CheckboxCard),
            new PropertyMetadata(default(Uri), new PropertyChangedCallback((d, e) => { LoadAnnouncements(); })));

            InitializeComponent();
            DefaultStyleKey = typeof(Announcer);
            BringIntoViewRequested += (s, e) => { LoadAnnouncements(); };

            int i = 0;
            PointerPressed += (s, e) => { if(i++ %3 != 0) LoadAnnouncements(); };

            SetText(binder.Translate("Fetching latest announcements, please wait..."));
            _textblock.TextWrapping = TextWrapping.Wrap;
        }

        public async void LoadAnnouncements(bool retry = false)
        {
            try
            {
                Uri announcement_url = Url;
                if (retry)
                    announcement_url = new Uri(Url.ToString().Replace("https://", "http://"));

                HttpResponseMessage response = await NetClient.GetAsync(announcement_url);
                if (response.IsSuccessStatusCode)
                {
                    string title = response.Content.ToString().Split("////")[0].Trim().Trim('\n').Trim();
                    string body = response.Content.ToString().Split("////")[1].Trim().Trim('\n').Trim();
                    string linkId = response.Content.ToString().Split("////")[2].Trim().Trim('\n').Trim();
                    string linkName = response.Content.ToString().Split("////")[3].Trim().Trim('\n').Trim();
                    Uri imageUrl = new(response.Content.ToString().Split("////")[4].Trim().Trim('\n').Trim());
                    SetText(title, body, linkId, linkName);
                    SetImage(imageUrl);
                }
                else
                {
                    SetText(binder.Translate("Could not load announcements - HTTP status code is $CODE").Replace("$CODE", response.StatusCode.ToString()));
                    SetImage(new Uri("ms-appx:///Assets/Images/warn.png"));
                    if (!retry)
                        LoadAnnouncements(true);
                }
            }
            catch (Exception ex)
            {
                AppTools.Log("Could not load announcements: " + ex.ToString());
                SetText(binder.Translate("Could not load announcements - ") + ex.ToString());
                SetImage(new Uri("ms-appx:///Assets/Images/warn.png"));
            }
        }

        public void SetText_Safe(string title, string body, string linkId, string linkName)
        {
            ((MainApp)Application.Current).MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                SetText(title, body, linkId, linkName);
            });
        }

        public void SetText(string title, string body, string linkId, string linkName)
        {
            Paragraph paragraph = new();
            paragraph.Inlines.Add(new Run() { Text = title, FontSize = 24, FontWeight = new FontWeight(700), FontFamily = new FontFamily("Segoe UI Variable Display") });
            _textblock.Blocks.Clear();
            _textblock.Blocks.Add(paragraph);

            paragraph = new();
            foreach (string line in body.Split("\n"))
            {
                paragraph.Inlines.Add(new Run() { Text = line + " " });
                paragraph.Inlines.Add(new LineBreak());
            }
            Hyperlink link = new();
            link.Inlines.Add(new Run() { Text = linkName });
            link.NavigateUri = new Uri("https://marticliment.com/redirect?" + linkId);
            paragraph.Inlines[^1] = link;
            paragraph.Inlines.Add(new LineBreak());

            _textblock.Blocks.Add(paragraph);
        }

        public void SetText(string body)
        {
            Paragraph paragraph = new();
            foreach (string line in body.Split("\n"))
            {
                paragraph.Inlines.Add(new Run() { Text = line });
                paragraph.Inlines.Add(new LineBreak());
            }
            _textblock.Blocks.Clear();
            _textblock.Blocks.Add(paragraph);
        }

        public void SetImage(Uri url)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.UriSource = url;
            _image.Source = bitmapImage;

        }
    }
}
