using Microsoft.Management.Deployment;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUIManagers = UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal class WinGetPackageDetailsProvider : BasePackageDetailsProvider<UniGetUIManagers.PackageManager>
    {
        private static readonly Dictionary<string, string> __msstore_package_manifests = new();

        struct MicrosoftStoreProductType
        {
            public string productIds { get; set; }
        }

        public WinGetPackageDetailsProvider(WinGet manager) : base(manager) { }
        
        protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
        {
            return await WinGetHelper.Instance.GetPackageVersions_Unsafe((WinGet)Manager, package);
        }

        protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
        {
            await WinGetHelper.Instance.GetPackageDetails_UnSafe((WinGet)Manager, details);
        }

        protected override async Task<CacheableIcon?> GetPackageIcon_Unsafe(IPackage package)
        {
            
            if(package.Source.Name == "msstore")
                return await GetMicrosoftStorePackageIcon(package);

            // Logger.Warn("Non-MSStore WinGet Native Icons have been forcefully disabled on code");
            // return null;
            return await GetWinGetPackageIcon(package);
        }

        protected override async Task<Uri[]> GetPackageScreenshots_Unsafe(IPackage package)
        {
            if (package.Source.Name != "msstore")
                return [];

            string? ResponseContent = await GetMicrosoftStorePackageManifest(package);
            if (ResponseContent == null)
                return [];

            Match IconArray = Regex.Match(ResponseContent, "(?:\"|')Images(?:\"|'): ?\\[([^\\]]+)\\]");
            if (!IconArray.Success)
            {
                Logger.Warn($"Could not parse Images array from Microsoft Store response");
                return [];
            }

            List<Uri> FoundIcons = new();

            foreach (Match ImageEntry in Regex.Matches(IconArray.Groups[1].Value, "{([^}]+)}"))
            {

                if (!ImageEntry.Success)
                    continue;

                Match ImagePurpose = Regex.Match(ImageEntry.Groups[1].Value, "(?:\"|')ImagePurpose(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                if (!ImagePurpose.Success || ImagePurpose.Groups[1].Value != "Screenshot")
                    continue;

                Match ImageUrl = Regex.Match(ImageEntry.Groups[1].Value, "(?:\"|')Uri(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                if (!ImageUrl.Success) 
                    continue;

                FoundIcons.Add(new Uri("https:" + ImageUrl.Groups[1].Value));
            }

            return FoundIcons.ToArray();
        }


        private async Task<string?> GetMicrosoftStorePackageManifest(IPackage package)
        {
            if(__msstore_package_manifests.ContainsKey(package.Id))
                return __msstore_package_manifests[package.Id];

            string CountryCode = CultureInfo.CurrentCulture.Name.Split("-")[^1];
            string Locale = CultureInfo.CurrentCulture.Name;
            string url = $"https://storeedgefd.dsx.mp.microsoft.com/v8.0/sdk/products?market={CountryCode}&locale={Locale}&deviceFamily=Windows.Desktop";

#pragma warning disable SYSLIB0014
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014

            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/json";

            string data = "{\"productIds\": \"" + package.Id.ToLower() + "\"}";

            using (StreamWriter streamWriter = new(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }

            HttpWebResponse httpResponse = (HttpWebResponse)await httpRequest.GetResponseAsync();
            string result;
            using (StreamReader streamReader = new(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            Logger.Debug("Microsoft Store API call status code: " + httpResponse.StatusCode);

            if(result != "" && httpResponse.StatusCode == HttpStatusCode.OK) __msstore_package_manifests[package.Id] = result;
            return result;
        }

        private async Task<CacheableIcon?> GetMicrosoftStorePackageIcon(IPackage package)
        {
            string? ResponseContent = await GetMicrosoftStorePackageManifest(package);
            if (ResponseContent == null)
                return null;

            Match IconArray = Regex.Match(ResponseContent, "(?:\"|')Images(?:\"|'): ?\\[([^\\]]+)\\]");
            if (!IconArray.Success)
            {
                Logger.Warn($"Could not parse Images array from Microsoft Store response");
                return null;
            }

            Dictionary<int, string> FoundIcons = new();

            foreach (Match ImageEntry in Regex.Matches(IconArray.Groups[1].Value, "{([^}]+)}"))
            {
                string CurrentImage = ImageEntry.Groups[1].Value;

                if (!ImageEntry.Success)
                    continue;

                Match ImagePurpose = Regex.Match(CurrentImage, "(?:\"|')ImagePurpose(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                if (!ImagePurpose.Success || ImagePurpose.Groups[1].Value != "Tile")
                    continue;

                Match ImageUrl = Regex.Match(CurrentImage, "(?:\"|')Uri(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                Match ImageSize = Regex.Match(CurrentImage, "(?:\"|')Height(?:\"|'): ?([^,]+)");

                if (!ImageUrl.Success || !ImageSize.Success)
                    continue;

                FoundIcons[int.Parse(ImageSize.Groups[1].Value)] = ImageUrl.Groups[1].Value;
            }

            if (FoundIcons.Count == 0)
            {
                Logger.Warn($"No Logo image found for package {package.Id} in Microsoft Store response");
                return null;
            }

            Logger.Debug("Choosing icon with size " + FoundIcons.Keys.Max() + " for package " + package.Id + " from Microsoft Store");

            string uri = "https:" + FoundIcons[FoundIcons.Keys.Max()];

            return new CacheableIcon(new Uri(uri));
        }


        private async Task<CacheableIcon?> GetWinGetPackageIcon(IPackage package)
        { // TODO: Need to work on retrieving WinGet icons

            if (WinGetHelper.Instance is not NativeWinGetHelper)
            {
                Logger.Warn("WinGet will not attempt to load icon since the helper is using bundled WinGet");
                return null;
            }

            Microsoft.Management.Deployment.PackageManager WinGetManager = ((NativeWinGetHelper)WinGetHelper.Instance).WinGetManager;
            WindowsPackageManager.Interop.WindowsPackageManagerStandardFactory Factory = ((NativeWinGetHelper)WinGetHelper.Instance).Factory;

            // Find the native package for the given Package object
            PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if (Catalog == null)
            {
                Logger.Error("[WINGET COM] Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return null;
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            ConnectResult ConnectResult = await Task.Run(() => Catalog.Connect());
            // ConnectResult ConnectResult = await Catalog.ConnectAsync();
            if (ConnectResult.Status != ConnectResultStatus.Ok)
            {
                Logger.Error("[WINGET COM] Failed to connect to catalog " + package.Source.Name);
                return null;
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            Task<FindPackagesResult> SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                Logger.Error("[WINGET COM] Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                return null;
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;

            // Extract data from NativeDetails
            CatalogPackageMetadata NativeDetails = await Task.Run(() => NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata());

            CacheableIcon? Icon = null;

            foreach (Icon? icon in NativeDetails.Icons.ToArray())
            {
                Icon = new CacheableIcon(new Uri(icon.Url), icon.Sha256);
                Logger.Debug($"Found WinGet native icon for {package.Id} with URL={icon.Url}");
            }

            return Icon;
        }

    }
}
