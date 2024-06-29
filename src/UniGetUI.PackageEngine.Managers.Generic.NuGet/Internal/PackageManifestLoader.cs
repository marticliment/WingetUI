using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Generic.NuGet.Internal
{
    internal static class PackageManifestLoader
    {
        private static readonly Dictionary<string, string> __manifest_cache = new();

        /// <summary>
        /// Returns the URL to the manifest of a NuGet-based package
        /// </summary>
        /// <param name="package">A valid Package object</param>
        /// <returns>A Uri object</returns>
        public static Uri GetPackageManifestUrl(IPackage package)
        {
            return new Uri($"{package.Source.Url}/Packages(Id='{package.Id}',Version='{package.Version}')");
        }

        /// <summary>
        /// Returns the URL to the NuPk file
        /// </summary>
        /// <param name="package">A valid Package object</param>
        /// <returns>A Uri object</returns>
        public static Uri GetPackageNuGetPackageUrl(IPackage package)
        {
            return new Uri($"{package.Source.Url}/Packages/{package.Id}.{package.Version}.nupkg");
        }

        /// <summary>
        /// Returns the contents of the manifest of a NuGet-based package
        /// </summary>
        /// <param name="package">The package for which to obtain the manifest</param>
        /// <returns>A string containing the contents of the manifest</returns>
        public static async Task<string?> GetPackageManifestContent(IPackage package)
        {
            string? PackageManifestContent = "";
            string PackageManifestUrl = GetPackageManifestUrl(package).ToString();
            if (__manifest_cache.ContainsKey(PackageManifestUrl))
            {
                Logger.Debug($"Loading cached NuGet manifest for package {package.Id} on manager {package.Manager.Name}");
                return __manifest_cache[PackageManifestUrl];
            }

            try
            {
                using (HttpClient client = new(CoreData.GenericHttpClientParameters))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                    HttpResponseMessage response = await client.GetAsync(PackageManifestUrl);
                    if (!response.IsSuccessStatusCode && package.Version.EndsWith(".0"))
                        response = await client.GetAsync(new Uri(PackageManifestUrl.ToString().Replace(".0')", "')")));

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifestUrl.ToString()} with status code {response.StatusCode}");
                        return null;
                    }

                    PackageManifestContent = await response.Content.ReadAsStringAsync();
                }
                __manifest_cache[PackageManifestUrl] = PackageManifestContent;
                return PackageManifestContent;
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifestUrl.ToString()}");
                Logger.Warn(e);
                return null;
            }
        }
    }
}
