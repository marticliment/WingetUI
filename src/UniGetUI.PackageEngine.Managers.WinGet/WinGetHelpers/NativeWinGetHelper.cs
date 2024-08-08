using System.Diagnostics;
using Microsoft.Management.Deployment;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;
using WindowsPackageManager.Interop;

namespace UniGetUI.PackageEngine.Managers.WingetManager;

internal sealed class NativeWinGetHelper : IWinGetManagerHelper
{
    public WindowsPackageManagerFactory Factory;
    public PackageManager WinGetManager;

    public NativeWinGetHelper()
    {
        if (Settings.Get("ForceUsePowerShellModules"))
        {
            throw new InvalidOperationException("User requested to disable the WinGet COM API, crashing...");
        }

        if (CoreTools.IsAdministrator())
        {
            Logger.Info("Running elevated, WinGet class registration is likely to fail");
        }

        Factory = new WindowsPackageManagerStandardFactory();
        WinGetManager = Factory.CreatePackageManager();
    }

    public async Task<Package[]> FindPackages_UnSafe(WinGet Manager, string query)
    {
        List<Package> Packages = [];
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.FindPackages);
        Dictionary<(PackageCatalogReference, PackageMatchField), Task<FindPackagesResult>> FindPackageTasks = [];
        
        // Load catalogs
        logger.Log("Loading available catalogs...");
        IReadOnlyList<PackageCatalogReference> AvailableCatalogs = WinGetManager.GetPackageCatalogs();

        // Spawn Tasks to find packages on catalogs
        logger.Log("Spawning catalog fetching tasks...");
        foreach (PackageCatalogReference CatalogReference in AvailableCatalogs.ToArray())
        {
            logger.Log($"Begin search on catalog {CatalogReference.Info.Name}");
            // Connect to catalog
            CatalogReference.AcceptSourceAgreements = true;
            ConnectResult result = await CatalogReference.ConnectAsync();
            if (result.Status == ConnectResultStatus.Ok)
            {
                foreach (var filter_type in new PackageMatchField[] { PackageMatchField.Name, PackageMatchField.Id, PackageMatchField.Moniker })
                {
                    FindPackagesOptions PackageFilters = Factory.CreateFindPackagesOptions();

                    logger.Log("Generating filters...");
                    // Name filter
                    PackageMatchFilter FilterName = Factory.CreatePackageMatchFilter();
                    FilterName.Field = filter_type;
                    FilterName.Value = query;
                    FilterName.Option = PackageFieldMatchOption.ContainsCaseInsensitive;
                    PackageFilters.Filters.Add(FilterName);

                    try
                    {
                        // Create task and spawn it
                        Task<FindPackagesResult> task = new(() => result.PackageCatalog.FindPackages(PackageFilters));
                        task.Start();

                        // Add task to list
                        FindPackageTasks.Add(
                            (CatalogReference, filter_type),
                            task
                        );
                    }
                    catch (Exception e)
                    {
                        logger.Error("WinGet: Catalog " + CatalogReference.Info.Name +
                                        " failed to spawn FindPackages task.");
                        logger.Error(e);
                    }
                }
            }
            else
            {
                logger.Error("WinGet: Catalog " + CatalogReference.Info.Name + " failed to connect.");
            }
        }

        // Wait for tasks completion
        await Task.WhenAll(FindPackageTasks.Values.ToArray());
        logger.Log($"All catalogs fetched. Fetching results for query piece {query}");

