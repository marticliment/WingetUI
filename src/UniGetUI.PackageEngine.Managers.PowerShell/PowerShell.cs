using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    public class PowerShell : BaseNuGet
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };

        public PowerShell() : base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                SupportsPreRelease = true,
                SupportsCustomPackageIcons = true,
                Sources = new SourceCapabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };

            Properties = new ManagerProperties()
            {
                Name = "PowerShell",
                Description = CoreTools.Translate("PowerShell's package manager. Find libraries and scripts to expand PowerShell capabilities<br>Contains: <b>Modules, Scripts, Cmdlets</b>"),
                IconId = "powershell",
                ColorIconId = "powershell_color",
                ExecutableFriendlyName = "powershell.exe",
                InstallVerb = "Install-Module",
                UninstallVerb = "Uninstall-Module",
                UpdateVerb = "Update-Module",
                ExecutableCallArgs = " -NoProfile -Command",
                KnownSources = [new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
                                new ManagerSource(this, "PoshTestGallery", new Uri("https://www.poshtestgallery.com/api/v2"))],
                DefaultSource = new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
            };

            SourceProvider = new PowerShellSourceProvider(this);
        }
        protected override async Task<IPackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

            p.Start();

            string command = """
                function Test-GalleryModuleUpdate {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [version] $Version,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Repository,
                        [switch] $NeedUpdateOnly
                    )
                    process {
                        $URLs = @{}
                        @(Get-PSRepository).ForEach({$URLs[$_.Name] = $_.SourceLocation})
                        $page = Invoke-WebRequest -Uri ($URLs[$Repository] + "/package/$Name") -UseBasicParsing -Maximum 0 -ea Ignore
                        [version]$latest = Split-Path -Path ($page.Headers.Location -replace "$Name." -replace ".nupkg") -Leaf
                        $needsupdate = $Latest -gt $Version
                        if ($needsupdate) {
                                Write-Output($Name + "|" + $Version.ToString() + "|" + $Latest.ToString() + "|" + $Repository)
                        }
                    }
                }
                Get-InstalledModule | Test-GalleryModuleUpdate


                exit
                """;
            await p.StandardInput.WriteLineAsync(command);
            logger.AddToStdIn(command);
            p.StandardInput.Close();

            string? line;
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.StartsWith(">>"))
                    continue;

                string[] elements = line.Split('|');
                if (elements.Length < 4)
                    continue;

                for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                if (elements[1] + ".0" == elements[2] || elements[1] + ".0.0" == elements[2])
                    continue;

                Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], GetSourceOrDefault(elements[3]), this));
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        protected override async Task<IPackage[]> GetInstalledPackages_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Get-InstalledModule",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListPackages, p);

            p.Start();
            string? line;
            List<Package> Packages = new();
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[1]), elements[1], elements[0], GetSourceOrDefault(elements[2]), this));
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetUninstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUpdateOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetUninstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (output_string.Contains("AdminPrivilegesAreRequired") && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = GetUpdateParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            parameters.AddRange(new string[] { "-AllowClobber" });
            if (package.Scope == PackageScope.Global)
                parameters.AddRange(new string[] { "-Scope", "AllUsers" });
            else
                parameters.AddRange(new string[] { "-Scope", "CurrentUser" });

            if (options.Version != "")
                parameters.AddRange(new string[] { "-RequiredVersion", options.Version });

            return parameters.ToArray();

        }
        public override string[] GetUpdateParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;

            if (options.PreRelease)
                parameters.Add("-AllowPrerelease");

            if (options.SkipHashCheck)
                parameters.Add("-SkipPublisherCheck");

            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(IPackage package, IInstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, "-Name", package.Id, "-Confirm:$false", "-Force" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            return parameters.ToArray();
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new()
            {
                ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe")
            };
            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " \"echo $PSVersionTable\"",
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
