using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{

    public class Scoop : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "No" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "Matches" };

        long LastScoopSourceUpdateTime = 0;

        public Scoop(): base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRemoveDataOnUninstall = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64 },
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                Sources = new SourceCapabilities()
                {
                    KnowsPackageCount = true,
                    KnowsUpdateDate = true
                }
            };

            Properties = new ManagerProperties()
            {
                Name = "Scoop",
                Description = CoreTools.Translate("Great repository of unknown but useful utilities and other interesting packages.<br>Contains: <b>Utilities, Command-line programs, General Software (extras bucket required)</b>"),
                IconId = "scoop",
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
        }

        protected override async Task<IPackage[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = new();

            Tuple<bool, string> which_res = await CoreTools.Which("scoop-search.exe");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " install main/scoop-search",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                ManagerClasses.Classes.ProcessTaskLogger aux_logger = TaskLogger.CreateNew(LoggableTaskType.InstallManagerDependency, proc);
                proc.Start();
                aux_logger.AddToStdOut(await proc.StandardOutput.ReadToEndAsync());
                aux_logger.AddToStdErr(await proc.StandardError.ReadToEndAsync());
                await proc.WaitForExitAsync();
                aux_logger.Close(proc.ExitCode);
                path = "scoop-search.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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
            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

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
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1].Replace("(", "").Replace(")", ""), source, this));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return Packages.ToArray();
        }

        protected override async Task<IPackage[]> GetAvailableUpdates_UnSafe()
        {
            Dictionary<string, Package> InstalledPackages = new();
            foreach (Package InstalledPackage in await GetInstalledPackages())
            {
                if (!InstalledPackages.ContainsKey(InstalledPackage.Id + "." + InstalledPackage.Version))
                    InstalledPackages.Add(InstalledPackage.Id + "." + InstalledPackage.Version, InstalledPackage);
            }

            List<Package> Packages = new();


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
            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

            p.Start();

            string? line;
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
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
                        Logger.Warn("Upgradable scoop package not listed on installed packages - id=" + elements[0]);
                        continue;
                    }

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], InstalledPackages[elements[0] + "." + elements[1]].Source, this, InstalledPackages[elements[0] + "." + elements[1]].Scope));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return Packages.ToArray();
        }

        protected override async Task<IPackage[]> GetInstalledPackages_UnSafe()
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
            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListPackages, p);
            p.Start();

            string? line;
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
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

                    PackageScope scope = PackageScope.User;
                    if (line.Contains("Global install"))
                        scope = PackageScope.Global;

                    Packages.Add(new Package(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1], GetSourceOrDefault(elements[2]), this, scope));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);
            return Packages.ToArray();
        }

        
        public override OperationVeredict GetUninstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
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
        public override OperationVeredict GetInstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
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
        public override OperationVeredict GetUpdateOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override string[] GetUninstallParameters(IPackage package, IInstallationOptions options)
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
        public override string[] GetInstallParameters(IPackage package, IInstallationOptions options)
        {
            string[] parameters = GetUpdateParameters(package, options);
            parameters[0] = Properties.InstallVerb;
            return parameters;
        }
        public override string[] GetUpdateParameters(IPackage package, IInstallationOptions options)
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
            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.RefreshIndexes, p);
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
            status.Found = (await CoreTools.Which("scoop")).Item1;


            Status = status; // Wee need this for the RunCleanup method to get the executable path
            if (status.Found && IsEnabled() && Settings.Get("EnableScoopCleanup"))
                RunCleanup();

            return status;
        }

        private async void RunCleanup()
        {
            Logger.Info("Starting scoop cleanup...");
            foreach (string command in new string[] { " cache rm *", " cleanup --all --cache", " cleanup --all --global --cache" })
            {
                Process p = new()
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
            Logger.Info("Scoop cleanup finished!");
        }
    }
}