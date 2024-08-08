using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PowerShell7Manager
{
    public class PowerShell7 : BaseNuGet
    {
        public static new string[] FALSE_PACKAGE_NAMES = [""];
        public static new string[] FALSE_PACKAGE_IDS = [""];
        public static new string[] FALSE_PACKAGE_VERSIONS = [""];

        public PowerShell7()
        {
            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                SupportsPreRelease = true,
                SupportsCustomPackageIcons = true,
                Sources = new SourceCapabilities
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };

            Properties = new ManagerProperties
            {
                Name = "PowerShell7",
                DisplayName = "PowerShell 7.x (beta)",
                Description = CoreTools.Translate("PowerShell's package manager. Find libraries and scripts to expand PowerShell capabilities<br>Contains: <b>Modules, Scripts, Cmdlets</b>"),
                IconId = IconType.PowerShell,
                ColorIconId = "powershell_color",
                ExecutableFriendlyName = "pwsh.exe",
                InstallVerb = "Install-Module",
                UninstallVerb = "Uninstall-Module",
                UpdateVerb = "Update-Module",
                ExecutableCallArgs = " -NoProfile -Command",
                KnownSources = [new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
                                new ManagerSource(this, "PoshTestGallery", new Uri("https://www.poshtestgallery.com/api/v2"))],
                DefaultSource = new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
            };

            SourceProvider = new PowerShell7SourceProvider(this);
            OperationProvider = new PowerShell7OperationProvider(this);
        }
        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardInputEncoding = new UTF8Encoding(false),
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

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
                        $page = Invoke-WebRequest -Uri ($URLs[$Repository] + "/package/$Name") -UseBasicParsing -ea Ignore
                        [version]$latest = Split-Path -Path ($page.BaseResponse.RequestMessage.RequestUri -replace "$Name." -replace ".nupkg") -Leaf
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
            List<Package> Packages = [];
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.StartsWith(">>"))
                {
                    continue;
                }

                string[] elements = line.Split('|');
                if (elements.Length < 4)
                {
                    continue;
                }

                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i] = elements[i].Trim();
                }

                if (elements[1] + ".0" == elements[2] || elements[1] + ".0.0" == elements[2])
                {
                    continue;
                }

                Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], GetSourceOrDefault(elements[3]), this));
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " Get-InstalledModule",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);

            p.Start();
            string? line;
            List<Package> Packages = [];
            bool DashesPassed = false;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("-----"))
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[1]), elements[1], elements[0], GetSourceOrDefault(elements[2]), this));
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }
        protected override async Task<ManagerStatus> LoadManager()
        {
            var result = await CoreTools.Which("pwsh.exe");

            ManagerStatus status = new()
            {
                ExecutablePath = result.Item2,
                Found = result.Item1,
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
                    Arguments = " -Version",
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
