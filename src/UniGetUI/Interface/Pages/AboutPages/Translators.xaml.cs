using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Language;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Translators : Page
    {
        public ObservableCollection<Person> TranslatorList = [];
        public Translators()
        {
            InitializeComponent();
            foreach (Person person in LanguageData.TranslatorsList)
            {
                TranslatorList.Add(person);
            }
        }
    }
}
