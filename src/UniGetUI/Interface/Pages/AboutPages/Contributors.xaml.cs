using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Contributors : Page
    {
        public ObservableCollection<Person> ContributorList = [];
        public Contributors()
        {
            InitializeComponent();
            foreach (string contributor in ContributorsData.Contributors)
            {
                Person person = new(
                    Name: "@" + contributor,
                    ProfilePicture: new Uri("https://github.com/" + contributor + ".png"),
                    GitHubUrl: new Uri("https://github.com/" + contributor)
                );
                ContributorList.Add(person);
            }
        }
    }
}
