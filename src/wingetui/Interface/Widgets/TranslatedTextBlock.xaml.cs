using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class TranslatedTextBlock : UserControl
    {
        static AppTools Tools = AppTools.Instance;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public string Suffix
        {
            get => (string)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);

        }
        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public TextWrapping WrappingMode
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        DependencyProperty TextProperty;
        DependencyProperty PrefixProperty;
        DependencyProperty SuffixProperty;
        DependencyProperty TextWrappingProperty;

        public TranslatedTextBlock()
        {
            TextWrappingProperty = DependencyProperty.Register(
                nameof(WrappingMode),
                typeof(TextWrapping),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.TextWrapping = WrappingMode; })));

            InitializeComponent();

            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.Text = Prefix + Tools.Translate((string)e.NewValue) + Suffix; })));

            PrefixProperty = DependencyProperty.Register(
                nameof(Prefix),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.Text = (string)e.NewValue + Tools.Translate(Text) + Suffix; })));

            SuffixProperty = DependencyProperty.Register(
                nameof(Suffix),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.Text = Prefix + Tools.Translate(Text) + (string)e.NewValue; })));

        }
    }
}
