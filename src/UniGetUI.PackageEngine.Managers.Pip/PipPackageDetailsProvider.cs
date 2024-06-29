using System.Diagnostics;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PipManager
{
    internal class PipPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public PipPackageDetailsProvider(Pip manager) : base(manager) { }

        protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
        {
            ManagerClasses.Classes.NativeTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageDetails);

            string JsonString;
            HttpClient client = new(CoreData.GenericHttpClientParameters);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
            JsonString = await client.GetStringAsync($"https://pypi.org/pypi/{details.Package.Id}/json");

            JsonObject? RawInfo = JsonObject.Parse(JsonString) as JsonObject;

            if(RawInfo == null)
            {
                logger.Error($"Can't load package info on manager {Manager.Name}, JsonObject? RawInfo was null");
                logger.Close(1);
                return;
            }

            JsonObject? infoNode = RawInfo["info"] as JsonObject;
            if (infoNode != null)
            {
                try
                {
                    if (infoNode.ContainsKey("author"))
                        details.Author = CoreTools.GetStringOrNull(infoNode["author"]?.ToString());
                }
                catch (Exception ex) { logger.Error("Can't load author: " + ex); }
                try
                {
                    if (infoNode.ContainsKey("home_page"))
                        details.HomepageUrl = CoreTools.GetUriOrNull(infoNode["home_page"]?.ToString());
                }
                catch (Exception ex) { logger.Error("Can't load home_page: " + ex); }
                try
                {
                    if (infoNode.ContainsKey("package_url"))
                        details.ManifestUrl = CoreTools.GetUriOrNull(infoNode["package_url"]?.ToString());
                }
                catch (Exception ex) { logger.Error("Can't load package_url: " + ex); }
                try
                {
                    if (infoNode.ContainsKey("summary"))
                        details.Description = CoreTools.GetStringOrNull(infoNode["summary"]?.ToString());
                }
                catch (Exception ex) { logger.Error("Can't load summary: " + ex); }
                try
                {
                    if (infoNode.ContainsKey("license"))
                        details.License = CoreTools.GetStringOrNull(infoNode["license"]?.ToString());
                }
                catch (Exception ex) { logger.Error("Can't load license: " + ex); }
                try
                {
                    if (infoNode.ContainsKey("maintainer"))
                        details.Publisher = CoreTools.GetStringOrNull(infoNode["maintainer"]?.ToString());
                }
                catch (Exception ex) { logger.Error("Can't load maintainer: " + ex); }
                try
                {
                    if ((infoNode.ContainsKey("classifiers"))
                        && (infoNode["classifiers"] is JsonArray))
                    {
                        List<string> Tags = new();
                        foreach (string? line in infoNode["classifiers"] as JsonArray ?? new())
                            if (line?.Contains("License ::") ?? false)
                                details.License = line.Split("::")[^1].Trim();
                            else if (line?.Contains("Topic ::") ?? false)
                                if (!Tags.Contains(line.Split("::")[^1].Trim()))
                                    Tags.Add(line.Split("::")[^1].Trim());
                        details.Tags = Tags.ToArray();
                    }
                }
                catch (Exception ex) { logger.Error("Can't load classifiers: " + ex); }
            }

            try
            {
                JsonObject? url = null;
                if (RawInfo.ContainsKey("url"))

                    url = RawInfo["url"] as JsonObject;
                else if (RawInfo.ContainsKey("urls"))
                    url = (RawInfo["urls"] as JsonArray)?[0] as JsonObject;

                if (url != null)
                {
                    if (url.ContainsKey("digests") && ((url["digests"] as JsonObject)?.ContainsKey("sha256") ?? false))
                    {
                        details.InstallerHash = url["digests"]?["sha256"]?.ToString();
                    }
                    if (url.ContainsKey("url"))
                    {
                        details.InstallerType = url["url"]?.ToString().Split('.')[^1].Replace("whl", "Wheel");
                        details.InstallerUrl = CoreTools.GetUriOrNull(url["url"]?.ToString());
                        details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                    }
                }
            }
            catch (Exception ex) { logger.Error("Can't load installer data: " + ex); }

            logger.Close(0);
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
                Arguments = Manager.Properties.ExecutableCallArgs + " index versions " + package.Id,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageVersions, p);
            p.Start();

            string? line;
            string[] result = [];
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.Contains("Available versions:"))
                    result = line.Replace("Available versions:", "").Trim().Split(", ");
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return result;
        }
    }
}
