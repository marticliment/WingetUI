using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public class TextboxEventArgs : EventArgs
    {

        public TextboxEventArgs()
        {
        }
    }

    public sealed class TextboxCard : SettingsCard
    {
        private TextBox _textbox;
        private HyperlinkButton _helpbutton;
        private static AppTools Tools = AppTools.Instance;

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Uri HelpUrl
        {
            get => (Uri)GetValue(HelpUrlProperty);
            set => SetValue(HelpUrlProperty, value);
        }

        DependencyProperty PlaceholderProperty;

        DependencyProperty SettingProperty;

        DependencyProperty TextProperty;

        DependencyProperty HelpUrlProperty;

        public event EventHandler<TextboxEventArgs> ValueChanged;

        public TextboxCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Header = Tools.Translate((string)e.NewValue); })));

            PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _textbox.PlaceholderText = Tools.Translate((string)e.NewValue); })));

            SettingProperty = DependencyProperty.Register(
            nameof(SettingName),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                _textbox.Text = Tools.GetSettingsValue((string)e.NewValue);
                _textbox.TextChanged += (sender, e) => { SaveValue(); };
            })));

            HelpUrlProperty = DependencyProperty.Register(
            nameof(HelpUrl),
            typeof(Uri),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                _helpbutton.NavigateUri = (Uri)e.NewValue;
                _helpbutton.Visibility = Visibility.Visible;
                _helpbutton.Content = Tools.Translate("More info");
            })));

            _helpbutton = new HyperlinkButton();
            _helpbutton.Visibility = Visibility.Collapsed;

            _textbox = new TextBox();
            _textbox.MinWidth = 200;

            StackPanel s = new();
            s.Orientation = Orientation.Horizontal;
            s.Children.Add(_helpbutton);
            s.Children.Add(_textbox);

            DefaultStyleKey = typeof(TextboxCard);
            Content = s;
        }

        public void SaveValue()
        {
            string SanitizedText = _textbox.Text;

            if (SettingName.Contains("File"))
                foreach (char rem in "#%&{}\\/<>*?$!'\":;@`|~")
                    SanitizedText = SanitizedText.Replace(rem.ToString(), "");

            if (SanitizedText != "")
                Tools.SetSettingsValue(SettingName, SanitizedText);
            else
                Tools.SetSettings(SettingName, false);
            TextboxEventArgs args = new();
            ValueChanged?.Invoke(this, args);
        }

    }
}
