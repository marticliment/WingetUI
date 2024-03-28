using H.NotifyIcon.Core;
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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Managers;

public class Scoop : PackageManagerWithSources
{
    new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
    new public static string[] FALSE_PACKAGE_IDS = new string[] { "No" };
    new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "Matches" };
    protected override async Task<Package[]> FindPackages_UnSafe(string query)
    {
        List<Package> Packages = new();

        string path = await Tools.Which("scoop-search.exe");
        if (!File.Exists(path))
        {
            Process proc = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    Arguments = Properties.ExecutableCallArgs + " install main/scoop-search",
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            await proc.WaitForExitAsync();
            path = "scoop-search.exe";
        }

        Process p = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = path,
                Arguments = "\"" + query + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        p.Start();

        string line;
        ManagerSource source = GetMainSource();
        string output = "";
        while ((line = await p.StandardOutput.ReadLineAsync()) != null)
        {
            output += line + "\n";
            if (line.StartsWith("'"))
            {
                string sourceName = line.Split(" ")[0].Replace("'", "");
                if (SourceReference.ContainsKey(sourceName))
                    source = SourceReference[sourceName];
                else
                {
                    AppTools.Log("Unknown source!");
                    source = new ManagerSource(this, sourceName, new Uri("https://scoop.sh/"), 0, "Unknown");
                    SourceReference.Add(sourceName, source);
                }
            }
            else if (line.Trim() != "")
            {
                string[] elements = line.Trim().Split(" ");
                if (elements.Length < 2)
                    continue;

                for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    continue;

                Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1].Replace("(", "").Replace(")", ""), source, this));
            }
        }
        output += await p.StandardError.ReadToEndAsync();
        AppTools.LogManagerOperation(this, p, output);
        return Packages.ToArray();
    }

    protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
    {
        Dictionary<string, Package> InstalledPackages = new();
        foreach (Package InstalledPackage in await GetInstalledPackages())
        {
            if (!InstalledPackages.ContainsKey(InstalledPackage.Id + "." + InstalledPackage.Version))
                InstalledPackages.Add(InstalledPackage.Id + "." + InstalledPackage.Version, InstalledPackage);
        }

        List<UpgradablePackage> Packages = new();

        Process p = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        p.Start();

        string line;
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
            else if (line.Trim() != "")
            {
                string[] elements = Regex.Replace(line, " {2,}", " ").Trim().Split(" ");
                if (elements.Length < 3)
                    continue;

                for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    continue;

                if (!InstalledPackages.ContainsKey(elements[0] + "." + elements[1]))
                {
                    AppTools.Log("Upgradable scoop package not listed on installed packages - id=" + elements[0]);
                    continue;
                }

                Packages.Add(new UpgradablePackage(Tools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], InstalledPackages[elements[0] + "." + elements[1]].Source, this, InstalledPackages[elements[0] + "." + elements[1]].Scope));
            }
        }
        output += await p.StandardError.ReadToEndAsync();
        AppTools.LogManagerOperation(this, p, output);
        return Packages.ToArray();
    }

    protected override async Task<Package[]> GetInstalledPackages_UnSafe()
    {
        List<Package> Packages = new();

        Process p = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        p.Start();

        string line;
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
            else if (line.Trim() != "")
            {
                string[] elements = Regex.Replace(line, " {2,}", " ").Trim().Split(" ");
                if (elements.Length < 3)
                    continue;

                for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    continue;

                ManagerSource source;
                string sourceName = elements[2];
                if (SourceReference.ContainsKey(sourceName))
                    source = SourceReference[sourceName];
                else
                {
                    AppTools.Log("Unknown source!");
                    source = new ManagerSource(this, sourceName, new Uri("https://scoop.sh/"), 0, "Unknown");
                    SourceReference.Add(sourceName, source);
                }

                PackageScope scope = PackageScope.User;
                if (line.Contains("Global install"))
                    scope = PackageScope.Global;

                Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1], source, this, scope));
            }
        }
        output += await p.StandardError.ReadToEndAsync();
        AppTools.LogManagerOperation(this, p, output);
        return Packages.ToArray();
    }

    public override ManagerSource GetMainSource()
    {
        return new ManagerSource(this, "main", new Uri("https://github.com/ScoopInstaller/Main"), 0, "");
    }

    public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
    {
        PackageDetails details = new(package);

        if (package.Source.Url != null)
            try
            {
                details.ManifestUrl = new Uri(package.Source.Url.ToString() + "/blob/master/bucket/" + package.Id + ".json");
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }

        Process p = new();
        p.StartInfo = new ProcessStartInfo()
        {
            FileName = Status.ExecutablePath,
            Arguments = Properties.ExecutableCallArgs + " cat " + package.Source.Name + "/" + package.Id,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };

        p.Start();
        string JsonString = await p.StandardOutput.ReadToEndAsync();

        JsonObject RawInfo = JsonObject.Parse(JsonString) as JsonObject;

        try
        {
            if (RawInfo.ContainsKey("description") && (RawInfo["description"] is JsonArray))
            {
                details.Description = "";
                foreach (JsonNode note in RawInfo["description"] as JsonArray)
                    details.Description += note.ToString() + "\n";
                details.Description = details.Description.Replace("\n\n", "\n").Trim();
            }
            else if (RawInfo.ContainsKey("description"))
                details.Description = RawInfo["description"].ToString();
        }
        catch (Exception ex) { AppTools.Log("Can't load description: " + ex); }

        try
        {
            if (RawInfo.ContainsKey("innosetup"))
                details.InstallerType = "Inno Setup (" + Tools.Translate("extracted") + ")";
            else
                details.InstallerType = Tools.Translate("Scoop package");
        }
        catch (Exception ex) { AppTools.Log("Can't load installer type: " + ex); }

        try
        {
            if (RawInfo.ContainsKey("homepage"))
            {
                details.HomepageUrl = new Uri(RawInfo["homepage"].ToString());
                if (details.HomepageUrl.ToString().Contains("https://github.com/"))
                    details.Author = details.HomepageUrl.ToString().Replace("https://github.com/", "").Split("/")[0];
                else
                    details.Author = details.HomepageUrl.Host.Split(".")[^2];
            }
        }
        catch (Exception ex) { AppTools.Log("Can't load homepage: " + ex); }

        try
        {
            if (RawInfo.ContainsKey("notes") && (RawInfo["notes"] is JsonArray))
            {
                details.ReleaseNotes = "";
                foreach (JsonNode note in RawInfo["notes"] as JsonArray)
                    details.ReleaseNotes += note.ToString() + "\n";
                details.ReleaseNotes = details.ReleaseNotes.Replace("\n\n", "\n").Trim();
            }
            else if (RawInfo.ContainsKey("notes"))
                details.ReleaseNotes = RawInfo["notes"].ToString();
        }
        catch (Exception ex) { AppTools.Log("Can't load notes: " + ex); }

        try
        {
            if (RawInfo.ContainsKey("license"))
            {
                if (RawInfo["license"] is not JsonValue)
                {
                    details.License = RawInfo["license"]["identifier"].ToString();
                    details.LicenseUrl = new Uri(RawInfo["license"]["url"].ToString());
                }
                else
                    details.License = RawInfo["license"].ToString();
            }
        }
        catch (Exception ex) { AppTools.Log("Can't load license: " + ex); }

        try
        {
            if (RawInfo.ContainsKey("url") && RawInfo.ContainsKey("hash"))
            {
                if (RawInfo["url"] is JsonArray)
                    details.InstallerUrl = new Uri(RawInfo["url"][0].ToString());
                else
                    details.InstallerUrl = new Uri(RawInfo["url"].ToString());

                if (RawInfo["hash"] is JsonArray)
                    details.InstallerHash = RawInfo["hash"][0].ToString();
                else
                    details.InstallerHash = RawInfo["hash"].ToString();
            }
            else if (RawInfo.ContainsKey("architecture"))
            {
                string FirstArch = (RawInfo["architecture"] as JsonObject).ElementAt(0).Key;
                details.InstallerHash = RawInfo["architecture"][FirstArch]["hash"].ToString();
                details.InstallerUrl = new Uri(RawInfo["architecture"][FirstArch]["url"].ToString());
            }

            details.InstallerSize = await Tools.GetFileSizeAsync(details.InstallerUrl);
        }
        catch (Exception ex) { AppTools.Log("Can't load installer URL: " + ex); }

        try
        {
            if (RawInfo.ContainsKey("checkver") && RawInfo["checkver"] is JsonObject && (RawInfo["checkver"] as JsonObject).ContainsKey("url"))
                details.ReleaseNotesUrl = new Uri(RawInfo["checkver"]["url"].ToString());
        }
        catch (Exception ex) { AppTools.Log("Can't load notes URL: " + ex); }

        return details;

    }

    protected override async Task<ManagerSource[]> GetSources_UnSafe()
    {
        using (Process process = new())
        {
            process.StartInfo.FileName = Status.ExecutablePath;
            process.StartInfo.Arguments = Properties.ExecutableCallArgs + " bucket list";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

            List<ManagerSource> sources = new();

            process.Start();

            var _output = "";
            bool DashesPassed = false;

            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                _output += line + "\n";
                try
                {
                    if (!DashesPassed)
                    {
                        if (line.Contains("---"))
                            DashesPassed = true;
                    }
                    else if (line.Trim() != "")
                    {
                        string[] elements = Regex.Replace(line.Replace("AM", "").Replace("PM", "").Trim(), " {2,}", " ").Split(' ');
                        if (elements.Length >= 5)
                        {
                            if (!elements[1].Contains("https://"))
                                elements[1] = "https://scoop.sh/"; // If the URI is invalid, we'll use the main website
                            sources.Add(new ManagerSource(this, elements[0].Trim(), new Uri(elements[1].Trim()), int.Parse(elements[4].Trim()), elements[2].Trim() + " " + elements[3].Trim()));
                        }
                    }
                }
                catch (Exception e)
                {
                    AppTools.Log(e);
                }
            }
            _output += await process.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, process, _output);

            await process.WaitForExitAsync();


            return sources.ToArray();
        }
    }

    public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
    {
        string output_string = string.Join("\n", Output);
        if ((output_string.Contains("Try again with the --global (or -g) flag instead") && package.Scope == PackageScope.Local))
        {
            package.Scope = PackageScope.Global;
            return OperationVeredict.AutoRetry;
        }
        if (output_string.Contains("requires admin rights") || output_string.Contains("requires administrator rights") || output_string.Contains("you need admin rights to install global apps"))
        {
            options.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }
        if (output_string.Contains("was uninstalled"))
            return OperationVeredict.Succeeded;
        return OperationVeredict.Failed;
    }
    public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
    {
        string output_string = string.Join("\n", Output);
        if ((output_string.Contains("Try again with the --global (or -g) flag instead") && package.Scope == PackageScope.Local))
        {
            package.Scope = PackageScope.Global;
            return OperationVeredict.AutoRetry;
        }
        if (output_string.Contains("requires admin rights") || output_string.Contains("requires administrator rights") || output_string.Contains("you need admin rights to install global apps"))
        {
            options.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }
        if (output_string.Contains("ERROR"))
            return OperationVeredict.Failed;
        return OperationVeredict.Succeeded;
    }
    public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
    {
        return GetInstallOperationVeredict(package, options, ReturnCode, Output);
    }

    public override string[] GetUninstallParameters(Package package, InstallationOptions options)
    {
        List<string> parameters = new();

        parameters.Add(Properties.UninstallVerb);
        parameters.Add(package.Source.Name + "/" + package.Id);

        if (package.Scope == PackageScope.Global)
            parameters.Add("--global");

        if (options.CustomParameters != null)
            parameters.AddRange(options.CustomParameters);

        if (options.RemoveDataOnUninstall)
            parameters.Add("--purge");

        return parameters.ToArray();
    }
    public override string[] GetInstallParameters(Package package, InstallationOptions options)
    {
        string[] parameters = GetUpdateParameters(package, options);
        parameters[0] = Properties.InstallVerb;
        return parameters;
    }
    public override string[] GetUpdateParameters(Package package, InstallationOptions options)
    {
        List<string> parameters = GetUninstallParameters(package, options).ToList();
        parameters[0] = Properties.UpdateVerb;

        parameters.Remove("--purge");

        switch (options.Architecture)
        {
            case null:
                break;
            case Architecture.X64:
                parameters.Add("--arch");
                parameters.Add("64bit");
                break;
            case Architecture.X86:
                parameters.Add("--arch");
                parameters.Add("32bit");
                break;
            case Architecture.Arm64:
                parameters.Add("--arch");
                parameters.Add("arm64");
                break;
        }

        if (options.SkipHashCheck)
        {
            parameters.Add("--skip");
        }

        return parameters.ToArray();
    }

    public override async Task RefreshSources()
    {
        Process process = new();
        ProcessStartInfo StartInfo = new()
        {
            FileName = Properties.ExecutableFriendlyName,
            Arguments = Properties.ExecutableCallArgs + " update"
        };
        process.StartInfo = StartInfo;
        process.Start();
        await process.WaitForExitAsync();
    }

    protected override ManagerCapabilities GetCapabilities()
    {
        return new ManagerCapabilities()
        {
            CanRunAsAdmin = true,
            CanSkipIntegrityChecks = true,
            CanRemoveDataOnUninstall = true,
            SupportsCustomArchitectures = true,
            SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64 },
            SupportsCustomScopes = true,
            SupportsCustomSources = true,
            Sources = new ManagerSource.Capabilities()
            {
                KnowsPackageCount = true,
                KnowsUpdateDate = true
            }
        };
    }

    protected override ManagerProperties GetProperties()
    {
        return new ManagerProperties()
        {
            Name = "Scoop",
            Description = Tools.Translate("Great repository of unknown but useful utilities and other interesting packages.<br>Contains: <b>Utilities, Command-line programs, General Software (extras bucket required)</b>"),
            IconId = "scoop",
            ColorIconId = "scoop_color",
            ExecutableCallArgs = " -NoProfile -ExecutionPolicy Bypass -Command scoop",
            ExecutableFriendlyName = "scoop",
            InstallVerb = "install",
            UpdateVerb = "update",
            UninstallVerb = "uninstall"
        };
    }

    protected override async Task<ManagerStatus> LoadManager()
    {
        ManagerStatus status = new()
        {
            ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe")
        };

        Process process = new()
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
        status.Found = process.ExitCode == 0;


        if (status.Found && IsEnabled())
            _ = RefreshSources();


        Status = status; // Wee need this for the RunCleanup method to get the executable path
        if (status.Found && IsEnabled() && Tools.GetSettings("EnableScoopCleanup"))
            RunCleanup();

        return status;
    }

    private async void RunCleanup()
    {
        AppTools.Log("Starting scoop cleanup...");
        foreach(var command in new string[] { " cache rm *", " cleanup --all --cache", " cleanup --all --global --cache" })
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            p.Start();
            await p.WaitForExitAsync();
        }
    }

    protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
    {
        await Task.Delay(0);
        AppTools.Log("Manager " + Name + " does not support version retrieving, this function should have never been called");
        return new string[0];
    }

    public override ManagerSource[] GetKnownSources()
    {
        return new ManagerSource[]
        {
            new(this, "main", new Uri("https://github.com/ScoopInstaller/Main")),
            new(this, "extras", new Uri("https://github.com/ScoopInstaller/Extras")),
            new(this, "versions", new Uri("https://github.com/ScoopInstaller/Versions")),
            new(this, "nirsoft", new Uri("https://github.com/kodybrown/scoop-nirsoft")),
            new(this, "sysinternals", new Uri("https://github.com/niheaven/scoop-sysinternals")),
            new(this, "php", new Uri("https://github.com/ScoopInstaller/PHP")),
            new(this, "nerd-fonts", new Uri("https://github.com/matthewjberger/scoop-nerd-fonts")),
            new(this, "nonportable", new Uri("https://github.com/ScoopInstaller/Nonportable")),
            new(this, "java", new Uri("https://github.com/ScoopInstaller/Java")),
            new(this, "games", new Uri("https://github.com/Calinou/scoop-games")),
        };
    }

    public override string[] GetAddSourceParameters(ManagerSource source)
    {
        return new string[] { "bucket", "add", source.Name, source.Url.ToString() };
    }

    public override string[] GetRemoveSourceParameters(ManagerSource source)
    {
        return new string[] { "bucket", "rm", source.Name };
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