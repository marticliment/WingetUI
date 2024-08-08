using System.Diagnostics;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.Managers.NpmManager
{
    public class Npm : PackageManager
    {
        public static new string[] FALSE_PACKAGE_NAMES = [""];
        public static new string[] FALSE_PACKAGE_IDS = [""];
        public static new string[] FALSE_PACKAGE_VERSIONS = [""];

        public Npm()
        {
            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };

            Properties = new ManagerProperties
            {
                Name = "Npm",
                Description = CoreTools.Translate("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"),
                IconId = IconType.Node,
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
            OperationProvider = new NpmOperationProvider(this);
        }
        
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
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
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);
            p.Start();

            string? line;
            List<Package> Packages = [];
            bool HeaderPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!HeaderPassed)
                {
                    if (line.Contains("NAME"))
                    {
                        HeaderPassed = true;
                    }
                    else
                    {
                        string[] elements = line.Split('\t');
                        if (elements.Length >= 5)
                        {
                            Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[4], DefaultSource, this));
                        }
                    }
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            List<Package> Packages = [];
            foreach (var options in new OverridenInstallationOptions[] { new(PackageScope.Local), new(PackageScope.Global) })
            {
                Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " outdated --parseable" + (options.Scope == PackageScope.Global ? " --global" : ""),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };

                IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);
                p.Start();

                string? line;
                while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                {
                    logger.AddToStdOut(line);
                    string[] elements = line.Split(':');
                    if (elements.Length >= 4)
                    {
                        if (elements[2][0] == '@')
                        {
                            elements[2] = "%" + elements[2][1..];
                        }

                        if (elements[3][0] == '@')
                        {
                            elements[3] = "%" + elements[3][1..];
                        }

                        Packages.Add(new Package(
                            CoreTools.FormatAsName(elements[2].Split('@')[0]).Replace('%', '@'),
                            elements[2].Split('@')[0].Replace('%', '@'),
                            elements[3].Split('@')[^1].Replace('%', '@'),
                            elements[2].Split('@')[^1].Replace('%', '@'),
                            DefaultSource,
                            this,
                            options
                        ));
                    }
                }

                logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
                await p.WaitForExitAsync();
                logger.Close(p.ExitCode);
            }
            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            List<Package> Packages = [];
            foreach (var options in new OverridenInstallationOptions[] { new(PackageScope.Local), new(PackageScope.Global) })
            {
                Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " list" + (options.Scope == PackageScope.Global ? " --global" : ""),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };

                IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);
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
                                if (elements.Length >= 3)
                                {
                                    elements[1] = elements[2];
                                }
                            }
                            Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, options));
                        }
                    }
                }
                logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
                await p.WaitForExitAsync();
                logger.Close(p.ExitCode);
            }

            return Packages.ToArray();
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new()
            {
                ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                Found = (await CoreTools.Which("npm")).Item1
            };

            if (!status.Found)
            {
                return status;
            }

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
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
