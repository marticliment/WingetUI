using System.Text.RegularExpressions;
using System.Web;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    public abstract class BaseNuGet : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };

        public BaseNuGet() : base()
        {
            PackageDetailsProvider = new BaseNuGetDetailsProvider(this);
        }

        public sealed override async Task InitializeAsync()
        {
            if(PackageDetailsProvider is not BaseNuGetDetailsProvider)
                throw new Exception("NuGet-based package managers must not reassign the PackageDetailsProvider property");

            if (!Capabilities.SupportsCustomVersions)
                throw new Exception("NuGet-based package managers must support custom versions");
            if (!Capabilities.SupportsCustomPackageIcons)
                throw new Exception("NuGet-based package managers must support custom versions");

            await base.InitializeAsync();
        }

        private struct SearchResult
        {
            public string version;
            public double version_float;
            public string id;
        }

        protected sealed override async Task<IPackage[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = new();

            ManagerClasses.Classes.NativeTaskLogger logger = TaskLogger.CreateNew(Enums.LoggableTaskType.FindPackages);

            IManagerSource[] sources;
            if (Capabilities.SupportsCustomSources)
                sources = await GetSources();
            else
                sources = [ Properties.DefaultSource ];
            
            foreach(IManagerSource source in sources)
            {
                Uri SearchUrl = new($"{source.Url}/Search()?searchTerm=%27{HttpUtility.UrlEncode(query)}%27&targetFramework=%27%27&includePrerelease=false");
                logger.Log($"Begin package search with url={SearchUrl} on manager {Name}"); ;

                using (HttpClient client = new(CoreData.GenericHttpClientParameters))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                    HttpResponseMessage response = await client.GetAsync(SearchUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.Error($"Failed to fetch api at Url={SearchUrl} with status code {response.StatusCode}");
                        continue;
                    }

                    string SearchResults = await response.Content.ReadAsStringAsync();
                    MatchCollection matches = Regex.Matches(SearchResults, "<entry>([\\s\\S]*?)<\\/entry>");

                    Dictionary<string, SearchResult> AlreadyProcessedPackages = new();

                    foreach (Match match in matches)
                    {
                        if (!match.Success) continue;

                        string id = Regex.Match(match.Value, "Id='([^<>']+)'").Groups[1].Value;
                        string version = Regex.Match(match.Value, "Version='([^<>']+)'").Groups[1].Value;
                        double float_version = CoreTools.GetVersionStringAsFloat(version);
                        Match title = Regex.Match(match.Value, "<title[ \\\"\\=A-Za-z0-9]+>([^<>]+)<\\/title>");

                        if (AlreadyProcessedPackages.ContainsKey(id) && AlreadyProcessedPackages[id].version_float >= float_version)
                            continue;

                        AlreadyProcessedPackages[id] = new SearchResult { id = id, version = version, version_float = float_version };
                    }
                    foreach (SearchResult package in AlreadyProcessedPackages.Values)
                    {
                        logger.Log($"Found package {package.id} version {package.version} on source {source.Name}");
                        Packages.Add(new Package(CoreTools.FormatAsName(package.id), package.id, package.version, source, this));
                    }

                }
            }

            logger.Close(0);

            return Packages.ToArray();
        }
        
    }

}
