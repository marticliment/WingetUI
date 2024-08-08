using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    public class WinGet : PackageManager
    {
        public static new string[] FALSE_PACKAGE_NAMES = ["", "e(s)", "have", "the", "Id"];
        public static new string[] FALSE_PACKAGE_IDS = ["", "e(s)", "have", "an", "'winget", "pin'", "have", "an", "Version"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["", "have", "an", "'winget", "pin'", "have", "an", "Version"];
        public LocalWinGetSource LocalPcSource { get; }
        public LocalWinGetSource AndroidSubsystemSource { get; }
        public LocalWinGetSource SteamSource { get; }
        public LocalWinGetSource UbisoftConnectSource { get; }
        public LocalWinGetSource GOGSource { get; }
        public LocalWinGetSource MicrosoftStoreSource { get; }

        public readonly string PowerShellPath;
        public readonly string PowerShellPromptArgs;
        public readonly string PowerShellInlineArgs;
        public string WinGetBundledPath;

        public WinGet()
        {
            PowerShellPath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe");
            PowerShellPromptArgs = "-ExecutionPolicy Bypass -NoLogo -NoProfile";
            PowerShellInlineArgs = "-ExecutionPolicy Bypass -NoLogo -NoProfile -NonInteractive";

            WinGetBundledPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "winget-cli_x64", "winget.exe");

            Dependencies = [
                new ManagerDependency(
                    "WinGet PowerShell Module",
                    PowerShellPath,
                    PowerShellPromptArgs + " -Command \"& {Install-Module -Name Microsoft.WinGet.Client -Force -Confirm:$false -Scope CurrentUser; if($error.count -ne 0){pause}}\"",
                    "Install-Module -Name Microsoft.WinGet.Client -Scope CurrentUser",
                    async () =>
                    {
                        if (!Settings.Get("ForceUsePowerShellModules") || Settings.Get("ForceLegacyBundledWinGet"))
                        {
                            Logger.ImportantInfo("Microsoft.Powershell.Client detection has been forcefully skipped as the module is not required on the current context");
                            return true;
                        }

                        Process p = new()
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = PowerShellPath,
                                Arguments = PowerShellPromptArgs,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            },
                        };
                        p.Start();
                        await p.StandardInput.WriteAsync("if(Get-Module -ListAvailable -Name \"Microsoft.WinGet.Client\"){exit 0}Else{exit 1}");
                        p.StandardInput.Close();
                        await p.WaitForExitAsync();
                        return p.ExitCode == 0;
                     })
            ];

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.X86, Architecture.X64, Architecture.Arm64],
                SupportsCustomScopes = true,
                SupportsCustomLocations = true,
                SupportsCustomSources = true,
                SupportsCustomPackageIcons = true,
                SupportsCustomPackageScreenshots = true,
                Sources = new SourceCapabilities
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = true,
                    MustBeInstalledAsAdmin = true,
                }
            };

            Properties = new ManagerProperties
            {
                Name = "Winget",
                DisplayName = "WinGet",
                Description = CoreTools.Translate("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"),
                IconId = IconType.WinGet,
                ColorIconId = "winget_color",
                ExecutableFriendlyName = "winget.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
                KnownSources = [ new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache")),
                                 new ManagerSource(this, "msstore", new Uri("https://storeedgefd.dsx.mp.microsoft.com/v9.0")) ],
                DefaultSource = new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache"))
            };

            SourceProvider = new WinGetSourceProvider(this);
            PackageDetailsProvider = new WinGetPackageDetailsProvider(this);
            OperationProvider = new WinGetOperationProvider(this);

            LocalPcSource = new LocalWinGetSource(this, CoreTools.Translate("Local PC"), IconType.LocalPc);
            AndroidSubsystemSource = new(this, CoreTools.Translate("Android Subsystem"), IconType.Android);
            SteamSource = new(this, "Steam", IconType.Steam);
            UbisoftConnectSource = new(this, "Ubisoft Connect", IconType.UPlay);
            GOGSource = new(this, "GOG", IconType.GOG);
            MicrosoftStoreSource = new(this, "Microsoft Store", IconType.MsStore);
        }

        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            return await Task.Run(() => WinGetHelper.Instance.FindPackages_UnSafe(this, query).GetAwaiter().GetResult());
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            return await Task.Run(() => WinGetHelper.Instance.GetAvailableUpdates_UnSafe(this).GetAwaiter().GetResult());
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            return await Task.Run(() => WinGetHelper.Instance.GetInstalledPackages_UnSafe(this).GetAwaiter().GetResult());
        }

        public ManagerSource GetLocalSource(string id)
        {
            var IdPieces = id.Split('\\');
            if (IdPieces[0] == "MSIX")
            {
                return MicrosoftStoreSource;
            }
            else
            {
                string MeaningfulId = IdPieces[^1];

                // Fast Local PC Check
                if (MeaningfulId[0] == '{')
                {
                    return LocalPcSource;
                }

                // Check if source is android
                if(MeaningfulId.Count(x => x == '.') >= 2 && MeaningfulId.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.'))
                {
                    return AndroidSubsystemSource;
                }

                // Check if source is Steam
                if (MeaningfulId == "Steam" || MeaningfulId.StartsWith("Steam App"))
                {
                    return SteamSource;
                }

                // Check if source is Ubisoft Connect
                if (MeaningfulId == "Uplay" || MeaningfulId.StartsWith("Uplay Install"))
                {
                    return UbisoftConnectSource;
                }

                // Check if source is GOG
                if (MeaningfulId.EndsWith("_is1") &&
                    MeaningfulId.Replace("_is1", "").All(c => (c >= '0' && c <= '9')))
                {
                    return GOGSource;
                }

                // Otherwise they are Local PC
                return LocalPcSource;
            }
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            bool FORCE_BUNDLED = Settings.Get("ForceLegacyBundledWinGet");

            Tuple<bool, string> which_res = await CoreTools.Which("winget.exe");
            status.ExecutablePath = which_res.Item2;
            status.Found = which_res.Item1;

            if (!status.Found)
            {
                Logger.Error("User does not have WinGet installed");
                FORCE_BUNDLED = true;
            }

            if (FORCE_BUNDLED)
            {
                status.ExecutablePath = WinGetBundledPath;
                status.Found = File.Exists(WinGetBundledPath);
            }

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
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            process.Start();
            status.Version = $"{(FORCE_BUNDLED ? "Bundled" : "System")} WinGet CLI Version: {(await process.StandardOutput.ReadToEndAsync()).Trim()}";
            string error = await process.StandardError.ReadToEndAsync();
            if (error != "")
            {
                Logger.Error("WinGet STDERR not empty: " + error);
            }

            process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PowerShellPath,
                    Arguments = PowerShellInlineArgs + " -Command Write-Output (Get-Module -Name Microsoft.WinGet.Client).Version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version += $"\nMicrosoft.WinGet.Client PSModule version: \"{(await process.StandardOutput.ReadToEndAsync()).Trim()}\"";
            error = await process.StandardError.ReadToEndAsync();
            if (error != "")
            {
                Logger.Error("WinGet STDERR not empty: " + error);
            }

            try
            {
                if (FORCE_BUNDLED)
                {
                    throw new InvalidOperationException("Bundled WinGet was forced by the user!");
                }

                await Task.Run(() => WinGetHelper.Instance = new NativeWinGetHelper());
                status.Version += "\nUsing Native WinGet helper (COM Api)";
            }
            catch (Exception ex)
            {
                Logger.Warn($"Cannot create native WinGet instance due to error: {ex.Message}");
                Logger.Warn(ex);
                Logger.Warn("WinGet will resort to using BundledWinGetHelper()");
                WinGetHelper.Instance = new BundledWinGetHelper();
                status.Version += "\nUsing bundled WinGet helper (CLI parsing)";
            }

            return status;
        }

        public override async Task RefreshPackageIndexes()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " source update --disable-interactivity",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.RefreshIndexes, p);

            p.Start();
            logger.AddToStdOut(await p.StandardOutput.ReadToEndAsync());
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            logger.Close(p.ExitCode);
            await p.WaitForExitAsync();
            p.Close();
        }
    }

    public class LocalWinGetSource : ManagerSource
    {
        private readonly string name;
        private readonly IconType __icon_id;
        public override IconType IconId { get => __icon_id; }

        public LocalWinGetSource(WinGet manager, string name, IconType iconId)
            : base(manager, name, new Uri("https://microsoft.com/local-pc-source"), isVirtualManager: true)
        {
            this.name = name;
            __icon_id = iconId;
            AsString = Name;
            AsString_DisplayName = Name;
        }
    }
}
