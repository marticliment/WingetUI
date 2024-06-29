using System.Diagnostics;
using System.Text.Json.Nodes;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{
    internal class ScoopPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public ScoopPackageDetailsProvider(Scoop manager) : base(manager) { }

        protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
        {
            if (details.Package.Source.Url != null)
                try
                {
                    details.ManifestUrl = new Uri(details.Package.Source.Url.ToString() + "/blob/master/bucket/" + details.Package.Id + ".json");
                }
                catch (Exception ex)
                {
                    Logger.Error("Cannot load package manifest URL");
                    Logger.Error(ex);
                }

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " cat " + details.Package.Source.Name + "/" + details.Package.Id,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };

            ProcessTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageDetails, p);

            p.Start();
            string JsonString = await p.StandardOutput.ReadToEndAsync();
            logger.AddToStdOut(JsonString);
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());

            JsonObject? RawInfo = JsonObject.Parse(JsonString) as JsonObject;

            if(RawInfo == null)
            {
                throw new Exception("Deserialized RawInfo was null");
            }

            try
            {
                if (RawInfo.ContainsKey("description") && (RawInfo["description"] is JsonArray))
                {
                    details.Description = "";
                    foreach (JsonNode? note in RawInfo["description"] as JsonArray ?? new())
                        details.Description += note?.ToString() + "\n";
                    details.Description = details.Description.Replace("\n\n", "\n").Trim();
                }
                else if (RawInfo.ContainsKey("description"))
                    details.Description = CoreTools.GetStringOrNull(RawInfo["description"]?.ToString());
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load description: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("innosetup"))
                    details.InstallerType = "Inno Setup (" + CoreTools.Translate("extracted") + ")";
                else
                    details.InstallerType = CoreTools.Translate("Scoop package");
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load installer type: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("homepage"))
                {
                    details.HomepageUrl = CoreTools.GetUriOrNull(RawInfo["homepage"]?.ToString());
                    if (details.HomepageUrl?.ToString().Contains("https://github.com/") ?? false)
                        details.Author = details.HomepageUrl.ToString().Replace("https://github.com/", "").Split("/")[0];
                    else
                        details.Author = details.HomepageUrl?.Host.Split(".")[^2];
                }
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load homepage: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("notes") && (RawInfo["notes"] is JsonArray))
                {
                    details.ReleaseNotes = "";
                    foreach (JsonNode? note in RawInfo["notes"] as JsonArray ?? new())
                        details.ReleaseNotes += note?.ToString() + "\n";
                    details.ReleaseNotes = details.ReleaseNotes.Replace("\n\n", "\n").Trim();
                }
                else if (RawInfo.ContainsKey("notes"))
                    details.ReleaseNotes = RawInfo["notes"]?.ToString();
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load notes: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("license"))
                {
                    if (RawInfo["license"] is not JsonValue)
                    {
                        details.License = RawInfo["license"]?["identifier"]?.ToString();
                        details.LicenseUrl = CoreTools.GetUriOrNull(RawInfo["license"]?["url"]?.ToString());
                    }
                    else
                        details.License = RawInfo["license"]?.ToString();
                }
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load license: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("url") && RawInfo.ContainsKey("hash"))
                {
                    if (RawInfo["url"] is JsonArray)
                        details.InstallerUrl = CoreTools.GetUriOrNull(RawInfo["url"]?[0]?.ToString());
                    else
                        details.InstallerUrl = CoreTools.GetUriOrNull(RawInfo["url"]?.ToString());

                    if (RawInfo["hash"] is JsonArray)
                        details.InstallerHash = RawInfo["hash"]?[0]?.ToString();
                    else
                        details.InstallerHash = RawInfo["hash"]?.ToString();
                }
                else if (RawInfo.ContainsKey("architecture"))
                {
                    string FirstArch = (RawInfo["architecture"] as JsonObject)?.ElementAt(0).Key ?? "";
                    details.InstallerHash = RawInfo["architecture"]?[FirstArch]?["hash"]?.ToString();
                    details.InstallerUrl = CoreTools.GetUriOrNull(RawInfo["architecture"]?[FirstArch]?["url"]?.ToString());
                }

                details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load installer URL: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("checkver") && RawInfo["checkver"] is JsonObject && ((RawInfo["checkver"] as JsonObject)?.ContainsKey("url") ?? false))
                    details.ReleaseNotesUrl = CoreTools.GetUriOrNull(RawInfo["checkver"]?["url"]?.ToString() ?? "");
            }
            catch (Exception ex) { logger.AddToStdErr("Can't load notes URL: " + ex); }

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

#pragma warning disable
        protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
        {
            throw new Exception("Scoop does not support custom package versions");
        }
    }
#pragma warning enable
}
