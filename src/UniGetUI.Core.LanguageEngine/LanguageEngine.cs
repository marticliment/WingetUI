using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using Jeffijoe.MessageFormat;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.Core.Language
{
    public class LanguageEngine
    {
        private Dictionary<string, string> MainLangDict = [];

        [NotNull]
        public string? Locale { get; private set; }

        private MessageFormatter? Formatter;

        public LanguageEngine(string ForceLanguage = "")
        {
            string LangName = Settings.GetValue("PreferredLanguage");
            if (LangName is "default" or "")
            {
                LangName = CultureInfo.CurrentUICulture.ToString().Replace("-", "_");
            }
            LoadLanguage((ForceLanguage != "") ? ForceLanguage : LangName);
        }

        /// <summary>
        /// Loads the specified language into the current instance
        /// </summary>
        /// <param name="lang">the language code</param>
        public void LoadLanguage(string lang)
        {
            Locale = "en";
            if (LanguageData.LanguageReference.ContainsKey(lang))
            {
                Locale = lang;
            }
            else if (LanguageData.LanguageReference.ContainsKey(lang[0..2]))
            {
                Locale = lang[0..2];
            }

            MainLangDict = LoadLanguageFile(Locale);
            Formatter = new() { Locale = Locale.Replace('_', '-') };

            LoadStaticTranslation();
            Logger.Info("Loaded language locale: " + Locale);
        }

        public Dictionary<string, string> LoadLanguageFile(string LangKey, bool ForceBundled = false)
        {
            try
            {
                Dictionary<string, string> LangDict = [];
                string LangFileToLoad = Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_" + LangKey + ".json");

                if (!File.Exists(LangFileToLoad) || Settings.Get("DisableLangAutoUpdater"))
                {
                    ForceBundled = true;
                }

                if (ForceBundled)
                {
                    LangFileToLoad = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Languages", "lang_" + LangKey + ".json");
                    if (!File.Exists(LangFileToLoad))
                    {
                        Logger.Error($"Tried to access a non-existing bundled language file! file={LangFileToLoad}");
                    }
                }

                Dictionary<string, string>? __LangDict = (JsonNode.Parse(File.ReadAllText(LangFileToLoad)) as JsonObject)?.ToDictionary(x => x.Key, x => x.Value != null ? x.Value.ToString() : "");

                if (__LangDict != null)
                {
                    LangDict = __LangDict;
                }
                else
                {
                    Logger.Error($"Deserialization of language file {LangFileToLoad} resulted in a null object");
                }

                if (!Settings.Get("DisableLangAutoUpdater"))
                {
                    _ = DownloadUpdatedLanguageFile(LangKey);
                }

                return LangDict;
            }
            catch (Exception e)
            {
                Logger.Error($"LoadLanguageFile Failed for LangKey={LangKey}, ForceBundled={ForceBundled}");
                Logger.Error(e);
                return [];
            }
        }

        /// <summary>
        /// Downloads and saves an updated version of the translations for the specified language.
        /// </summary>
        /// <param name="LangKey">The Id of the language to download</param>
        public async Task DownloadUpdatedLanguageFile(string LangKey)
        {
            try
            {
                Uri NewFile = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_" + LangKey + ".json");

                HttpClient client = new(CoreData.GenericHttpClientParameters);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                string fileContents = await client.GetStringAsync(NewFile);

                if (!Directory.Exists(CoreData.UniGetUICacheDirectory_Lang))
                {
                    Directory.CreateDirectory(CoreData.UniGetUICacheDirectory_Lang);
                }

                File.WriteAllText(Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_" + LangKey + ".json"), fileContents);

                Logger.ImportantInfo("Lang files were updated successfully from GitHub");
            }
            catch (Exception e)
            {
                Logger.Warn("Could not download updated translations from GitHub");
                Logger.Warn(e);
            }
        }

        public void LoadStaticTranslation()
        {
            CommonTranslations.ScopeNames[PackageScope.Local] = Translate("User | Local");
            CommonTranslations.ScopeNames[PackageScope.Global] = Translate("Machine | Global");

            CommonTranslations.InvertedScopeNames.Clear();
            CommonTranslations.InvertedScopeNames.Add(Translate("Machine | Global"), PackageScope.Global);
            CommonTranslations.InvertedScopeNames.Add(Translate("User | Local"), PackageScope.Local);
        }

        public string Translate(string key)
        {
            if (key == "WingetUI")
            {
                if (MainLangDict.TryGetValue("formerly WingetUI", out var formerly) && formerly != "")
                {
                    return "UniGetUI (" + formerly + ")";
                }

                return "UniGetUI (formerly WingetUI)";
            }

            if (key == "Formerly known as WingetUI")
            {
                return MainLangDict.GetValueOrDefault(key, key);
            }

            if (key is null or "")
            {
                return "";
            }

            if (MainLangDict.TryGetValue(key, out var value) && value != "")
            {
                return value.Replace("WingetUI", "UniGetUI");
            }

            return key.Replace("WingetUI", "UniGetUI");
        }

        public string Translate(string key, Dictionary<string, object?> dict)
        {
            return Formatter!.FormatMessage(Translate(key), dict);
        }
    }
}
