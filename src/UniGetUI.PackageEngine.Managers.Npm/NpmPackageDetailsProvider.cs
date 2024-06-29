using System.Diagnostics;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.NpmManager
{
    internal class NpmPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public NpmPackageDetailsProvider(Npm manager) : base(manager) { }

        protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
        {
            try
            {
                details.InstallerType = "Tarball";
                details.ManifestUrl = new Uri($"https://www.npmjs.com/package/{details.Package.Id}");
                details.ReleaseNotesUrl = new Uri($"https://www.npmjs.com/package/{details.Package.Id}?activeTab=versions");

                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo()
                    {
                        FileName = Manager.Status.ExecutablePath,
                        Arguments = Manager.Properties.ExecutableCallArgs + " info " + details.Package.Id,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    };

                    ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageDetails, p);
                    p.Start();

                    string? outLine;
                    int lineNo = 0;
                    bool ReadingMaintainer = false;
                    while ((outLine = await p.StandardOutput.ReadLineAsync()) != null)
                    {
                        try
                        {
                            lineNo++;
                            if (lineNo == 2)
                            {
                                details.License = outLine.Split("|")[1];
                            }
                            else if (lineNo == 3)
                            {
                                details.Description = outLine.Trim();
                            }
                            else if (lineNo == 4)
                            {
                                details.HomepageUrl = new Uri(outLine.Trim());
                            }
                            else if (outLine.StartsWith(".tarball"))
                            {
                                details.InstallerUrl = new Uri(outLine.Replace(".tarball: ", "").Trim());
                                details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                            }
                            else if (outLine.StartsWith(".integrity"))
                            {
                                details.InstallerHash = outLine.Replace(".integrity: sha512-", "").Replace("==", "").Trim();
                            }
                            else if (outLine.StartsWith("maintainers:"))
                            {
                                ReadingMaintainer = true;
                            }
                            else if (ReadingMaintainer)
                            {
                                ReadingMaintainer = false;
                                details.Author = outLine.Replace("-", "").Split('<')[0].Trim();
                            }
                            else if (outLine.StartsWith("published"))
                            {
                                details.Publisher = outLine.Split("by").Last().Split('<')[0].Trim();
                                details.UpdateDate = outLine.Replace("published", "").Split("by")[0].Trim();
                            }
                        }
                        catch (Exception e)
                        {
                            logger.AddToStdErr(e.ToString());
                        }
                    }

                    logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
                    await p.WaitForExitAsync();
                    logger.Close(p.ExitCode);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return;
        }

        protected override Task<CacheableIcon?> GetPackageIcon_Unsafe(IPackage package)
        {
            throw new NotImplementedException();
        }

        protected override Task<Uri[]> GetPackageScreenshots_Unsafe(IPackage package)
        {
            throw new NotImplementedException();
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " show " + package.Id + " versions --json",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageVersions, p);
            p.Start();

            string? line;
            List<string> versions = new();

            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.Contains("\""))
                    versions.Add(line.Trim().TrimStart('"').TrimEnd(',').TrimEnd('"'));
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return versions.ToArray();
        }
    }
}
