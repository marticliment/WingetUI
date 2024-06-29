using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{
    internal class ScoopSourceProvider : BaseSourceProvider<PackageManager>
    {
        public ScoopSourceProvider(Scoop manager) : base(manager) { }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            return new string[] { "bucket", "add", source.Name, source.Url.ToString() };
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return new string[] { "bucket", "rm", source.Name };
        }

        protected override async Task<IManagerSource[]> GetSources_UnSafe()
        {
            using (Process p = new())
            {
                p.StartInfo.FileName = Manager.Status.ExecutablePath;
                p.StartInfo.Arguments = Manager.Properties.ExecutableCallArgs + " bucket list";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
                p.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                ManagerClasses.Classes.ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListSources, p);

                List<ManagerSource> sources = new();

                p.Start();

                bool DashesPassed = false;

                string? line;
                while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                {
                    logger.AddToStdOut(line);
                    try
                    {
                        if (!DashesPassed)
                        {
                            if (line.Contains("---"))
                                DashesPassed = true;
                        }
                        else if (line.Trim() != "")
                        {
                            string[] elements = Regex.Replace(line.Replace("AM", "").Replace("am", "").Replace("PM", "").Replace("pm", "").Trim(), " {2,}", " ").Split(' ');
                            if (elements.Length >= 5)
                            {
                                if (!elements[1].Contains("https://"))
                                    elements[1] = "https://scoop.sh/"; // If the URI is invalid, we'll use the main website
                                sources.Add(new ManagerSource(Manager, elements[0].Trim(), new Uri(elements[1].Trim()), int.Parse(elements[4].Trim()), elements[2].Trim() + " " + elements[3].Trim()));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e);
                    }
                }
                logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
                await p.WaitForExitAsync();
                logger.Close(p.ExitCode);
                
                return sources.ToArray();
            }
        }
    }
}
