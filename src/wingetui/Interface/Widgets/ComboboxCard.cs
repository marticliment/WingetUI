using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public class ComboCardEventArgs : EventArgs
    {

        public ComboCardEventArgs()
        {
        }
    }

    public sealed class ComboboxCard : SettingsCard
    {
        private ComboBox _combobox;
        private static AppTools Tools = AppTools.Instance;
        private ObservableCollection<string> _elements;
        private Dictionary<string, string> _values_ref;
        private Dictionary<string, string> _inverted_val_ref;

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        public string Elements
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        DependencyProperty SettingProperty = DependencyProperty.Register(
        nameof(SettingName),
        typeof(string),
        typeof(CheckboxCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        DependencyProperty TextProperty;

        public event EventHandler<ComboCardEventArgs> ValueChanged;

        public ComboboxCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Header = Tools.Translate((string)e.NewValue); })));

            _elements = new ObservableCollection<string>();
            _values_ref = new Dictionary<string, string>();
            _inverted_val_ref = new Dictionary<string, string>();

            _combobox = new ComboBox();
            _combobox.MinWidth = 200;
            _combobox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding() { Source = _elements });

            DefaultStyleKey = typeof(CheckboxCard);
            Content = _combobox;
        }

        public void AddItem(string name, string value)
        {
            AddItem(name, value, true);
        }

        public void AddItem(string name, string value, bool translate)
        {
            if (translate)
                name = Tools.Translate(name);
            _elements.Add(name);
            _values_ref.Add(name, value);
            _inverted_val_ref.Add(value, name);
        }


        public void ShowAddedItems()
        {
            try
            {
                string savedItem = Tools.GetSettingsValue(SettingName);
                _combobox.SelectedIndex = _elements.IndexOf(_inverted_val_ref[savedItem]);
            }
            catch
            {
                _combobox.SelectedIndex = 0;
            }
            _combobox.SelectionChanged += (sender, e) =>
            {
                try
                {
                    Tools.SetSettingsValue(SettingName, _values_ref[_combobox.SelectedItem.ToString()]);
                    ValueChanged?.Invoke(this, new ComboCardEventArgs());
                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            };
        }
    }
}
