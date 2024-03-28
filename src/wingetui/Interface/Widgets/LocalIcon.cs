using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core;
using System;

namespace UniGetUI.Interface.Widgets
{
    public class LocalIcon : ImageIcon
    {
        public static AppTools Tools = AppTools.Instance;
        private bool __registered_theme_event = false;
        public DependencyProperty IconNameProperty;

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, value);
        }

        public LocalIcon()
        {
            IconNameProperty = DependencyProperty.Register(
            nameof(IconName),
            typeof(string),
            typeof(ButtonCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { 
                IconName = (string)e.NewValue; 
                __apply_icon();
                if (!__registered_theme_event)
                {
                    ActualThemeChanged += (s, e) => { __apply_icon(); };
                    __registered_theme_event = true;
                }
            })));
        }

        public LocalIcon(string iconName) : this()
        {
            IconName = iconName;
            __apply_icon();

            if(!__registered_theme_event)
            {
                ActualThemeChanged += (s, e) => { __apply_icon(); };
                __registered_theme_event = true;
            }
        }

        public void __apply_icon()
        {
            string theme = "white";
            if (ActualTheme == ElementTheme.Light)
            {
                theme = "black";
            }

            if (Source == null)
                Source = new BitmapImage();
            (Source as BitmapImage).UriSource = new Uri("ms-appx:///Assets/Images/" + IconName + "_" + theme + ".png");
        }
    }
}
