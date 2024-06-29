using System.Diagnostics;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class ChocolateyDetailsProvider : BaseNuGetDetailsProvider
    {
        public ChocolateyDetailsProvider(BaseNuGet manager) : base(manager)
        { }

        protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    // choco search php --exact --all-versions
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + $" search {package.Id} --exact --all-versions",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions, p);

            p.Start();

            string? line;
            List<string> versions = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.Contains("[Approved]"))
                    versions.Add(line.Split(' ')[1].Trim());
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return versions.ToArray();
        }
    }
}
