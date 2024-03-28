using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public class ButtonCardEventArgs : EventArgs
    {
        public ButtonCardEventArgs()
        {
        }
    }


    public sealed class ButtonCard : SettingsCard
    {
        private static Button _button;
        private static AppTools Tools = AppTools.Instance;

        public string ButtonText
        {
            get => (string)GetValue(ButtonProperty);
            set => SetValue(ButtonProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        DependencyProperty TextProperty;

        DependencyProperty ButtonProperty = DependencyProperty.Register(
        nameof(ButtonText),
        typeof(string),
        typeof(ButtonCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _button.Content = Tools.Translate((string)e.NewValue); })));

        public new event EventHandler<ButtonCardEventArgs> Click;

        public ButtonCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ButtonCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Header = Tools.Translate((string)e.NewValue); })));

            _button = new Button();
            _button.MinWidth = 200;
            _button.Click += (s, e) => { Click?.Invoke(this, new ButtonCardEventArgs()); };

            DefaultStyleKey = typeof(ButtonCard);
            Content = _button;
        }
    }
}
