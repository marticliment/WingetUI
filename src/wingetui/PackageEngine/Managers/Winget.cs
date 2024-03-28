using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace UniGetUI.PackageEngine.Managers
{
    public class Winget : PackageManagerWithSources
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "", "e(s)", "have", "the", "Id" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "", "e(s)", "have", "an", "'winget", "pin'", "have", "an", "Version" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "have", "an", "'winget", "pin'", "have", "an", "Version" };
        private static LocalPcSource LocalPcSource { get; } = new LocalPcSource();
        private static AndroidSubsystemSource AndroidSubsystemSource { get; } = new AndroidSubsystemSource();
        private static SteamSource SteamSource { get; } = new SteamSource();
        private static UbisoftConnectSource UbisoftConnectSource { get; } = new UbisoftConnectSource();
        private static GOGSource GOGSource { get; } = new GOGSource();
        private static MicrosoftStoreSource MicrosoftStoreSource { get; } = new MicrosoftStoreSource();

        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = new();
            Process p = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search \"" + query + "\"  --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            p.StartInfo = startInfo;
            p.Start();

            string OldLine = "";
            int IdIndex = -1;
            int VersionIndex = -1;
            int SourceIndex = -1;
            bool DashesPassed = false;
            string line;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed && line.Contains("---"))
                {
                    string HeaderPrefix = OldLine.Contains("SearchId") ? "Search" : "";
                    IdIndex = OldLine.IndexOf(HeaderPrefix + "Id");
                    VersionIndex = OldLine.IndexOf(HeaderPrefix + "Version");
                    SourceIndex = OldLine.IndexOf(HeaderPrefix + "Source");
                    DashesPassed = true;
                }
                else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && IdIndex < VersionIndex && VersionIndex < line.Length)
                {
                    int offset = 0; // Account for non-unicode character length
                    while (line[IdIndex - offset - 1] != ' ' || offset > (IdIndex - 5))
                        offset++;
                    string name = line[..(IdIndex - offset)].Trim();
                    string id = line[(IdIndex - offset)..].Trim().Split(' ')[0];
                    string version = line[(VersionIndex - offset)..].Trim().Split(' ')[0];
                    ManagerSource source;
                    if (SourceIndex == -1 || SourceIndex >= line.Length)
                        source = MainSource;
                    else
                    {
                        string sourceName = line[(SourceIndex - offset)..].Trim().Split(' ')[0];
                        if (SourceReference.ContainsKey(sourceName))
                            source = SourceReference[sourceName];
                        else
                        {
                            source = new ManagerSource(this, sourceName, new Uri("https://microsoft.com/winget"));
                            SourceReference.Add(source.Name, source);
                        }
                    }
                    Packages.Add(new Package(name, id, version, source, this));
                }
                OldLine = line;
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await Task.Run(p.WaitForExit);

            return Packages.ToArray();

        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            List<UpgradablePackage> Packages = new();
            Process p = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " update --include-unknown  --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            p.StartInfo = startInfo;
            p.Start();

            string OldLine = "";
            int IdIndex = -1;
            int VersionIndex = -1;
            int NewVersionIndex = -1;
            int SourceIndex = -1;
            bool DashesPassed = false;
            string line;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";

                if (line.Contains("have pins"))
                    continue;
                
                if (!DashesPassed && line.Contains("---"))
                {
                    string HeaderPrefix = OldLine.Contains("SearchId") ? "Search" : "";
                    string HeaderSuffix = OldLine.Contains("SearchId") ? "Header" : "";
                    IdIndex = OldLine.IndexOf(HeaderPrefix + "Id");
                    VersionIndex = OldLine.IndexOf(HeaderPrefix + "Version");
                    NewVersionIndex = OldLine.IndexOf("Available" + HeaderSuffix);
                    SourceIndex = OldLine.IndexOf(HeaderPrefix + "Source");
                    DashesPassed = true;
                }
                else if (line.Trim() == "")
                {
                    DashesPassed = false;
                }
                else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && NewVersionIndex > 0 && IdIndex < VersionIndex && VersionIndex < NewVersionIndex && NewVersionIndex < line.Length)
                {
                    int offset = 0; // Account for non-unicode character length
                    while (line[IdIndex - offset - 1] != ' ' || offset > (IdIndex - 5))
                        offset++;
                    string name = line[..(IdIndex - offset)].Trim();
                    string id = line[(IdIndex - offset)..].Trim().Split(' ')[0];
                    string version = line[(VersionIndex - offset)..(NewVersionIndex - offset)].Trim();
                    string newVersion;
                    if (SourceIndex != -1)
                        newVersion = line[(NewVersionIndex - offset)..(SourceIndex - offset)].Trim();
                    else
                        newVersion = line[(NewVersionIndex - offset)..].Trim().Split(' ')[0];

                    ManagerSource source;
                    if (SourceIndex == -1 || SourceIndex >= line.Length)
                        source = MainSource;
                    else
                    {
                        string sourceName = line[(SourceIndex - offset)..].Trim().Split(' ')[0];
                        if (SourceReference.ContainsKey(sourceName))
                            source = SourceReference[sourceName];
                        else
                        {
                            source = new ManagerSource(this, sourceName, new Uri("https://microsoft.com/winget"));
                            SourceReference.Add(source.Name, source);
                        }
                    }

                    Packages.Add(new UpgradablePackage(name, id, version, newVersion, source, this));
                }
                OldLine = line;
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await Task.Run(p.WaitForExit);

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            List<Package> Packages = new();
            Process p = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list  --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            p.StartInfo = startInfo;
            p.Start();

            string OldLine = "";
            int IdIndex = -1;
            int VersionIndex = -1;
            int SourceIndex = -1;
            int NewVersionIndex = -1;
            bool DashesPassed = false;
            string line;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                try
                {
                    output += line + "\n";
                    if (!DashesPassed && line.Contains("---"))
                    {
                        string HeaderPrefix = OldLine.Contains("SearchId") ? "Search" : "";
                        string HeaderSuffix = OldLine.Contains("SearchId") ? "Header" : "";
                        IdIndex = OldLine.IndexOf(HeaderPrefix + "Id");
                        VersionIndex = OldLine.IndexOf(HeaderPrefix + "Version");
                        NewVersionIndex = OldLine.IndexOf("Available" + HeaderSuffix);
                        SourceIndex = OldLine.IndexOf(HeaderPrefix + "Source");
                        DashesPassed = true;
                    }
                    else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && IdIndex < VersionIndex && VersionIndex < line.Length)
                    {
                        int offset = 0; // Account for non-unicode character length
                        while (((IdIndex - offset) <= line.Length && line[IdIndex - offset - 1] != ' ') || offset > (IdIndex - 5))
                            offset++;
                        string name = line[..(IdIndex - offset)].Trim();
                        string id = line[(IdIndex - offset)..].Trim().Split(' ')[0];
                        if (NewVersionIndex == -1 && SourceIndex != -1) NewVersionIndex = SourceIndex;
                        else if (NewVersionIndex == -1 && SourceIndex == -1) NewVersionIndex = line.Length - 1;
                        string version = line[(VersionIndex - offset)..(NewVersionIndex - offset)].Trim();

                        ManagerSource source;
                        if (SourceIndex == -1 || (SourceIndex - offset) >= line.Length)
                        {
                            source = GetLocalSource(id); // Load Winget Local Sources
                        }
                        else
                        {
                            string sourceName = line[(SourceIndex - offset)..].Trim().Split(' ')[0].Trim();
                            if (SourceReference.ContainsKey(sourceName))
                                source = SourceReference[sourceName];
                            else
                            {
                                source = new ManagerSource(this, sourceName, new Uri("https://microsoft.com/winget"));
                                SourceReference.Add(source.Name, source);
                            }
                        }
                        Packages.Add(new Package(name, id, version, source, this));
                    }
                    OldLine = line;
                }
                catch (Exception e)
                {
                    AppTools.Log(e);
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await Task.Run(p.WaitForExit);

            return Packages.ToArray();
        }

        private ManagerSource GetLocalSource(string id)
        {
            try
            {
                // Check if source is android
                bool AndroidValid = true;
                foreach (char c in id)
                    if (!"abcdefghijklmnopqrstuvwxyz.…".Contains(c))
                    {
                        AndroidValid = false;
                        break;
                    }
                if (AndroidValid && id.Count(x => x == '.') >= 2)
                    return AndroidSubsystemSource;

                // Check if source is Steama
                if ((id == "Steam" || id.Contains("Steam App ")) && id.Split("Steam App").Count() >= 2 && id.Split("Steam App")[1].Trim().Count(x => !"1234567890".Contains(x)) == 0)
                    return SteamSource;

                // Check if source is Ubisoft Connect
                if (id == "Uplay" || id.Contains("Uplay Install ") && id.Split("Uplay Install").Count() >= 2 && id.Split("Uplay Install")[1].Trim().Count(x => !"1234567890".Contains(x)) == 0)
                    return UbisoftConnectSource;

                // Check if source is GOG
                if (id.EndsWith("_is1") && id.Split("_is1")[0].Count(x => !"1234567890".Contains(x)) == 0)
                    return GOGSource;

                // Check if source is Microsoft Store
                if (id.Count(x => x == '_') == 1 && (id.Split('_')[^1].Length == 14 | id.Split('_')[^1].Length == 13 | id.Split('_')[^1].Length <= 13 && id[^1] == '…'))
                    return MicrosoftStoreSource;

                return LocalPcSource;
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
                return LocalPcSource;
            }
        }

        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            parameters.Add("--accept-package-agreements");

            if (options.SkipHashCheck)
                parameters.Add("--ignore-security-hash");

            if (options.CustomInstallLocation != "")
            {
                parameters.Add("--location"); parameters.Add("\"" + options.CustomInstallLocation + "\"");
            }

            switch (options.Architecture)
            {
                case (null):
                    break;
                case (Architecture.X86):
                    parameters.Add("--architecture"); parameters.Add("x86");
                    break;
                case (Architecture.X64):
                    parameters.Add("--architecture"); parameters.Add("x64");
                    break;
                case (Architecture.Arm64):
                    parameters.Add("--architecture"); parameters.Add("arm64");
                    break;
            }
            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            if (package.Name.Contains("64-bit") || package.Id.ToLower().Contains("x64"))
                options.Architecture = Architecture.X64;
            else if (package.Name.Contains("32-bit") || package.Id.ToLower().Contains("x86"))
                options.Architecture = Architecture.X86;

            string[] parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            List<string> p = parameters.ToList();
            p.Add("--force");
            p.Add("--include-unknown");
            parameters = p.ToArray();
            return parameters;
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb };
            if (!package.Id.Contains("…"))
                parameters.AddRange(new string[] { "--id", package.Id, "--exact" });
            else if (!package.Name.Contains("…"))
                parameters.AddRange(new string[] { "--name", "\"" + package.Name + "\"", "--exact" });
            else
                parameters.AddRange(new string[] { "--id", package.Id.Replace("…", "") });

            parameters.Add("--accept-source-agreements");

            switch (options.InstallationScope)
            {
                case (PackageScope.Local):
                    parameters.Add("--scope"); parameters.Add("user");
                    break;
                case (PackageScope.Global):
                    parameters.Add("--scope"); parameters.Add("machine");
                    break;
            }

            if (options.Version != "")
                parameters.AddRange(new string[] { "--version", options.Version, "--force" });

            if (options.InteractiveInstallation)
                parameters.Add("--interactive");
            else
                parameters.AddRange(new string[] { "--silent", "--disable-interactivity" });

            return parameters.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == -1978334967) // Use https://www.rapidtables.com/convert/number/hex-to-decimal.html for easy UInt(hex) to Int(dec) conversion
                return OperationVeredict.Succeeded; // TODO: Needs restart
            else if (ReturnCode == -1978335215)
                return OperationVeredict.Failed; // TODO: Needs skip checksum

            if (output_string.Contains("No applicable upgrade found") || output_string.Contains("No newer package versions are available from the configured sources"))
                return OperationVeredict.Succeeded;

            if(output_string.Contains("winget settings --enable InstallerHashOverride"))
            {
                AppTools.Log("Enabling skip hash ckeck for winget...");
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = CoreData.GSudoPath,
                        Arguments = Status.ExecutablePath + " " + Properties.ExecutableCallArgs + " settings --enable InstallerHashOverride",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };
                p.Start();
                p.WaitForExit();
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (output_string.Contains("1603") || output_string.Contains("0x80070005") || output_string.Contains("Access is denied"))
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }


        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache"));
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);

            if (package.Source.Name == "winget")
                details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                    + package.Id[0].ToString().ToLower() + "/"
                    + package.Id.Split('.')[0] + "/"
                    + String.Join("/", (package.Id.Contains('.') ? package.Id.Split('.')[1..] : package.Id.Split('.')))
                );
            else if (package.Source.Name == "msstore")
                details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + package.Id);

            // Get the output for the best matching locale
            Process process = new();
            string packageIdentifier;
            if (!package.Id.Contains("…"))
                packageIdentifier = "--id " + package.Id + " --exact";
            else if (!package.Name.Contains("…"))
                packageIdentifier = "--name " + package.Id + " --exact";
            else
                packageIdentifier = "--id " + package.Id;

            List<string> output = new();
            bool LocaleFound = true;
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements --locale " + System.Globalization.CultureInfo.CurrentCulture.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            string _line;
            while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                if (_line.Trim() != "")
                {
                    output.Add(_line);
                    AppTools.Log(_line);
                    if (_line.Contains("The value provided for the `locale` argument is invalid") || _line.Contains("No applicable installer found; see logs for more details."))
                    {
                        LocaleFound = false;
                        break;
                    }
                }

            // Load fallback english locale
            if (!LocaleFound)
            {
                output.Clear();
                AppTools.Log("Winget could not found culture data for package Id=" + package.Id + " and Culture=" + System.Globalization.CultureInfo.CurrentCulture.ToString() + ". Trying to get data for en-US");
                process = new Process();
                LocaleFound = true;
                startInfo = new()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements --locale en-US",
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
                        AppTools.Log(_line);
                        if (_line.Contains("The value provided for the `locale` argument is invalid") || _line.Contains("No applicable installer found; see logs for more details."))
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
                AppTools.Log("Winget could not found culture data for package Id=" + package.Id + " and Culture=en-US. Loading default");
                LocaleFound = true;
                process = new Process();
                startInfo = new()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements",
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
                        AppTools.Log(_line);
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
                        details.InstallerSize = await Tools.GetFileSizeAsync(details.InstallerUrl);
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
                    AppTools.Log("Error occurred while parsing line value=\"" + _line + "\"");
                    AppTools.Log(e.Message);
                }
            }

            return details;
        }

        protected override async Task<ManagerSource[]> GetSources_UnSafe()
        {
            List<ManagerSource> sources = new();

            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            process.StartInfo = startInfo;
            process.Start();

            bool dashesPassed = false;
            string output = "";
            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
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
                        sources.Add(new ManagerSource(this, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                }
                catch (Exception e)
                {
                    AppTools.Log(e);
                }
            }

            output += await process.StandardError.ReadToEndAsync(); 
            AppTools.LogManagerOperation(this, process, output);

            await process.WaitForExitAsync();
            return sources.ToArray();
        }


        public override async Task RefreshSources()
        {
            Process process = new();
            ProcessStartInfo StartInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " source update",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = StartInfo;
            process.Start();

            int StartTime = Environment.TickCount;

            while (!process.HasExited && Environment.TickCount - StartTime < 8000)
                await Task.Delay(100);

            if (!process.HasExited)
            {
                process.Kill();
                AppTools.Log("Winget source update timed out. Current output was");
                AppTools.Log(await process.StandardOutput.ReadToEndAsync());
            }
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64 },
                SupportsCustomScopes = true,
                SupportsCustomLocations = true,
                SupportsCustomSources = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                    MustBeInstalledAsAdmin = true,
                }
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new()
            {
                Name = "Winget",
                Description = Tools.Translate("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"),
                IconId = "winget",
                ColorIconId = "winget_color",
                ExecutableFriendlyName = "winget.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();
            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C echo %PROCESSOR_ARCHITECTURE%",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();

            if (Tools.GetSettings("UseSystemWinget"))
                status.ExecutablePath = await Tools.Which("winget.exe");
            else if (output.Contains("ARM64") | Tools.GetSettings("EnableArmWinget"))
                status.ExecutablePath = Path.Join(CoreData.WingetUIExecutableDirectory, "PackageEngine", "Managers", "winget-cli_arm64", "winget.exe");
            else
                status.ExecutablePath = Path.Join(CoreData.WingetUIExecutableDirectory, "PackageEngine", "Managers", "winget-cli_x64", "winget.exe");

            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
                return status;

            process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();

            Status = status; // Need to set status before calling RefreshSources, otherwise will crash
            if (status.Found && IsEnabled())
                await RefreshSources();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            string callId = "";
            if (!package.Id.Contains("…"))
                callId = "--id " + package.Id + " --exact";
            else if (!package.Name.Contains("…"))
                callId = "--name \"" + package.Name + "\" --exact";
            else
                callId = "--id " + package.Id;


            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " show " + callId + " --versions --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            p.Start();

            string line;
            List<string> versions = new();
            bool DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("---"))
                        DashesPassed = true;
                }
                else
                    versions.Add(line.Trim());
            }
            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            return versions.ToArray();
        }

        public override ManagerSource[] GetKnownSources()
        {
            return new ManagerSource[]
            {
                new(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache")),
                new(this, "msstore", new Uri("https://storeedgefd.dsx.mp.microsoft.com/v9.0")),
            };
        }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "add", "--name", source.Name, "--arg", source.Url.ToString(), "--accept-source-agreements", "--disable-interactivity" };
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "remove", "--name", source.Name, "--disable-interactivity" };
        }

        public override OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
    }

    public class LocalPcSource : ManagerSource
    {
        public override string IconId { get { return "localpc"; } }
        public LocalPcSource() : base(Winget.Tools.App.Winget, Winget.Tools.Translate("Local PC"), new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return Winget.Tools.Translate("Local PC");
        }
    }

    public class AndroidSubsystemSource : ManagerSource
    {
        public override string IconId { get { return "android"; } }
        public AndroidSubsystemSource() : base(Winget.Tools.App.Winget, Winget.Tools.Translate("Android Subsystem"), new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return Winget.Tools.Translate("Android Subsystem");
        }
    }

    public class SteamSource : ManagerSource
    {
        public override string IconId { get { return "steam"; } }
        public SteamSource() : base(Winget.Tools.App.Winget, "Steam", new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "Steam";
        }
    }

    public class UbisoftConnectSource : ManagerSource
    {
        public override string IconId { get { return "uplay"; } }
        public UbisoftConnectSource() : base(Winget.Tools.App.Winget, "Ubisoft Connect", new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "Ubisoft Connect";
        }
    }

    public class GOGSource : ManagerSource
    {
        public override string IconId { get { return "gog"; } }
        public GOGSource() : base(Winget.Tools.App.Winget, "GOG", new Uri("https://microsoft.com/gog-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "GOG";
        }
    }

    public class MicrosoftStoreSource : ManagerSource
    {
        public override string IconId { get { return "msstore"; } }
        public MicrosoftStoreSource() : base(Winget.Tools.App.Winget, "Microsoft Store", new Uri("https://microsoft.com/microsoft-store-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "Microsoft Store";
        }
    }

}
