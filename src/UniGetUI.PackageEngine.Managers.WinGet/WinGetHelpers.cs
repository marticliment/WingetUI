using Microsoft.Management.Deployment;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using WindowsPackageManager.Interop;
using Deployment = Microsoft.Management.Deployment;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal static class WinGetHelper
    {
        private static IWinGetPackageHelper? __helper;
        public static IWinGetPackageHelper Instance
        {
            get
            {
                if (__helper == null)
                {
                    __helper = new BundledWinGetHelper();
                }
                return __helper;
            }

            set
            {
                __helper = value;
            }
        }
    }

    internal interface IWinGetPackageHelper
    {

        public Task<IPackage[]> FindPackages_UnSafe(WinGet ManagerInstance, string query);
        public Task<IManagerSource[]> GetSources_UnSafe(WinGet ManagerInstance);
        public Task<string[]> GetPackageVersions_Unsafe(WinGet ManagerInstance, IPackage package);
        public Task GetPackageDetails_UnSafe(WinGet ManagerInstance, IPackageDetails details);

    }

    internal class NativeWinGetHelper : IWinGetPackageHelper
    {
        public WindowsPackageManagerStandardFactory Factory;
        public Deployment.PackageManager WinGetManager;

        public NativeWinGetHelper()
        {
            if (CoreTools.IsAdministrator())
                Logger.Info("Running elevated, WinGet class registration is likely to fail");
            Factory = new WindowsPackageManagerStandardFactory();
            WinGetManager = Factory.CreatePackageManager();
        }


        public async Task<IPackage[]> FindPackages_UnSafe(WinGet ManagerInstance, string query)
        {
            ManagerClasses.Classes.NativeTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.FindPackages);

            List<Package> Packages = new();
            FindPackagesOptions PackageFilters = Factory.CreateFindPackagesOptions();

            logger.Log("Generating filters...");
            // Name filter
            PackageMatchFilter FilterName = Factory.CreatePackageMatchFilter();
            FilterName.Field = Deployment.PackageMatchField.Name;
            FilterName.Value = query;
            FilterName.Option = Deployment.PackageFieldMatchOption.ContainsCaseInsensitive;
            PackageFilters.Filters.Add(FilterName);

            // Id filter
            PackageMatchFilter FilterId = Factory.CreatePackageMatchFilter();
            FilterId.Field = Deployment.PackageMatchField.Name;
            FilterId.Value = query;
            FilterId.Option = Deployment.PackageFieldMatchOption.ContainsCaseInsensitive;
            PackageFilters.Filters.Add(FilterId);

            // Load catalogs
            logger.Log("Loading available catalogs...");
            IReadOnlyList<PackageCatalogReference> AvailableCatalogs = WinGetManager.GetPackageCatalogs();
            Dictionary<Deployment.PackageCatalogReference, Task<Deployment.FindPackagesResult>> FindPackageTasks = new();

            // Spawn Tasks to find packages on catalogs
            logger.Log("Spawning catalog fetching tasks...");
            foreach (PackageCatalogReference CatalogReference in AvailableCatalogs.ToArray())
            {
                // Connect to catalog
                CatalogReference.AcceptSourceAgreements = true;
                ConnectResult result = await CatalogReference.ConnectAsync();
                if (result.Status == Deployment.ConnectResultStatus.Ok)
                {
                    try
                    {
                        // Create task and spawn it
                        Task<FindPackagesResult> task = new(() => result.PackageCatalog.FindPackages(PackageFilters));
                        task.Start();

                        // Add task to list
                        FindPackageTasks.Add(
                            CatalogReference,
                            task
                        );
                    }
                    catch (Exception e)
                    {
                        logger.Error("WinGet: Catalog " + CatalogReference.Info.Name + " failed to spawn FindPackages task.");
                        logger.Error(e);
                    }
                }
                else
                {
                    logger.Error("WinGet: Catalog " + CatalogReference.Info.Name + " failed to connect.");
                }
            }

            // Wait for tasks completion
            await Task.WhenAll(FindPackageTasks.Values.ToArray());
            logger.Log($"All catalogs fetched. Fetching results for query {query}");

            foreach (KeyValuePair<PackageCatalogReference, Task<FindPackagesResult>> CatalogTaskPair in FindPackageTasks)
            {
                try
                {
                    // Get the source for the catalog
                    IManagerSource source = ManagerInstance.GetSourceOrDefault(CatalogTaskPair.Key.Info.Name);

                    FindPackagesResult FoundPackages = CatalogTaskPair.Value.Result;
                    foreach (MatchResult package in FoundPackages.Matches.ToArray())
                    {
                        CatalogPackage catPkg = package.CatalogPackage;
                        // Create the Package item and add it to the list
                        logger.Log($"Found package: {catPkg.Name}|{catPkg.Name}|{catPkg.DefaultInstallVersion.Version} on catalog {source.Name}");
                        Packages.Add(new Package(
                            catPkg.Name,
                            catPkg.Id,
                            catPkg.DefaultInstallVersion.Version,
                            source,
                            ManagerInstance
                        ));
                    }
                }
                catch (Exception e)
                {
                    logger.Error("WinGet: Catalog " + CatalogTaskPair.Key.Info.Name + " failed to get available packages.");
                    logger.Error(e);
                }
            }
            logger.Close(0);
            return Packages.ToArray();
        }

        public async Task<IManagerSource[]> GetSources_UnSafe(WinGet ManagerInstance)
        {
            List<IManagerSource> sources = new();
            ManagerClasses.Classes.NativeTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.ListSources);

            foreach (PackageCatalogReference catalog in await Task.Run(() => WinGetManager.GetPackageCatalogs().ToArray()))
                try
                {
                    logger.Log($"Found source {catalog.Info.Name} with argument {catalog.Info.Argument}");
                    sources.Add(new ManagerSource(ManagerInstance, catalog.Info.Name, new Uri(catalog.Info.Argument), updateDate: catalog.Info.LastUpdateTime.ToString()));
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            logger.Close(0);
            return sources.ToArray();
        }

        public async Task<string[]> GetPackageVersions_Unsafe(WinGet ManagerInstance, IPackage package)
        {
            ManagerClasses.Classes.NativeTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions);

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
            if (ConnectResult.Status != Deployment.ConnectResultStatus.Ok)
            {
                logger.Error("Failed to connect to catalog " + package.Source.Name);
                logger.Close(1);
                return [];
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = Deployment.PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = Deployment.PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            Task<FindPackagesResult> SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                logger.Error("Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                logger.Close(1);
                return [];
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;
            string[] versions = NativePackage.AvailableVersions.Select(x => x.Version).ToArray();
            foreach (string? version in versions) logger.Log(version);
            logger.Close(0);
            return versions ?? [];
        }

        public async Task GetPackageDetails_UnSafe(WinGet ManagerInstance, IPackageDetails details)
        {
            ManagerClasses.Classes.NativeTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.LoadPackageDetails);

            if (details.Package.Source.Name == "winget")
                details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                    + details.Package.Id[0].ToString().ToLower() + "/"
                    + details.Package.Id.Split('.')[0] + "/"
                    + String.Join("/", (details.Package.Id.Contains('.') ? details.Package.Id.Split('.')[1..] : details.Package.Id.Split('.')))
                );
            else if (details.Package.Source.Name == "msstore")
                details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + details.Package.Id);

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
            if (ConnectResult.Status != Deployment.ConnectResultStatus.Ok)
            {
                logger.Error("Failed to connect to catalog " + details.Package.Source.Name);
                logger.Close(1);
                return;
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = Deployment.PackageMatchField.Id;
            filters.Value = details.Package.Id;
            filters.Option = Deployment.PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            Task<FindPackagesResult> SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                logger.Error("WinGet: Failed to find package " + details.Package.Id + " in catalog " + details.Package.Source.Name);
                logger.Close(1);
                return;
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;

            // Extract data from NativeDetails
            CatalogPackageMetadata NativeDetails = NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata(Windows.System.UserProfile.GlobalizationPreferences.Languages[0]);

            if (NativeDetails.Author != "")
                details.Author = NativeDetails.Author;

            if (NativeDetails.Description != "")
                details.Description = NativeDetails.Description;

            if (NativeDetails.PackageUrl != "")
                details.HomepageUrl = new Uri(NativeDetails.PackageUrl);

            if (NativeDetails.License != "")
                details.License = NativeDetails.License;

            if (NativeDetails.LicenseUrl != "")
                details.LicenseUrl = new Uri(NativeDetails.LicenseUrl);

            if (NativeDetails.Publisher != "")
                details.Publisher = NativeDetails.Publisher;

            if (NativeDetails.ReleaseNotes != "")
                details.ReleaseNotes = NativeDetails.ReleaseNotes;

            if (NativeDetails.ReleaseNotesUrl != "")
                details.ReleaseNotesUrl = new Uri(NativeDetails.ReleaseNotesUrl);

            if (NativeDetails.Tags != null)
                details.Tags = NativeDetails.Tags.ToArray();


            // There is no way yet to retrieve installer URLs right now so this part will be console-parsed.
            // TODO: Replace this code with native code when available on the COM api
            Process process = new();
            List<string> output = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Join(CoreData.UniGetUIExecutableDirectory, "winget-cli_x64", "winget.exe"),
                Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show --id " + details.Package.Id + " --exact --disable-interactivity --accept-source-agreements --source " + details.Package.Source.Name,
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
                if (_line.Trim() != "")
                {
                    logger.Log(_line);
                    output.Add(_line);
                }

            logger.Error(await process.StandardError.ReadToEndAsync());

            // Parse the output
            foreach (string __line in output)
            {
                try
                {
                    string line = __line.Trim();
                    if (line.Contains("Installer SHA256:"))
                        details.InstallerHash = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Url:"))
                    {
                        details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                        details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                    }
                    else if (line.Contains("Release Date:"))
                        details.UpdateDate = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Type:"))
                        details.InstallerType = line.Split(":")[1].Trim();
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


    internal class BundledWinGetHelper : IWinGetPackageHelper
    {

        private readonly string WinGetBundledPath;
        public BundledWinGetHelper()
        {
            WinGetBundledPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "PackageEngine", "Managers", "winget-cli_x64", "winget.exe");
        }

        public async Task<IPackage[]> FindPackages_UnSafe(WinGet ManagerInstance, string query)
        {
            List<Package> Packages = new();

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            ManagerClasses.Classes.ProcessTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

            string command = """
                Set-ExecutionPolicy Bypass -Scope Process -Force
                function Print-WinGetPackage {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Id,
                        [Parameter(ValueFromPipelineByPropertyName)] [string[]] $AvailableVersions,
                        [Parameter(ValueFromPipelineByPropertyName)] [string] $Source
                    )
                    process {
                        Write-Output(""#"" + $Name + ""`t"" + $Id + ""`t"" + $AvailableVersions[0] + ""`t"" + $Source)
                    }
                }

                Find-WinGetPackage -Query {query} | Print-WinGetPackage
                
                exit
                

                """;

            await p.StandardInput.WriteAsync(command);
            p.StandardInput.Close();
            logger.AddToStdIn(command);

            string? line;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!line.StartsWith("#"))
                    continue; // The PowerShell script appends a '#' to the beginning of each line to identify the output

                string[] elements = line.Split('\t');
                if (elements.Length < 4)
                    continue;

                IManagerSource source = ManagerInstance.GetSourceOrDefault(elements[3]);

                Packages.Add(new Package(elements[0][1..], elements[1], elements[2], source, ManagerInstance));
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();

        }

        public async Task GetPackageDetails_UnSafe(WinGet ManagerInstance, IPackageDetails details)
        {
            if (details.Package.Source.Name == "winget")
                details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                    + details.Package.Id[0].ToString().ToLower() + "/"
                    + details.Package.Id.Split('.')[0] + "/"
                    + String.Join("/", (details.Package.Id.Contains('.') ? details.Package.Id.Split('.')[1..] : details.Package.Id.Split('.')))
                );
            else if (details.Package.Source.Name == "msstore")
                details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + details.Package.Id);

            // Get the output for the best matching locale
            Process process = new();
            string packageIdentifier = "--id " + details.Package.Id + " --exact";

            List<string> output = new();
            bool LocaleFound = true;
            ProcessStartInfo startInfo = new()
            {
                FileName = WinGetBundledPath,
                Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements --locale " + System.Globalization.CultureInfo.CurrentCulture.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            string? _line;
            while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                if (_line.Trim() != "")
                {
                    output.Add(_line);
                    if (_line.Contains("The value provided for the `locale` argument is invalid") || _line.Contains("No applicable installer found; see Logger.Logs for more details."))
                    {
                        LocaleFound = false;
                        break;
                    }
                }

            // Load fallback english locale
            if (!LocaleFound)
            {
                output.Clear();
                Logger.Info("Winget could not found culture data for package Id=" + details.Package.Id + " and Culture=" + System.Globalization.CultureInfo.CurrentCulture.ToString() + ". Trying to get data for en-US");
                process = new Process();
                LocaleFound = true;
                startInfo = new()
                {
                    FileName = WinGetBundledPath,
                    Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements --locale en-US",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                process.StartInfo = startInfo;
                process.Start();

                while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                    if (_line.Trim() != "")
                    {
                        output.Add(_line);
                        if (_line.Contains("The value provided for the `locale` argument is invalid") || _line.Contains("No applicable installer found; see Logger.Logs for more details."))
                        {
                            LocaleFound = false;
                            break;
                        }
                    }
            }

            // Load default locale
            if (!LocaleFound)
            {
                output.Clear();
                Logger.Info("Winget could not found culture data for package Id=" + details.Package.Id + " and Culture=en-US. Loading default");
                LocaleFound = true;
                process = new Process();
                startInfo = new()
                {
                    FileName = WinGetBundledPath,
                    Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                process.StartInfo = startInfo;
                process.Start();

                while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                    if (_line.Trim() != "")
                    {
                        output.Add(_line);
                    }
            }

            // Parse the output
            bool IsLoadingDescription = false;
            bool IsLoadingReleaseNotes = false;
            bool IsLoadingTags = false;
            foreach (string __line in output)
            {
                try
                {
                    string line = __line.TrimEnd();
                    if (line == "")
                        continue;

                    // Check if a multiline field is being loaded
                    if (line.StartsWith(" ") && IsLoadingDescription)
                        details.Description += "\n" + line.Trim();
                    else if (line.StartsWith(" ") && IsLoadingReleaseNotes)
                        details.ReleaseNotes += "\n" + line.Trim();
                    else if (line.StartsWith(" ") && IsLoadingTags)
                        details.Tags = details.Tags.Append(line.Trim()).ToArray();

                    // Stop loading multiline fields
                    else if (IsLoadingDescription)
                        IsLoadingDescription = false;
                    else if (IsLoadingReleaseNotes)
                        IsLoadingReleaseNotes = false;
                    else if (IsLoadingTags)
                        IsLoadingTags = false;

                    // Check for singleline fields
                    if (line.Contains("Publisher:"))
                        details.Publisher = line.Split(":")[1].Trim();

                    else if (line.Contains("Author:"))
                        details.Author = line.Split(":")[1].Trim();

                    else if (line.Contains("Homepage:"))
                        details.HomepageUrl = new Uri(line.Replace("Homepage:", "").Trim());

                    else if (line.Contains("License:"))
                        details.License = line.Split(":")[1].Trim();

                    else if (line.Contains("License Url:"))
                        details.LicenseUrl = new Uri(line.Replace("License Url:", "").Trim());

                    else if (line.Contains("Installer SHA256:"))
                        details.InstallerHash = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Url:"))
                    {
                        details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                        details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                    }
                    else if (line.Contains("Release Date:"))
                        details.UpdateDate = line.Split(":")[1].Trim();

                    else if (line.Contains("Release Notes Url:"))
                        details.ReleaseNotesUrl = new Uri(line.Replace("Release Notes Url:", "").Trim());

                    else if (line.Contains("Installer Type:"))
                        details.InstallerType = line.Split(":")[1].Trim();

                    else if (line.Contains("Description:"))
                    {
                        details.Description = line.Split(":")[1].Trim();
                        IsLoadingDescription = true;
                    }
                    else if (line.Contains("ReleaseNotes"))
                    {
                        details.ReleaseNotes = line.Split(":")[1].Trim();
                        IsLoadingReleaseNotes = true;
                    }
                    else if (line.Contains("Tags"))
                    {
                        details.Tags = new string[0];
                        IsLoadingTags = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn("Error occurred while parsing line value=\"" + _line + "\"");
                    Logger.Warn(e.Message);
                }
            }

            return;
        }

        public async Task<string[]> GetPackageVersions_Unsafe(WinGet ManagerInstance, IPackage package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = WinGetBundledPath,
                    Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show --id " + package.Id + " --exact --versions --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions, p);

            p.Start();

            string? line;
            List<string> versions = new();
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("---"))
                        DashesPassed = true;
                }
                else
                    versions.Add(line.Trim());
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return versions.ToArray();
        }

        public async Task<IManagerSource[]> GetSources_UnSafe(WinGet ManagerInstance)
        {
            List<ManagerSource> sources = new();

            Process p = new();
            p.StartInfo = new()
            {
                FileName = ManagerInstance.Status.ExecutablePath,
                Arguments = ManagerInstance.Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            ManagerClasses.Classes.ProcessTaskLogger logger = ManagerInstance.TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

            bool dashesPassed = false;
            string? line;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                try
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!dashesPassed)
                    {
                        if (line.Contains("---"))
                            dashesPassed = true;
                    }
                    else
                    {
                        string[] parts = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                        if (parts.Length > 1)
                            sources.Add(new ManagerSource(ManagerInstance, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return sources.ToArray();

        }
    }
}


