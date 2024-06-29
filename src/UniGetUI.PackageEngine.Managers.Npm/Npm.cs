using System.Diagnostics;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.NpmManager
{
    public class Npm : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };
        
        public Npm() : base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };

            Properties = new ManagerProperties()
            {
                Name = "Npm",
                Description = CoreTools.Translate("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"),
                IconId = "node",
                ColorIconId = "node_color",
                ExecutableFriendlyName = "npm",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install",
                ExecutableCallArgs = " -NoProfile -ExecutionPolicy Bypass -Command npm",
                DefaultSource = new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/")),
                KnownSources = [new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/"))],

            };

            PackageDetailsProvider = new NpmPackageDetailsProvider(this);
        }
        
        protected override async Task<IPackage[]> FindPackages_UnSafe(string query)
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

            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);
            p.Start();
            
            string? line;
            List<Package> Packages = new();
            bool HeaderPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!HeaderPassed)
                {
                    if (line.Contains("NAME"))
                        HeaderPassed = true;
                    else
                    {
                        string[] elements = line.Split('\t');
                        if (elements.Length >= 5)
                            Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[4], DefaultSource, this));
                    }
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        protected override async Task<IPackage[]> GetAvailableUpdates_UnSafe()
        {
            List<Package> Packages = new();
            foreach (PackageScope scope in new PackageScope[] { PackageScope.Local, PackageScope.Global })
            {
                Process p = new();
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " outdated --parseable" + (scope == PackageScope.Global ? " --global" : ""),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);
                p.Start();

                string? line;
                while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                {
                    logger.AddToStdOut(line);
                    string[] elements = line.Split(':');
                    if (elements.Length >= 4)
                    {
                        if (elements[2][0] == '@')
                            elements[2] = "%" + elements[2][1..];

                        if (elements[3][0] == '@')
                            elements[3] = "%" + elements[3][1..];

                        Packages.Add(new Package(CoreTools.FormatAsName(elements[2].Split('@')[0]).Replace('%', '@'), elements[2].Split('@')[0].Replace('%', '@'), elements[3].Split('@')[^1].Replace('%', '@'), elements[2].Split('@')[^1].Replace('%', '@'), DefaultSource, this, scope));
                    }
                }

                logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
                await p.WaitForExitAsync();
                logger.Close(p.ExitCode);
            }
            return Packages.ToArray();
        }

        protected override async Task<IPackage[]> GetInstalledPackages_UnSafe()
        {
            List<Package> Packages = new();
            foreach (PackageScope scope in new PackageScope[] { PackageScope.Local, PackageScope.Global })
            {
                Process p = new();
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list" + (scope == PackageScope.Global? " --global": ""),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListPackages, p);
                p.Start();
                
                string? line;
                while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                {
                    logger.AddToStdOut(line);
                    if (line.Contains("--") || line.Contains("├─") || line.Contains("└─"))
                    {
                        string[] elements = line[4..].Split('@');
                        if (elements.Length >= 2)
                        {
                            if (line.Contains(" @"))
                            {
                                elements[0] = "@" + elements[1];
                                if (elements.Length >= 3) elements[1] = elements[2];
                            }
                            Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, scope));
                        }
                    }
                }
                logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
                await p.WaitForExitAsync();
                logger.Close(p.ExitCode);
            }

            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUninstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            if (options.Version != "")
                parameters[1] = package.Id + "@" + package.Version;
            else
                parameters[1] = package.Id + "@latest";

            if (options.PreRelease)
                parameters.AddRange(["--include", "dev"]);
            
            if (options.InstallationScope == PackageScope.Global)
                parameters.Add("--global");

            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(IPackage package, IInstallationOptions options)
        {
            string[] parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            parameters[1] = package.Id + "@" + package.NewVersion;
            return parameters;
        }
        public override string[] GetUninstallParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (package.Scope == PackageScope.Global)
                parameters.Add("--global");

            return parameters.ToArray();

        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            status.ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe");
            status.Found = (await CoreTools.Which("npm")).Item1;

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

            return status;
        }

        
    }
}
