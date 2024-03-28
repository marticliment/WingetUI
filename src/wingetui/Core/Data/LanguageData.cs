using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core;
using Nancy.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Media.Streaming.Adaptive;

namespace UniGetUI.Core.Data
{
    public static class LanguageData
    {
        public static string TranslatorsJSON = File.ReadAllText(
            Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Data", "Translators.json")
        );

        public static Dictionary<string, string> LanguageList = (JsonObject.Parse(
            File.ReadAllText(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Data", "LanguagesReference.json"))
        ) as JsonObject).ToDictionary(x => x.Key, x => x.Value.ToString());

        public static Dictionary<string, string> TranslatedPercentages = (JsonObject.Parse(
            File.ReadAllText(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Data", "TranslatedPercentages.json"))
        ) as JsonObject).ToDictionary(x => x.Key, x => x.Value.ToString());
    }

    public class LanguageEngine
    {
        public static Dictionary<string, string> MainLangDict = new();

        public LanguageEngine()
        {
            string LangName = AppTools.GetSettingsValue_Static("PreferredLanguage");
            if (LangName == "default" || LangName == "")
            {
                LangName = System.Globalization.CultureInfo.CurrentCulture.ToString().Replace("-", "_");
            }

            if (LanguageData.LanguageList.ContainsKey(LangName))
            {
                MainLangDict = LoadLanguageFile(LangName);
                MainLangDict.TryAdd("locale", LangName);
            }
            else if (LanguageData.LanguageList.ContainsKey(LangName[0..2]))
            {
                MainLangDict = LoadLanguageFile(LangName[0..2]);
                MainLangDict.TryAdd("locale", LangName[0..2]);
            }
            else
            {
                MainLangDict = LoadLanguageFile("en");
                MainLangDict.TryAdd("locale", "en");
            }
            LoadStaticTranslation();
            AppTools.Log("Loaded language locale: " + MainLangDict["locale"]);
        }

        public Dictionary<string, string> LoadLanguageFile(string LangKey, bool ForceBundled = false)
        {
            try
            {
                Dictionary<string, string> LangDict = new();
                string LangFileToLoad = Path.Join(CoreData.WingetUICacheDirectory_Lang, "lang_" + LangKey + ".json");
                AppTools.Log(LangFileToLoad);

                if (!File.Exists(LangFileToLoad) || AppTools.GetSettings_Static("DisableLangAutoUpdater"))
                {
                    ForceBundled = true;
                }

                if (ForceBundled)
                {
                    LangFileToLoad = Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Languages", "lang_" + LangKey + ".json");
                    AppTools.Log(LangFileToLoad);
                }

                LangDict = (JsonNode.Parse(File.ReadAllText(LangFileToLoad)) as JsonObject).ToDictionary(x => x.Key, x => x.Value != null ? x.Value.ToString() : "");

                if (!AppTools.GetSettings_Static("DisableLangAutoUpdater"))
                    _ = UpdateLanguageFile(LangKey);

                return LangDict;
            }
            catch (Exception e)
            {
                AppTools.Log($"LoadLanguageFile Failed for LangKey={LangKey}, ForceBundled={ForceBundled}");
                AppTools.Log(e);
                return new Dictionary<string, string>();
            }
        }

        public async Task UpdateLanguageFile(string LangKey)
        {
            try
            {
                Uri NewFile = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/src/wingetui/Assets/Languages/" + "lang_" + LangKey + ".json");

                HttpClient client = new();
                string fileContents = await client.GetStringAsync(NewFile);

                if (!Directory.Exists(CoreData.WingetUICacheDirectory_Lang))
                    Directory.CreateDirectory(CoreData.WingetUICacheDirectory_Lang);

                File.WriteAllText(Path.Join(CoreData.WingetUICacheDirectory_Lang, "lang_" + LangKey + ".json"), fileContents);

                AppTools.Log("Lang files were updated successfully");
            }
            catch (Exception e)
            {
                AppTools.Log(e);
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
                if (MainLangDict.ContainsKey("formerly WingetUI"))
                    return "UniGetUI (" + MainLangDict["formerly WingetUI"] + ")";
                return "UniGetUI (formerly WingetUI)";
            }
            else if (key == "Formerly known as WingetUI")
            {
                if (MainLangDict.ContainsKey(key))
                    return MainLangDict[key];
                return key;
            }

            if (key == null || key == "")
                return "";
            else if (MainLangDict.ContainsKey(key) && MainLangDict[key] != "")
                return MainLangDict[key].Replace("WingetUI", "UniGetUI");
            else
                return key.Replace("WingetUI", "UniGetUI");
        }
    }

    public static class CommonTranslations
    {
        public static Dictionary<Architecture, string> ArchNames = new()
        {
            { Architecture.X64, "x64" },
            { Architecture.X86, "x86" },
            { Architecture.Arm64, "arm64" },
            { Architecture.Arm, "arm32" },
        };

        public static Dictionary<string, Architecture> InvertedArchNames = new()
        {
            { "x64", Architecture.X64 },
            { "x86", Architecture.X86 },
            { "arm64", Architecture.Arm64 },
            { "arm32", Architecture.Arm },
        };

        public static Dictionary<PackageScope, string> ScopeNames = new()
        {
            { PackageScope.Global, "Machine | Global" },
            { PackageScope.Local, "User | Local" },
        };

        public static Dictionary<string, PackageScope> InvertedScopeNames = new()
        {
            { "Machine | Global", PackageScope.Global },
            { "User | Local", PackageScope.Local },
        };

        public static Dictionary<PackageScope, string> ScopeNames_NonLang = new()
        {
            { PackageScope.Global, "machine" },
            { PackageScope.Local, "user" },
        };

        public static Dictionary<string, PackageScope> InvertedScopeNames_NonLang = new()
        {
            { "machine", PackageScope.Global },
            { "user", PackageScope.Local },
        };
    }
}