        foreach (var CatalogTaskPair in FindPackageTasks)
        {
            try
            {
                // Get the source for the catalog
                IManagerSource source = Manager.GetSourceOrDefault(CatalogTaskPair.Key.Item1.Info.Name);

                FindPackagesResult FoundPackages = CatalogTaskPair.Value.Result;
                foreach (MatchResult package in FoundPackages.Matches.ToArray())
                {
                    CatalogPackage catPkg = package.CatalogPackage;
                    // Create the Package item and add it to the list
                    logger.Log(
                        $"Found package: {catPkg.Name}|{catPkg.Id}|{catPkg.DefaultInstallVersion.Version} on catalog {source.Name}");

                    var overriden_options = new OverridenInstallationOptions();

                    var installOptions = Factory.CreateInstallOptions();
                    if (catPkg.DefaultInstallVersion.HasApplicableInstaller(installOptions))
                    {
                        var options = catPkg.DefaultInstallVersion.GetApplicableInstaller(installOptions);
                        if (options.ElevationRequirement is ElevationRequirement.ElevationRequired or ElevationRequirement.ElevatesSelf)
                            overriden_options.RunAsAdministrator = true;
                        else if (options.ElevationRequirement is ElevationRequirement.ElevationProhibited)
                            overriden_options.RunAsAdministrator = false;
                    }

                    Packages.Add(new Package(
                        catPkg.Name,
                        catPkg.Id,
                        catPkg.DefaultInstallVersion.Version,
                        source,
                        Manager,
                        overriden_options
                    ));
                }
            }
            catch (Exception e)
            {
                logger.Error("WinGet: Catalog " + CatalogTaskPair.Key.Item1.Info.Name +
                                " failed to get available packages.");
                logger.Error(e);
            }
        }

