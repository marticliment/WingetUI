using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PipManager
{
    public class Pip : PackageManager
    {
        public static new string[] FALSE_PACKAGE_NAMES = ["", "WARNING:", "[notice]", "Package", "DEPRECATION:"];
        public static new string[] FALSE_PACKAGE_IDS = ["", "WARNING:", "[notice]", "Package", "DEPRECATION:"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["", "Ignoring", "invalid"];

        public Pip()
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
                Name = "Pip",
                Description = CoreTools.Translate("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"),
                IconId = IconType.Python,
                ColorIconId = "pip_color",
                ExecutableFriendlyName = "pip",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install --upgrade",
                ExecutableCallArgs = " -m pip",
                DefaultSource = new ManagerSource(this, "pip", new Uri("https://pypi.org/")),
                KnownSources = [new ManagerSource(this, "pip", new Uri("https://pypi.org/"))],

            };

            PackageDetailsProvider = new PipPackageDetailsProvider(this);
            OperationProvider = new PipOperationProvider(this);
        }
        
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = [];

            Tuple<bool, string> which_res = await CoreTools.Which("parse_pip_search.exe");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " install parse_pip_search",
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
                path = "parse_pip_search.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "\"" + query + "\"",
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
                    string[] elements = line.Split('|');
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

                    Packages.Add(new Package(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, new(PackageScope.Global)));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list --outdated",
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this, new(PackageScope.Global)));
                }
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, new(PackageScope.Global)));
                }
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            Tuple<bool, string> which_res = await CoreTools.Which("python.exe");
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

            return status;
        }
    }
}

