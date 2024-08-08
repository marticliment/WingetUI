using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public class LibraryLicense
    {
        public string Name { get; set; } = "";
        public string License { get; set; } = "";
        public Uri? LicenseURL { get; set; }
        public string HomepageText { get; set; } = "";
        public Uri? HomepageUrl { get; set; }
    }

    public sealed partial class ThirdPartyLicenses : Page
    {
        public ObservableCollection<LibraryLicense> Licenses = [];

        public ThirdPartyLicenses()
        {
            InitializeComponent();
            foreach (string license in LicenseData.LicenseNames.Keys)
            {
                Licenses.Add(new LibraryLicense
                {
                    Name = license,
                    License = LicenseData.LicenseNames[license],
                    LicenseURL = LicenseData.LicenseURLs[license],
                    HomepageUrl = LicenseData.HomepageUrls[license],
                    HomepageText = CoreTools.Translate("{0} homepage", license)
                });
            }

        }
    }
}
