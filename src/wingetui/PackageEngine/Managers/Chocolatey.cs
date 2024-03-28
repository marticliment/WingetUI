using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ModernWindow.PackageEngine.Managers
{
    public class Chocolatey : PackageManagerWithSources
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "Directory", "", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "Output is package name ", "operable", "Invalid" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "packages", "current version", "installed version", "is", "program", "validations", "argument", "no" };
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search \"" + query + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            string output = "";
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length < 2)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " outdated",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            string output = "";
            List<UpgradablePackage> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split('|');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length <= 2)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new UpgradablePackage(Tools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], MainSource, this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            string output = "";
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length <= 1)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }
        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == 1641 || ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (ReturnCode == 3010)
                return OperationVeredict.Succeeded; // TODO: Restart required
            else if ((output_string.Contains("Run as administrator") || output_string.Contains("The requested operation requires elevation") || output_string.Contains("ERROR: Exception calling \"CreateDirectory\" with \"1\" argument(s): \"Access to the path")) && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }
            return OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == 1641 || ReturnCode == 1614 || ReturnCode == 1605 || ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (ReturnCode == 3010)
                return OperationVeredict.Succeeded; // TODO: Restart required
            else if ((output_string.Contains("Run as administrator") || output_string.Contains("The requested operation requires elevation")) && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }
            return OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;
            parameters.Add("--no-progress");

            if (options.Architecture == System.Runtime.InteropServices.Architecture.X86)
                parameters.Add("--forcex86");

            if (options.PreRelease)
                parameters.Add("--prerelease");

            if (options.SkipHashCheck)
                parameters.AddRange(new string[] { "--ignore-checksums", "--force" });

            if (options.Version != "")
                parameters.AddRange(new string[] { "--version=" + options.Version, "--allow-downgrade" });

            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            return parameters;
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id, "-y" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (options.InteractiveInstallation)
                parameters.Add("--notsilent");

            return parameters.ToArray();
        }

        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "community", new Uri("https://community.chocolatey.org/api/v2/"));
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);

            AppTools.Log(package.Source.Url.ToString().Trim()[^1]);

            if (package.Source.Name == "community")
                details.ManifestUrl = new Uri("https://community.chocolatey.org/packages/" + package.Id);
            else if (package.Source.Url != null && package.Source.Url.ToString().Trim()[^1].ToString() == "/")
                details.ManifestUrl = new Uri((package.Source.Url.ToString().Trim() + "package/" + package.Id).Replace("//", "/").Replace(":/", "://"));



            if (package.Source.Name == "community")
            {
                try
                {
                    details.InstallerType = Tools.Translate("NuPkg (zipped manifest)");
                    details.InstallerUrl = new Uri("https://packages.chocolatey.org/" + package.Id + "." + package.Version + ".nupkg");
                    details.InstallerSize = await Tools.GetFileSizeAsync(details.InstallerUrl);
                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            }

            Process process = new();
            List<string> output = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " info " + package.Id,
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
                    if (line.StartsWith("  ") && IsLoadingDescription)
                        details.Description += "\n" + line.Trim();
                    else if (line.StartsWith("  ") && IsLoadingReleaseNotes)
                        details.ReleaseNotes += "\n" + line.Trim();

                    // Stop loading multiline fields
                    else if (IsLoadingDescription)
                        IsLoadingDescription = false;
                    else if (IsLoadingReleaseNotes)
                        IsLoadingReleaseNotes = false;
                    else if (IsLoadingTags)
                        IsLoadingTags = false;

                    // Check for singleline fields
                    if (line.StartsWith(" ") && line.Contains("Title:"))
                        details.UpdateDate = line.Split("|")[1].Trim().Replace("Published:", "");

                    else if (line.StartsWith(" ") && line.Contains("Author:"))
                        details.Author = line.Split(":")[1].Trim();

                    else if (line.StartsWith(" ") && line.Contains("Software Site:"))
                        details.HomepageUrl = new Uri(line.Replace("Software Site:", "").Trim());

                    else if (line.StartsWith(" ") && line.Contains("Software License:"))
                        details.LicenseUrl = new Uri(line.Replace("Software License:", "").Trim());

                    else if (line.StartsWith(" ") && line.Contains("Package Checksum:"))
                        details.InstallerHash = line.Split(":")[1].Trim().Replace("'", "");

                    else if (line.StartsWith(" ") && line.Contains("Description:"))
                    {
                        details.Description = line.Split(":")[1].Trim();
                        IsLoadingDescription = true;
                    }
                    else if (line.StartsWith(" ") && line.Contains("Release Notes:"))
                    {
                        details.ReleaseNotesUrl = new Uri(line.Replace("Release Notes:", "").Trim());
                        details.ReleaseNotes = "";
                        IsLoadingReleaseNotes = true;
                    }
                    else if (line.StartsWith(" ") && line.Contains("Tags"))
                    {
                        List<string> tags = new();
                        foreach (string tag in line.Replace("Tags:", "").Trim().Split(' '))
                        {
                            if (tag.Trim() != "")
                                tags.Add(tag.Trim());
                        }
                        details.Tags = tags.ToArray();
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


            string output = "";
            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                try
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.Contains(" - ") && line.Contains(" | "))
                    {
                        string[] parts = line.Trim().Split('|')[0].Trim().Split(" - ");
                        if (parts[1].Trim() == "https://community.chocolatey.org/api/v2/")
                            sources.Add(new ManagerSource(this, "community", new Uri("https://community.chocolatey.org/api/v2/")));
                        else
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

#pragma warning disable CS1998
        public override async Task RefreshPackageIndexes()
        {
            // Chocolatey does not support source refreshing
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
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86 },
                SupportsPreRelease = true,
                SupportsCustomSources = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new()
            {
                Name = "Chocolatey",
                Description = Tools.Translate("The classical package manager for windows. You'll find everything there. <br>Contains: <b>General Software</b>"),
                IconId = "choco",
                ColorIconId = "choco_color",
                ExecutableFriendlyName = "choco.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "upgrade",
                ExecutableCallArgs = "",

            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            if (Tools.GetSettings("UseSystemChocolatey"))
                status.ExecutablePath = await Tools.Which("choco.exe");
            else if (File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\WingetUI\\choco-cli\\choco.exe")))
                status.ExecutablePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\WingetUI\\choco-cli\\choco.exe");
            else
                status.ExecutablePath = Path.Join(Directory.GetParent(Environment.ProcessPath).FullName, "choco-cli\\choco.exe");

            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();
    
            // If the user is running bundled chocolatey and chocolatey is not in path, add chocolatey to path
            if (/*Tools.GetSettings("ShownWelcomeWizard") && */!Tools.GetSettings("UseSystemChocolatey") && !Tools.GetSettings("ChocolateyAddedToPath") && !File.Exists(@"C:\ProgramData\Chocolatey\bin\choco.exe"))
            {
                AppTools.Log("Adding chocolatey to path");
                string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("PATH", $"{status.ExecutablePath.Replace("\\choco.exe", "\\bin")};{path}", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("chocolateyinstall", Path.GetDirectoryName(status.ExecutablePath), EnvironmentVariableTarget.User);
                Tools.SetSettings("ChocolateyAddedToPath", true);
            }
            
            if (status.Found && IsEnabled())
                await RefreshPackageIndexes();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " find -e " + package.Id + " -a",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            AppTools.Log(p.StartInfo.FileName);
            AppTools.Log(p.StartInfo.Arguments.ToString());

            p.Start();
            string line;
            string output = "";
            List<string> versions = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    AppTools.Log(line);
                    if (elements.Length < 2 || elements[0].Trim() != package.Id)
                        continue;

                    versions.Add(elements[1].Trim());
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);

            return versions.ToArray();
        }

        public override ManagerSource[] GetKnownSources()
        {
            return new ManagerSource[] { new(this, "chocolatey", new Uri("https://community.chocolatey.org/api/v2/")) };
        }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "add", "--name", source.Name, "--source", source.Url.ToString(), "-y" };
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "remove", "--name", source.Name, "-y" };
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
}
