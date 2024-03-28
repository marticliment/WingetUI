using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UniGetUI.Core.Data;
using UniGetUI.Interface.Pages.AboutPages;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class AboutWingetUI : Page
    {

        AppTools Tools = AppTools.Instance;

        int previousSelectedIndex = 0;
        public AboutWingetUI()
        {
            InitializeComponent();
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            System.Type pageType;

            switch (currentSelectedIndex)
            {
                case 0:
                    pageType = typeof(Pages.AboutPages.AboutWingetUI);
                    break;
                case 1:
                    pageType = typeof(ThirdPartyLicenses);
                    break;
                case 2:
                    pageType = typeof(Contributors);
                    break;
                case 3:
                    pageType = typeof(Translators);
                    break;
                default:
                    pageType = typeof(SupportMe);
                    break;
            }

            var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

            ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

            previousSelectedIndex = currentSelectedIndex;

        }
    }
}
