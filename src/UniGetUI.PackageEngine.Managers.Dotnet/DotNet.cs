using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.DotNetManager
{
    public class DotNet : BaseNuGet
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };

        public DotNet() : base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomScopes = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64, Architecture.Arm },
                SupportsPreRelease = true,
                SupportsCustomLocations = true,
                SupportsCustomPackageIcons = true,
                SupportsCustomVersions = true,
            };

            Properties = new ManagerProperties()
            {
                Name = ".NET Tool",
                Description = CoreTools.Translate("A repository full of tools and executables designed with Microsoft's .NET ecosystem in mind.<br>Contains: <b>.NET related tools and scripts</b>"),
                IconId = "dotnet",
                ColorIconId = "dotnet_color",
                ExecutableFriendlyName = "dotnet tool",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "tool",
                DefaultSource = new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/api/v2")),
                KnownSources = [new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/api/v2"))],
            };
        }

        protected override async Task<IPackage[]> GetAvailableUpdates_UnSafe()
        {
            Tuple<bool, string> which_res = await CoreTools.Which("dotnet-tools-outdated.exe");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new();
                proc.StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " install --global dotnet-tools-outdated",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                ManagerClasses.Classes.ProcessTaskLogger aux_logger = TaskLogger.CreateNew(LoggableTaskType.InstallManagerDependency, proc);
                proc.Start();

                aux_logger.AddToStdOut(await proc.StandardOutput.ReadToEndAsync());
                aux_logger.AddToStdErr(await proc.StandardError.ReadToEndAsync());
                await proc.WaitForExitAsync();
                aux_logger.Close(proc.ExitCode);
                
                path = "dotnet-tools-outdated.exe";
            }

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = path,
                Arguments = "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);
            p.Start();

            string? line;
            bool DashesPassed = false;
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this, PackageScope.Global));
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
            foreach (PackageScope scope in new PackageScope[] { PackageScope.Local, PackageScope.Global})
            {
                Process p = new();
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + $" list" + (scope == PackageScope.Global? " --global": ""),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
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
                        if (line.Contains("----"))
                            DashesPassed = true;
                    }
                    else
                    {
                        string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                        if (elements.Length < 2)
                            continue;

                        for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                        if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                            continue;

                        Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, scope));
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
            string[] parameters = GetUpdateParameters(package, options);
            parameters[0] = Properties.InstallVerb;
            return parameters;
        }
        public override string[] GetUpdateParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;

            if (options.Architecture == Architecture.X86)
                parameters.AddRange(new string[] { "--arch", "x86" });
            else if (options.Architecture == Architecture.X64)
                parameters.AddRange(new string[] { "--arch", "x64" });
            else if (options.Architecture == Architecture.Arm)
                parameters.AddRange(new string[] { "--arch", "arm32" });
            else if (options.Architecture == Architecture.Arm64)
                parameters.AddRange(new string[] { "--arch", "arm64" });

            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (options.CustomInstallLocation != "")
                parameters.AddRange(new string[] { "--tool-path", "\"" + options.CustomInstallLocation + "\"" });
            else if (package.Scope == PackageScope.Global)
                parameters.Add("--global");

            return parameters.ToArray();
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            Tuple<bool, string> which_res = await CoreTools.Which("dotnet.exe");
            status.ExecutablePath = which_res.Item2;
            status.Found = which_res.Item1;

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

            return status;
        }
    }
}
