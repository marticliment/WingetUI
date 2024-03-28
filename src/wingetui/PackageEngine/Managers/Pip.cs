using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModernWindow.PackageEngine.Managers
{
    public class Pip : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "", "WARNING:", "[notice]", "Package", "DEPRECATION:" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "", "WARNING:", "[notice]", "Package", "DEPRECATION:" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "Ignoring", "invalid" };
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = new();

            string path = await Tools.Which("parse_pip_search");
            if (!File.Exists(path))
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = Properties.ExecutableCallArgs + " install parse_pip_search",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                proc.Start();
                await proc.WaitForExitAsync();
                path = "parse_pip_search.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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

            p.Start();

            string line;
            bool DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = line.Split('|');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this, scope: PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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

            p.Start();

            string line;
            bool DashesPassed = false;
            List<UpgradablePackage> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new UpgradablePackage(Tools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], MainSource, this, scope: PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {

            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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

            p.Start();

            string line;
            bool DashesPassed = false;
            List<Package> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(Tools.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this, scope: PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (output_string.Contains("--user") && package.Scope == PackageScope.Global)
            {
                package.Scope = PackageScope.User;
                return OperationVeredict.AutoRetry;
            }
            else
                return OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetUpdateParameters(package, options);
            parameters[0] = Properties.InstallVerb;
            return parameters;
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;
            parameters.Remove("--yes");

            if (options.PreRelease)
                parameters.Add("--pre");

            if (options.InstallationScope == PackageScope.User)
                parameters.Add("--user");

            if (options.Version != "")
                parameters[1] = package.Id + "==" + options.Version;


            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id, "--yes", "--no-input", "--no-color", "--no-python-version-warning", "--no-cache" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            return parameters.ToArray();
        }
        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "pip", new Uri("https://pypi.org/"));
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);

            AppTools.Log("Getting package details for " + package.Id);

            string JsonString;
            HttpClient client = new();
            JsonString = await client.GetStringAsync($"https://pypi.org/pypi/{package.Id}/json");

            JsonObject RawInfo = JsonObject.Parse(JsonString) as JsonObject;

            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("author"))
                    details.Author = (RawInfo["info"] as JsonObject)["author"].ToString();
            }
            catch (Exception ex) { AppTools.Log("Can't load author: " + ex); }

            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("home_page"))
                    details.HomepageUrl = new Uri((RawInfo["info"] as JsonObject)["home_page"].ToString());
            }
            catch (Exception ex) { AppTools.Log("Can't load home_page: " + ex); }
            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("package_url"))
                    details.ManifestUrl = new Uri((RawInfo["info"] as JsonObject)["package_url"].ToString());
            }
            catch (Exception ex) { AppTools.Log("Can't load package_url: " + ex); }
            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("summary"))
                    details.Description = (RawInfo["info"] as JsonObject)["summary"].ToString();
            }
            catch (Exception ex) { AppTools.Log("Can't load summary: " + ex); }
            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("license"))
                    details.License = (RawInfo["info"] as JsonObject)["license"].ToString();
            }
            catch (Exception ex) { AppTools.Log("Can't load license: " + ex); }
            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("maintainer"))
                    details.Publisher = (RawInfo["info"] as JsonObject)["maintainer"].ToString();
            }
            catch (Exception ex) { AppTools.Log("Can't load maintainer: " + ex); }
            try
            {
                if ((RawInfo["info"] as JsonObject).ContainsKey("classifiers") && (RawInfo["info"] as JsonObject)["classifiers"] is JsonArray)
                {
                    List<string> Tags = new();
                    foreach (string line in (RawInfo["info"] as JsonObject)["classifiers"] as JsonArray)
                        if (line.Contains("License ::"))
                            details.License = line.Split("::")[^1].Trim();
                        else if (line.Contains("Topic ::"))
                            if (!Tags.Contains(line.Split("::")[^1].Trim()))
                                Tags.Add(line.Split("::")[^1].Trim());
                    details.Tags = Tags.ToArray();
                }
            }
            catch (Exception ex) { AppTools.Log("Can't load classifiers: " + ex); }

            try
            {
                JsonObject? url = null;
                if (RawInfo.ContainsKey("url"))

                    url = RawInfo["url"] as JsonObject;
                else if (RawInfo.ContainsKey("urls"))
                    url = (RawInfo["urls"] as JsonArray)[0] as JsonObject;

                if (url != null)
                {
                    if (url.ContainsKey("digests") && (url["digests"] as JsonObject).ContainsKey("sha256"))
                    {
                        details.InstallerHash = url["digests"]["sha256"].ToString();
                    }
                    if (url.ContainsKey("url"))
                    {
                        details.InstallerType = url["url"].ToString().Split('.')[^1].Replace("whl", "Wheel");
                        details.InstallerUrl = new Uri(url["url"].ToString());
                        details.InstallerSize = await Tools.GetFileSizeAsync(details.InstallerUrl);
                    }
                }
            }
            catch (Exception ex) { AppTools.Log("Can't load installer data: " + ex); }

            return details;
        }

#pragma warning disable CS1998
        public override async Task RefreshPackageIndexes()
        {
            // Pip does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new()
            {
                Name = "Pip",
                Description = Tools.Translate("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"),
                IconId = "python",
                ColorIconId = "pip_color",
                ExecutableFriendlyName = "pip",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install --upgrade",
                ExecutableCallArgs = " -m pip",

            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            status.ExecutablePath = await Tools.Which("python.exe");
            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
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

            if (status.Found && IsEnabled())
                await RefreshPackageIndexes();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " index versions " + package.Id,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            p.Start();

            string line;
            string[] result = new string[0];
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (line.Contains("Available versions:"))
                    result = line.Replace("Available versions:", "").Trim().Split(", ");
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            return result;
        }
    }
}
