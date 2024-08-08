using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{

    public class Scoop : PackageManager
    {
        public static new string[] FALSE_PACKAGE_NAMES = [""];
        public static new string[] FALSE_PACKAGE_IDS = ["No"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["Matches"];

        private long LastScoopSourceUpdateTime;

        public Scoop()
        {
            Dependencies = [
                // Scoop-Search is required for search to work
                new ManagerDependency(
                    "Scoop-Search",
                    Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                    "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {scoop install main/scoop-search; if($error.count -ne 0){pause}}\"",
                    "scoop install main/scoop-search",
                    async () => (await CoreTools.Which("scoop-search.exe")).Item1),
                // GIT is required for scoop updates to work
                new ManagerDependency(
                    "Git",
                    Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                    "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {scoop install main/git; if($error.count -ne 0){pause}}\"",
                    "scoop install main/git",
                    async () => (await CoreTools.Which("git.exe")).Item1)
            ];

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRemoveDataOnUninstall = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.X86, Architecture.X64, Architecture.Arm64],
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                Sources = new SourceCapabilities
                {
                    KnowsPackageCount = true,
                    KnowsUpdateDate = true
                }
            };

            Properties = new ManagerProperties
            {
                Name = "Scoop",
                Description = CoreTools.Translate("Great repository of unknown but useful utilities and other interesting packages.<br>Contains: <b>Utilities, Command-line programs, General Software (extras bucket required)</b>"),
                IconId = IconType.Scoop,
                ColorIconId = "scoop_color",
                ExecutableCallArgs = " -NoProfile -ExecutionPolicy Bypass -Command scoop",
                ExecutableFriendlyName = "scoop",
                InstallVerb = "install",
                UpdateVerb = "update",
                UninstallVerb = "uninstall",
                KnownSources = [new ManagerSource(this, "main", new Uri("https://github.com/ScoopInstaller/Main")),
                                new ManagerSource(this, "extras", new Uri("https://github.com/ScoopInstaller/Extras")),
                                new ManagerSource(this, "versions", new Uri("https://github.com/ScoopInstaller/Versions")),
                                new ManagerSource(this, "nirsoft", new Uri("https://github.com/kodybrown/scoop-nirsoft")),
                                new ManagerSource(this, "sysinternals", new Uri("https://github.com/niheaven/scoop-sysinternals")),
                                new ManagerSource(this, "php", new Uri("https://github.com/ScoopInstaller/PHP")),
                                new ManagerSource(this, "nerd-fonts", new Uri("https://github.com/matthewjberger/scoop-nerd-fonts")),
                                new ManagerSource(this, "nonportable", new Uri("https://github.com/ScoopInstaller/Nonportable")),
                                new ManagerSource(this, "java", new Uri("https://github.com/ScoopInstaller/Java")),
                                new ManagerSource(this, "games", new Uri("https://github.com/Calinou/scoop-games"))],
                DefaultSource = new ManagerSource(this, "main", new Uri("https://github.com/ScoopInstaller/Main")),
            };

            SourceProvider = new ScoopSourceProvider(this);
            PackageDetailsProvider = new ScoopPackageDetailsProvider(this);
            OperationProvider = new ScoopOperationProvider(this);
        }

        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = [];

            Tuple<bool, string> which_res = await CoreTools.Which("scoop-search.exe");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " install main/scoop-search",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                IProcessTaskLogger aux_logger = TaskLogger.CreateNew(LoggableTaskType.InstallManagerDependency, proc);
                proc.Start();
                aux_logger.AddToStdOut(await proc.StandardOutput.ReadToEndAsync());
                aux_logger.AddToStdErr(await proc.StandardError.ReadToEndAsync());
                await proc.WaitForExitAsync();
                aux_logger.Close(proc.ExitCode);
                path = "scoop-search.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = query,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

            p.Start();

            string? line;
            IManagerSource source = Properties.DefaultSource;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.StartsWith("'"))
                {
                    string sourceName = line.Split(" ")[0].Replace("'", "");
                    source = GetSourceOrDefault(sourceName);
                }
                else if (line.Trim() != "")
                {
                    string[] elements = line.Trim().Split(" ");
                    if (elements.Length < 2)
                    {
                        continue;
                    }

                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    {
                        continue;
                    }

                    Packages.Add(new Package(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1].Replace("(", "").Replace(")", ""), source, this));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            Dictionary<string, Package> InstalledPackages = [];
            foreach (Package InstalledPackage in await GetInstalledPackages())
            {
                if (!InstalledPackages.ContainsKey(InstalledPackage.Id + "." + InstalledPackage.Version))
                {
                    InstalledPackages.Add(InstalledPackage.Id + "." + InstalledPackage.Version, InstalledPackage);
                }
            }

            List<Package> Packages = [];

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
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
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

            p.Start();

            string? line;
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("---"))
                    {
                        DashesPassed = true;
                    }
                }
                else if (line.Trim() != "")
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Trim().Split(" ");
                    if (elements.Length < 3)
                    {
                        continue;
                    }

                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    {
                        continue;
                    }

                    if (InstalledPackages.TryGetValue(elements[0] + "." + elements[1], out Package? InstalledPackage) && InstalledPackage is not null)
                    {
                        OverridenInstallationOptions options = new(InstalledPackage.OverridenOptions.Scope);
                        Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], InstalledPackage.Source, this, options));
                    }
                    else
                    {
                        Logger.Warn("Upgradable scoop package not listed on installed packages - id=" + elements[0]);
                    }
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            List<Package> Packages = [];

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
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
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);
            p.Start();

            string? line;
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("---"))
                    {
                        DashesPassed = true;
                    }
                }
                else if (line.Trim() != "")
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Trim().Split(" ");
                    if (elements.Length < 3)
                    {
                        continue;
                    }

                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    {
                        continue;
                    }

                    OverridenInstallationOptions options = new(
                        line.Contains("Global install") ? PackageScope.Global : PackageScope.User
                    );

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], GetSourceOrDefault(elements[2]), this, options));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return Packages.ToArray();
        }

        public override async Task RefreshPackageIndexes()
        {
            if (new TimeSpan(DateTime.Now.Ticks - LastScoopSourceUpdateTime).TotalMinutes < 10)
            {
                Logger.Info("Scoop buckets have been already refreshed in the last ten minutes, skipping.");
                return;
            }
            LastScoopSourceUpdateTime = DateTime.Now.Ticks;
            Process p = new();
            ProcessStartInfo StartInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " update",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            p.StartInfo = StartInfo;
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.RefreshIndexes, p);
            p.Start();
            logger.AddToStdOut(await p.StandardOutput.ReadToEndAsync());
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new()
            {
                ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe")
            };

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
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
            status.Found = (await CoreTools.Which("scoop")).Item1;

            Status = status; // Wee need this for the RunCleanup method to get the executable path
            if (status.Found && IsEnabled() && Settings.Get("EnableScoopCleanup"))
            {
                RunCleanup();
            }

            return status;
        }

        private async void RunCleanup()
        {
            Logger.Info("Starting scoop cleanup...");
            foreach (string command in new[] { " cache rm *", " cleanup --all --cache", " cleanup --all --global --cache" })
            {
                Process p = new()
                {
                    StartInfo = new ProcessStartInfo
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

            Logger.Info("Scoop cleanup finished!");
        }
    }
}
