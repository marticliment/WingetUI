using System.Diagnostics;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.ChocolateyManager
{
    internal class ChocolateySourceProvider : BaseSourceProvider<PackageManager>
    {
        public ChocolateySourceProvider(Chocolatey manager) : base(manager) { }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            return new string[] { "source", "add", "--name", source.Name, "--source", source.Url.ToString(), "-y" };
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return new string[] { "source", "remove", "--name", source.Name, "-y" };
        }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        protected override async Task<IManagerSource[]> GetSources_UnSafe()
        {
            List<ManagerSource> sources = new();

            Process p = new();
            p.StartInfo = new()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListSources, p);
            p.Start();

            string? line;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                try
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.Contains(" - ") && line.Contains(" | "))
                    {
                        string[] parts = line.Trim().Split('|')[0].Trim().Split(" - ");
                        if (parts[1].Trim() == "https://community.chocolatey.org/api/v2/")
                            sources.Add(new ManagerSource(Manager, "community", new Uri("https://community.chocolatey.org/api/v2/")));
                        else
                            sources.Add(new ManagerSource(Manager, parts[0].Trim(), new Uri(parts[1].Trim())));
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
            
            return sources.ToArray();
        }
    }
}
