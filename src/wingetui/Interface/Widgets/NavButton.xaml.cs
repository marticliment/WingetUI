using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Media.Devices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class NavButton : UserControl
    {

        private AppTools Tools = AppTools.Instance;
        public class NavButtonEventArgs : EventArgs
        {
            public NavButtonEventArgs()
            {
            }
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        DependencyProperty TextProperty;
        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }
        DependencyProperty GlyphProperty;

        public event EventHandler<NavButtonEventArgs> Click;

        public NavButton()
        {
            InitializeComponent();

            DefaultStyleKey = typeof(NavButton);


            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(NavButton),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    string val = Tools.Translate((string)e.NewValue);
                    int count = val.Count(x => x == ' ');
                    TextBlock.Text = val.Replace(" ", "\x0a");
                    ToggleButton.Content = val.Replace(" ", "\x0a");
                }))
            );

            GlyphProperty = DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(NavButton),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { IconBlock.Glyph = (string)e.NewValue; }))
            );

            Tools.App.MainWindow.NavButtonList.Add(this);
        }
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, new NavButtonEventArgs());
        }

        public void ForceClick()
        {
            ToggleButton.IsChecked = true;
            Click?.Invoke(this, new NavButtonEventArgs());
        }
    }
}


/*


*/