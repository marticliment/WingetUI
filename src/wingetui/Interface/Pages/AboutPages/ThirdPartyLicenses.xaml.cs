using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Core.Data;
using System.ComponentModel;
using ModernWindow.Structures;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public class LibraryLicense
    {
        public string Name { get; set; }
        public string License { get; set; }
        public Uri LicenseURL { get; set; }
        public string HomepageText { get; set; }
        public Uri HomepageUrl { get; set; }
    }

    public sealed partial class ThirdPartyLicenses : Page
    {
        public AppTools Tools = AppTools.Instance;
        public ObservableCollection<LibraryLicense> Licenses = new();

        public ThirdPartyLicenses()
        {
            this.InitializeComponent();
            foreach (string license in LicenseData.LicenseNames.Keys)
            {
                Licenses.Add(new LibraryLicense()
                {
                    Name = license,
                    License = LicenseData.LicenseNames[license],
                    LicenseURL = LicenseData.LicenseURLs[license],
                    HomepageUrl = LicenseData.HomepageUrls[license],
                    HomepageText = Tools.Translate("{0} homepage", license)
                });
            }

        }
    }
}
