using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ModernWindow.PackageEngine.Managers
{
    public class Npm : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search \"" + query + "\" --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            bool HeaderPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!HeaderPassed)
                    if (line.Contains("NAME"))
                        HeaderPassed = true;
                    else
                    {
                        string[] elements = line.Split('\t');
                        if (elements.Length >= 5)
                            Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[4], MainSource, this));
                    }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " outdated --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            List<UpgradablePackage> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                string[] elements = line.Split(':');
                if (elements.Length >= 4)
                {
                    Packages.Add(new UpgradablePackage(Tools.FormatAsName(elements[2].Split('@')[0]), elements[2].Split('@')[0], elements[3].Split('@')[^1], elements[2].Split('@')[^1], MainSource, this));
                }
            }

            p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " outdated --global --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                string[] elements = line.Split(':');
                if (elements.Length >= 4)
                {
                    if (elements[2][0] == '@')
                        elements[2] = "%" + elements[2][1..];


                    if (elements[3][0] == '@')
                        elements[3] = "%" + elements[3][1..];

                    Packages.Add(new UpgradablePackage(Tools.FormatAsName(elements[2].Split('@')[0]).Replace('%', '@'), elements[2].Split('@')[0].Replace('%', '@'), elements[3].Split('@')[^1].Replace('%', '@'), elements[2].Split('@')[^1].Replace('%', '@'), MainSource, this, PackageScope.Global));
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
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (line.Contains("--") || line.Contains("├─") || line.Contains("└─"))
                {
                    string[] elements = line[4..].Split('@');
                    Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }

            p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list --global",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (line.Contains("--") || line.Contains("├─") || line.Contains("└─"))
                {
                    line = line.Replace("- @", "- %");
                    string[] elements = line[4..].Split('@');
                    Packages.Add(new Package(Tools.FormatAsName(elements[0].Replace('%', '@')), elements[0].Replace('%', '@'), elements[1], MainSource, this, PackageScope.Global));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetUninstallParameters(package, options);
            parameters[0] = Properties.InstallVerb;

            if (options.Version != "")
                parameters[1] = package.Id + "@" + package.Version;
            else
                parameters[1] = package.Id + "@latest";

            return parameters;
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetUninstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            parameters[1] = package.Id + "@" + package.NewVersion;
            return parameters;
        }
        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (package.Scope == PackageScope.Global)
                parameters.Add("--global");

            return parameters.ToArray();

        }
        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/"));
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);
            try
            {
                details.InstallerType = "Tarball";
                details.ManifestUrl = new Uri($"https://www.npmjs.com/package/{package.Id}");
                details.ReleaseNotesUrl = new Uri($"https://www.npmjs.com/package/{package.Id}?activeTab=versions");

                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo()
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " info " + package.Id,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    };

                    p.Start();

                    List<string> output = new();
                    string line;
                    while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                    {
                        output.Add(line);
                    }

                    int lineNo = 0;
                    bool ReadingMaintainer = false;
                    foreach (string outLine in output)
                    {
                        try
                        {
                            lineNo++;
                            if (lineNo == 2)
                            {
                                details.License = outLine.Split("|")[1];
                            }
                            else if (lineNo == 3)
                            {
                                details.Description = outLine.Trim();
                            }
                            else if (lineNo == 4)
                            {
                                details.HomepageUrl = new Uri(outLine.Trim());
                            }
                            else if (outLine.StartsWith(".tarball"))
                            {
                                details.InstallerUrl = new Uri(outLine.Replace(".tarball: ", "").Trim());
                                details.InstallerSize = await Tools.GetFileSizeAsync(details.InstallerUrl);
                            }
                            else if (outLine.StartsWith(".integrity"))
                            {
                                details.InstallerHash = outLine.Replace(".integrity: sha512-", "").Replace("==", "").Trim();
                            }
                            else if (outLine.StartsWith("maintainers:"))
                            {
                                ReadingMaintainer = true;
                            }
                            else if (ReadingMaintainer)
                            {
                                ReadingMaintainer = false;
                                details.Author = outLine.Replace("-", "").Split('<')[0].Trim();
                            }
                            else if (outLine.StartsWith("published"))
                            {
                                details.Publisher = outLine.Split("by").Last().Split('<')[0].Trim();
                                details.UpdateDate = outLine.Replace("published", "").Split("by")[0].Trim();
                            }
                        }
                        catch (Exception e)
                        {
                            AppTools.Log(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }

            return details;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task RefreshPackageIndexes()
        {
            // Npm does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new()
            {
                Name = "Npm",
                Description = Tools.Translate("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"),
                IconId = "node",
                ColorIconId = "node_color",
                ExecutableFriendlyName = "npm",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install",
                ExecutableCallArgs = " -NoProfile -ExecutionPolicy Bypass -Command npm",

            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            status.ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe");
            status.Found = File.Exists(await Tools.Which("npm"));

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();
            await process.WaitForExitAsync();

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
                    Arguments = Properties.ExecutableCallArgs + " show " + package.Id + " versions --json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            string line;
            List<string> versions = new();

            p.Start();

            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (line.Contains("\""))
                    versions.Add(line.Trim().TrimStart('"').TrimEnd(',').TrimEnd('"'));
            }

            return versions.ToArray();
        }
    }
}