        logger.Close(0);
        return Packages.ToArray();
    }

    public async Task<Package[]> GetAvailableUpdates_UnSafe(WinGet Manager)
    {
        var logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListUpdates);
        List<Package> packages = [];
        foreach (var nativePackage in await Task.Run(() => GetLocalWinGetPackages(logger)))
        {
            if (nativePackage.IsUpdateAvailable)
            {
                IManagerSource source;
                source = Manager.GetSourceOrDefault(nativePackage.DefaultInstallVersion.PackageCatalog.Info.Name);
                packages.Add(new Package(nativePackage.Name, nativePackage.Id, nativePackage.InstalledVersion.Version, nativePackage.DefaultInstallVersion.Version, source, Manager));
                logger.Log($"Found package {nativePackage.Name} {nativePackage.Id} on source {source.Name}, from version {nativePackage.InstalledVersion.Version} to version {nativePackage.DefaultInstallVersion.Version}");
            }
        }

        logger.Close(0);
        return packages.ToArray();

    }

    public async Task<Package[]> GetInstalledPackages_UnSafe(WinGet Manager)
    {
        var logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages);
        List<Package> packages = [];
        foreach (var nativePackage in await Task.Run(() => GetLocalWinGetPackages(logger)))
        {
            IManagerSource source;
            if (nativePackage.DefaultInstallVersion != null)
            {
                source = Manager.GetSourceOrDefault(nativePackage.DefaultInstallVersion.PackageCatalog.Info.Name);
            }
            else
            {
                source = Manager.GetLocalSource(nativePackage.Id);
            }
            logger.Log($"Found package {nativePackage.Name} {nativePackage.Id} on source {source.Name}");
            packages.Add(new Package(nativePackage.Name, nativePackage.Id, nativePackage.InstalledVersion.Version, source, Manager));
        }
        logger.Close(0);
        return packages.ToArray();
    }

    private IEnumerable<CatalogPackage> GetLocalWinGetPackages(INativeTaskLogger logger)
    {
        PackageCatalogReference installedSearchCatalogRef;
        CreateCompositePackageCatalogOptions createCompositePackageCatalogOptions = Factory.CreateCreateCompositePackageCatalogOptions();
        foreach (var catalogRef in WinGetManager.GetPackageCatalogs().ToArray())
        {
            logger.Log($"Adding catalog {catalogRef.Info.Name} to composite catalog");
            createCompositePackageCatalogOptions.Catalogs.Add(catalogRef);
        }
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs;
        installedSearchCatalogRef = WinGetManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);

        var ConnectResult = installedSearchCatalogRef.Connect();
        if (ConnectResult.Status != ConnectResultStatus.Ok)
        {
            logger.Error("Failed to connect to installedSearchCatalogRef. Aborting.");
            logger.Close(1);
            throw new InvalidOperationException("WinGet: Failed to connect to composite catalog.");
        }

        FindPackagesOptions findPackagesOptions = Factory.CreateFindPackagesOptions();
        PackageMatchFilter filter = Factory.CreatePackageMatchFilter();
        filter.Field = PackageMatchField.Id;
        filter.Option = PackageFieldMatchOption.StartsWithCaseInsensitive;
        filter.Value = "";
        findPackagesOptions.Filters.Add(filter);

        var TaskResult = ConnectResult.PackageCatalog.FindPackages(findPackagesOptions);
        List<CatalogPackage> foundPackages = [];
        foreach (var match in TaskResult.Matches.ToArray())
        {
            foundPackages.Add(match.CatalogPackage);
        }

        return foundPackages;
    }

    public async Task<IManagerSource[]> GetSources_UnSafe(WinGet Manager)
    {
        List<ManagerSource> sources = [];
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListSources);

        foreach (PackageCatalogReference catalog in await Task.Run(() => WinGetManager.GetPackageCatalogs().ToArray()))
        {
            try
            {
                logger.Log($"Found source {catalog.Info.Name} with argument {catalog.Info.Argument}");
                sources.Add(new ManagerSource(
                    Manager,
                    catalog.Info.Name,
                    new Uri(catalog.Info.Argument),
                    updateDate: (catalog.Info.LastUpdateTime.Second != 0 ? catalog.Info.LastUpdateTime : DateTime.Now).ToString()));
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        logger.Close(0);
        return sources.ToArray();
    }

    public async Task<string[]> GetPackageVersions_Unsafe(WinGet Manager, IPackage package)
    {
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions);

        // Find the native package for the given Package object
        PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
        if (Catalog == null)
        {
            logger.Error("Failed to get catalog " + package.Source.Name + ". Is the package local?");
            logger.Close(1);
            return [];
        }

        // Connect to catalog
        Catalog.AcceptSourceAgreements = true;
        ConnectResult ConnectResult = await Task.Run(() => Catalog.Connect());
        if (ConnectResult.Status != ConnectResultStatus.Ok)
        {
            logger.Error("Failed to connect to catalog " + package.Source.Name);
            logger.Close(1);
            return [];
        }

        // Match only the exact same Id
        FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
        PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
        filters.Field = PackageMatchField.Id;
        filters.Value = package.Id;
        filters.Option = PackageFieldMatchOption.Equals;
        packageMatchFilter.Filters.Add(filters);
        packageMatchFilter.ResultLimit = 1;
        Task<FindPackagesResult> SearchResult =
            Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

        if (SearchResult.Result == null || SearchResult.Result.Matches == null ||
            SearchResult.Result.Matches.Count == 0)
        {
            logger.Error("Failed to find package " + package.Id + " in catalog " + package.Source.Name);
            logger.Close(1);
            return [];
        }

        // Get the Native Package
        CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;
        string[] versions = NativePackage.AvailableVersions.Select(x => x.Version).ToArray();
        foreach (string? version in versions)
        {
            logger.Log(version);
        }

        logger.Close(0);
        return versions ?? [];
    }

    public async Task GetPackageDetails_UnSafe(WinGet Manager, IPackageDetails details)
    {
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageDetails);

        if (details.Package.Source.Name == "winget")
        {
            details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                                          + details.Package.Id[0].ToString().ToLower() + "/"
                                          + details.Package.Id.Split('.')[0] + "/"
                                          + string.Join("/",
                                              details.Package.Id.Contains('.')
                                                  ? details.Package.Id.Split('.')[1..]
                                                  : details.Package.Id.Split('.'))
            );
        }
        else if (details.Package.Source.Name == "msstore")
        {
            details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + details.Package.Id);
        }

        // Find the native package for the given Package object
        PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(details.Package.Source.Name);
        if (Catalog == null)
        {
            logger.Error("Failed to get catalog " + details.Package.Source.Name + ". Is the package local?");
            logger.Close(1);
            return;
        }

        // Connect to catalog
        Catalog.AcceptSourceAgreements = true;
        ConnectResult ConnectResult = await Task.Run(() => Catalog.Connect());
        if (ConnectResult.Status != ConnectResultStatus.Ok)
        {
            logger.Error("Failed to connect to catalog " + details.Package.Source.Name);
            logger.Close(1);
            return;
        }

        // Match only the exact same Id
        FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
        PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
        filters.Field = PackageMatchField.Id;
        filters.Value = details.Package.Id;
        filters.Option = PackageFieldMatchOption.Equals;
        packageMatchFilter.Filters.Add(filters);
        packageMatchFilter.ResultLimit = 1;
        Task<FindPackagesResult> SearchResult =
            Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

        if (SearchResult.Result == null || SearchResult.Result.Matches == null ||
            SearchResult.Result.Matches.Count == 0)
        {
            logger.Error("WinGet: Failed to find package " + details.Package.Id + " in catalog " +
                         details.Package.Source.Name);
            logger.Close(1);
            return;
        }

        // Get the Native Package
        CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;

        // Extract data from NativeDetails
        CatalogPackageMetadata NativeDetails =
            NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata(Windows.System.UserProfile
                .GlobalizationPreferences.Languages[0]);

        if (NativeDetails.Author != "")
        {
            details.Author = NativeDetails.Author;
        }

        if (NativeDetails.Description != "")
        {
            details.Description = NativeDetails.Description;
        }

        if (NativeDetails.PackageUrl != "")
        {
            details.HomepageUrl = new Uri(NativeDetails.PackageUrl);
        }

        if (NativeDetails.License != "")
        {
            details.License = NativeDetails.License;
        }

        if (NativeDetails.LicenseUrl != "")
        {
            details.LicenseUrl = new Uri(NativeDetails.LicenseUrl);
        }

        if (NativeDetails.Publisher != "")
        {
            details.Publisher = NativeDetails.Publisher;
        }

        if (NativeDetails.ReleaseNotes != "")
        {
            details.ReleaseNotes = NativeDetails.ReleaseNotes;
        }

        if (NativeDetails.ReleaseNotesUrl != "")
        {
            details.ReleaseNotesUrl = new Uri(NativeDetails.ReleaseNotesUrl);
        }

        if (NativeDetails.Tags != null)
        {
            details.Tags = NativeDetails.Tags.ToArray();
        }

        // There is no way yet to retrieve installer URLs right now so this part will be console-parsed.
        // TODO: Replace this code with native code when available on the COM api
        Process process = new();
        List<string> output = [];
        ProcessStartInfo startInfo = new()
        {
            FileName = Manager.WinGetBundledPath,
            Arguments = Manager.Properties.ExecutableCallArgs + " show --id " + details.Package.Id +
                        " --exact --disable-interactivity --accept-source-agreements --source " +
                        details.Package.Source.Name,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };
        process.StartInfo = startInfo;
        process.Start();

        logger.Log("Begin loading installers:");
        logger.Log(" Executable: " + startInfo.FileName);
        logger.Log(" Arguments: " + startInfo.Arguments);

        // Retrieve the output
        string? _line;
        while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
        {
            if (_line.Trim() != "")
            {
                logger.Log(_line);
                output.Add(_line);
            }
        }

        logger.Error(await process.StandardError.ReadToEndAsync());

        // Parse the output
        foreach (string __line in output)
        {
            try
            {
                string line = __line.Trim();
                if (line.Contains("Installer SHA256:"))
                {
                    details.InstallerHash = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Installer Url:"))
                {
                    details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                    details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                }
                else if (line.Contains("Release Date:"))
                {
                    details.UpdateDate = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Installer Type:"))
                {
                    details.InstallerType = line.Split(":")[1].Trim();
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Error occurred while parsing line value=\"" + __line + "\"");
                Logger.Warn(e.Message);
            }
        }

        logger.Close(0);
        return;
    }
}
