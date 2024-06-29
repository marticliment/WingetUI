using System.Text.RegularExpressions;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet.Internal;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    public class BaseNuGetDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public BaseNuGetDetailsProvider(BaseNuGet manager) : base(manager) { }

        protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
        {
            ManagerClasses.Classes.NativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageDetails);
            try
            {
                details.ManifestUrl = PackageManifestLoader.GetPackageManifestUrl(details.Package);
                string? PackageManifestContents = await PackageManifestLoader.GetPackageManifestContent(details.Package);
                logger.Log(PackageManifestContents);

                if (PackageManifestContents == null)
                {
                    logger.Error($"No manifest content could be loaded for package {details.Package.Id} on manager {details.Package.Manager.Name}, returning empty PackageDetails");
                    logger.Close(1);
                    return;
                }

                details.InstallerUrl = PackageManifestLoader.GetPackageNuGetPackageUrl(details.Package);
                details.InstallerType = CoreTools.Translate("NuPkg (zipped manifest)");
                details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<name>[^<>]+<\/name>"))
                {
                    details.Author = match.Value.Replace("<name>", "").Replace("</name>", "");
                    details.Publisher = match.Value.Replace("<name>", "").Replace("</name>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:Description>[^<>]+<\/d:Description>"))
                {
                    details.Description = match.Value.Replace("<d:Description>", "").Replace("</d:Description>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<updated>[^<>]+<\/updated>"))
                {
                    details.UpdateDate = match.Value.Replace("<updated>", "").Replace("</updated>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:ProjectUrl>[^<>]+<\/d:ProjectUrl>"))
                {
                    details.HomepageUrl = new Uri(match.Value.Replace("<d:ProjectUrl>", "").Replace("</d:ProjectUrl>", ""));
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:LicenseUrl>[^<>]+<\/d:LicenseUrl>"))
                {
                    details.LicenseUrl = new Uri(match.Value.Replace("<d:LicenseUrl>", "").Replace("</d:LicenseUrl>", ""));
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:PackageHash>[^<>]+<\/d:PackageHash>"))
                {
                    details.InstallerHash = match.Value.Replace("<d:PackageHash>", "").Replace("</d:PackageHash>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:ReleaseNotes>[^<>]+<\/d:ReleaseNotes>"))
                {
                    details.ReleaseNotes = match.Value.Replace("<d:ReleaseNotes>", "").Replace("</d:ReleaseNotes>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:LicenseNames>[^<>]+<\/d:LicenseNames>"))
                {
                    details.License = match.Value.Replace("<d:LicenseNames>", "").Replace("</d:LicenseNames>", "");
                    break;
                }

                logger.Close(0);
                return;
            }
            catch (Exception e)
            {
                logger.Error(e);
                logger.Close(1);
                return;
            }
        }

        protected override async Task<CacheableIcon?> GetPackageIcon_Unsafe(IPackage package)
        {
            string? PackageManifestContent = await PackageManifestLoader.GetPackageManifestContent(package);
            if (PackageManifestContent == null)
            {
                Logger.Warn($"No manifest content could be loaded for package {package.Id} on manager {package.Manager.Name}");
                return null;
            }

            Match possibleIconUrl = Regex.Match(PackageManifestContent, "<(?:d\\:)?IconUrl>(.*)<(?:\\/d:)?IconUrl>");

            if (!possibleIconUrl.Success)
            {
                Logger.Warn($"No Icon URL could be parsed on the manifest Url={PackageManifestLoader.GetPackageManifestUrl(package).ToString()}");
                return null;
            }

            Logger.Debug($"A native icon with Url={possibleIconUrl.Groups[1].Value} was found");
            return new CacheableIcon(new Uri(possibleIconUrl.Groups[1].Value), package.Version);
        }

        protected override Task<Uri[]> GetPackageScreenshots_Unsafe(IPackage package)
        {
            throw new NotImplementedException();
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
        {
            Uri SearchUrl = new($"{package.Source.Url}/FindPackagesById()?id='{package.Id}'");
            Logger.Debug($"Begin package version search with url={SearchUrl} on manager {Manager.Name}");

            List<string> results = new();

            HttpClient client = new(CoreData.GenericHttpClientParameters);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);

            HttpResponseMessage response = await client.GetAsync(SearchUrl);
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Failed to fetch api at Url={SearchUrl} with status code {response.StatusCode} to load versions");
                return [];
            }

            string SearchResults = await response.Content.ReadAsStringAsync();
            HashSet<string> alreadyProcessed = new();

            MatchCollection matches = Regex.Matches(SearchResults, "Version='([^<>']+)'");
            foreach (Match match in matches)
            {
                if (!alreadyProcessed.Contains(match.Groups[1].Value) && match.Success)
                {
                    results.Add(match.Groups[1].Value);
                    alreadyProcessed.Add(match.Groups[1].Value);
                }
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results.ToArray();
        }
    }
}
