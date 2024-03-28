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
using UniGetUI.Core.Data;
using UniGetUI.Core;
using System.Text.Json.Nodes;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Translators : Page
    {
        public ObservableCollection<Person> TranslatorList = new();
        public Translators()
        {
            this.InitializeComponent();
            JsonObject TranslatorsInfo = JsonNode.Parse(LanguageData.TranslatorsJSON).AsObject();


            foreach (KeyValuePair<string, JsonNode> langKey in TranslatorsInfo)
            {
                if (!LanguageData.LanguageList.ContainsKey(langKey.Key))
                {
                    AppTools.Log($"Language {langKey.Key} not in list, maybe has not been added yet?");
                    continue;
                }
                JsonArray TranslatorsForLang = langKey.Value.AsArray();
                bool LangShown = false;
                foreach (JsonNode translator in TranslatorsForLang)
                {
                    Uri? url = null;
                    if (translator["link"].ToString() != "")
                        url = new Uri(translator["link"].ToString());
                    Person person = new()
                    {
                        Name = (url != null ? "@" : "") + translator["name"].ToString(),
                        HasPicture = url != null,
                        HasGitHubProfile = url != null,
                        GitHubUrl = url != null ? url : new Uri("https://github.com/"),
                        ProfilePicture = url != null ? new Uri(url.ToString() + ".png") : new Uri("https://github.com/"),
                        Language = !LangShown ? LanguageData.LanguageList[langKey.Key] : "",
                    };
                    LangShown = true;
                    TranslatorList.Add(person);
                }
            }
        }
    }
}
