using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core;
using System;

namespace UniGetUI.Interface.Widgets
{
    public class BetterMenu : MenuFlyout
    {
        public AppTools Tools = AppTools.Instance;
        public BetterMenu() : base()
        {
            MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["BetterContextMenu"];
        }
    }

    public class BetterMenuItem : MenuFlyoutItem
    {
        public AppTools Tools = AppTools.Instance;
        DependencyProperty IconNameProperty;

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, (string)value);
        }

        new DependencyProperty TextProperty;

        new public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, (string)value);
        }

        public BetterMenuItem() : base()
        {
            Style = (Style)Application.Current.Resources["BetterMenuItem"];

            IconNameProperty = DependencyProperty.Register(
                nameof(IconName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    Icon = new LocalIcon(e.NewValue as string);
                })));

            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    (this as MenuFlyoutItem).Text = AppTools.Instance.Translate(e.NewValue as string);
                })));

        }

    }


    public class MenuForPackage : MenuFlyout
    {
        public event EventHandler<Package> AboutToShow;
        DependencyProperty PackageProperty;

        public MenuForPackage() : base()
        {
            MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["BetterContextMenu"];
            PackageProperty = DependencyProperty.Register(
                nameof(Package),
                typeof(Package),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

            Opening += (s, e) => { AboutToShow?.Invoke(this, Package); };
        }

        public Package Package
        {
            get => (Package)GetValue(PackageProperty);
            set => SetValue(PackageProperty, value);
        }
    }
    public class MenuItemForPackage : MenuFlyoutItem
    {
        public event EventHandler<Package> Invoked;

        DependencyProperty PackageProperty;

        public Package Package
        {
            get => (Package)GetValue(PackageProperty);
            set => SetValue(PackageProperty, value);
        }

        DependencyProperty IconNameProperty;

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, (string)value);
        }

        public MenuItemForPackage() : base()
        {
            Style = (Style)Application.Current.Resources["BetterMenuItem"];
            PackageProperty = DependencyProperty.Register(
                nameof(Package),
                typeof(Package),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

            IconNameProperty = DependencyProperty.Register(
                nameof(IconName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    Icon = new LocalIcon(e.NewValue as string);
                })));

            Click += (s, e) => { Invoked?.Invoke(this, Package); };
        }

    }
}
