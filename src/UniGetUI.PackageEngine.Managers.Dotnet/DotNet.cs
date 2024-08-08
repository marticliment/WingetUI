using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.Managers.DotNetManager
{
    public class DotNet : BaseNuGet
    {
        public static new string[] FALSE_PACKAGE_NAMES = [""];
        public static new string[] FALSE_PACKAGE_IDS = [""];
        public static new string[] FALSE_PACKAGE_VERSIONS = [""];

        public DotNet()
        {
            Dependencies = [
                new ManagerDependency(
                    ".NET Tools Outdated",
                    Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                    "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {dotnet tool install --global dotnet-tools-outdated --add-source https://api.nuget.org/v3/index.json; if($error.count -ne 0){pause}}\"",
                    "dotnet tool install --global dotnet-tools-outdated --add-source https://api.nuget.org/v3/index.json",
                    async () => (await CoreTools.Which("dotnet-tools-outdated.exe")).Item1)
            ];

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                SupportsCustomScopes = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.X86, Architecture.X64, Architecture.Arm64, Architecture.Arm],
                SupportsPreRelease = true,
                SupportsCustomLocations = true,
                SupportsCustomPackageIcons = true,
                SupportsCustomVersions = true,
            };

            Properties = new ManagerProperties
            {
                Name = ".NET Tool",
                Description = CoreTools.Translate("A repository full of tools and executables designed with Microsoft's .NET ecosystem in mind.<br>Contains: <b>.NET related tools and scripts</b>"),
                IconId = IconType.DotNet,
                ColorIconId = "dotnet_color",
                ExecutableFriendlyName = "dotnet tool",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "tool",
                DefaultSource = new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/api/v2")),
                KnownSources = [new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/api/v2"))],
            };

            OperationProvider = new DotNetOperationProvider(this);
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            Tuple<bool, string> which_res = await CoreTools.Which("dotnet-tools-outdated.exe");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " install --global dotnet-tools-outdated",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                };

                IProcessTaskLogger aux_logger = TaskLogger.CreateNew(LoggableTaskType.InstallManagerDependency, proc);
                proc.Start();

                aux_logger.AddToStdOut(await proc.StandardOutput.ReadToEndAsync());
                aux_logger.AddToStdErr(await proc.StandardError.ReadToEndAsync());
                await proc.WaitForExitAsync();
                aux_logger.Close(proc.ExitCode);

                path = "dotnet-tools-outdated.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "",
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
            List<Package> Packages = [];
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                    {
                        DashesPassed = true;
                    }
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
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

                    Packages.Add(new Package(
                        CoreTools.FormatAsName(elements[0]),
                        elements[0],
                        elements[1],
                        elements[2],
                        DefaultSource,
                        this,
                        new(PackageScope.Global)
                    ));
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
                        if (line.Contains("----"))
                        {
                            DashesPassed = true;
                        }
                    }
                    else
                    {
                        string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
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

                        Packages.Add(new Package(
                            CoreTools.FormatAsName(elements[0]),
                            elements[0],
                            elements[1],
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

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            Tuple<bool, string> which_res = await CoreTools.Which("dotnet.exe");
            status.ExecutablePath = which_res.Item2;
            status.Found = which_res.Item1;

            if (!status.Found)
            {
                return status;
            }

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = status.ExecutablePath,
                    Arguments = "tool -h",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                status.Found = false;
                return status;
            }

            process = new()
            {
                StartInfo = new ProcessStartInfo
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

            return status;
        }
    }
}
