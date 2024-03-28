using UniGetUI.Core;
using UniGetUI.PackageEngine.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Classes
{

    /// <summary>
    /// Absract class that all managers must implement
    /// </summary>
    public abstract class PackageManager : SingletonBase<PackageManager>
    {
        public ManagerProperties Properties { get; set; } = new();
        public ManagerCapabilities Capabilities { get; set; } = new();
        public ManagerStatus Status { get; set; } = new() { Found = false };
        public string Name { get; set; } = "Unset";
        public static AppTools Tools = AppTools.Instance;
        public ManagerSource MainSource { get; set; }
        public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };
        public bool ManagerReady { get; set; } = false;

        private Dictionary<string, Package> __known_installed_packages = new();
        private Dictionary<string, Package> __known_available_packages = new();
        private Dictionary<string, UpgradablePackage> __known_upgradable_packages = new();

        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            try
            {
                Properties = GetProperties();
                Name = Properties.Name;
                Capabilities = GetCapabilities();
                MainSource = GetMainSource();
                Status = await LoadManager();


                if (this is PackageManagerWithSources && Status.Found)
                {
                    (this as PackageManagerWithSources).KnownSources = (this as PackageManagerWithSources).GetKnownSources();

                    Task<ManagerSource[]> SourcesTask = (this as PackageManagerWithSources).GetSources();
                    Task winner = await Task.WhenAny(
                        SourcesTask,
                        Task.Delay(10000));
                    if (winner == SourcesTask)
                    {
                        ManagerReady = true;
                        (this as PackageManagerWithSources).Sources = SourcesTask.Result;
                    }
                    else
                    {
                        ManagerReady = true;
                        AppTools.Log(Name + " sources took too long to load, using known sources as default");
                        (this as PackageManagerWithSources).Sources = (this as PackageManagerWithSources).KnownSources;
                    }
                }
                ManagerReady = true;

                string LogData = "\n▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄" +
                                 "\n█▀▀▀▀▀▀▀▀▀▀▀▀▀ MANAGER LOADED ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀" +
                                 "\n█ Name: " + Name +
                                 "\n█ Enabled: " + IsEnabled().ToString() +
                                    ((IsEnabled())? (
                                 "\n█ Found: " + Status.Found.ToString() +
                                    ((Status.Found)? (
                                 "\n█ Fancye exe name: " + Properties.ExecutableFriendlyName +
                                 "\n█ Executable path: " + Status.ExecutablePath +
                                 "\n█ Call arguments: " + Properties.ExecutableCallArgs +
                                 "\n█ Version: \n" + "█   " + Status.Version.Replace("\n", "\n█   "))
                                    :
                                 "\n█ THE MANAGER WAS NOT FOUND. PERHAPS IT IS NOT " + 
                                 "\n█ INSTALLED OR IT HAS BEEN MISCONFIGURED "
                                    ))
                                    : 
                                 "\n█ THE MANAGER IS DISABLED"
                                    ) +
                                 "\n▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀\n";
                
                AppTools.Log(LogData);
            }
            catch (Exception e)
            {
                ManagerReady = true; // We need this to unblock the main thread
                AppTools.Log("Could not initialize Package Manager " + Name + ": \n" + e.ToString());
            }
        }

        /// <summary>
        /// Returns a ManagerProperties object representing the properties of the package manager
        /// </summary>
        /// <returns></returns>
        protected abstract ManagerProperties GetProperties();
        /// <summary>
        /// Returns a ManagerCapabilities object representing the capabilities of the package manager
        /// </summary>
        /// <returns></returns>
        protected abstract ManagerCapabilities GetCapabilities();
        /// <summary>
        /// Returns a ManagerStatus object representing the current status of the package manager. This method runs asynchronously.
        /// </summary>
        /// <returns></returns>
        protected abstract Task<ManagerStatus> LoadManager();

        /// <summary>
        /// Returns true if the manager is enabled, false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled()
        {
            return !Tools.GetSettings("Disable" + Name);
        }

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may 
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<Package[]> FindPackages(string query)
        {
            try
            {
                var packages = await FindPackages_UnSafe(query);
                for (int i = 0; i < packages.Length; i++)
                {
                    if (!__known_available_packages.ContainsKey(packages[i].GetHash()))
                        __known_available_packages.Add(packages[i].GetHash(), packages[i]);
                    else
                    {
                        packages[i] = __known_available_packages[packages[i].GetHash()];
                    }
                }
                return packages;
            }
            catch (Exception e)
            {
                AppTools.Log("Error finding packages on manager " + Name + " with query " + query + ": \n" + e.ToString());
                return new Package[] { };
            }
        }

        /// <summary>
        /// Returns an array of UpgradablePackage objects that represent the available updates reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<UpgradablePackage[]> GetAvailableUpdates()
        {
            try
            {
                var packages = await GetAvailableUpdates_UnSafe();
                for (int i = 0; i < packages.Length; i++)
                {
                    if (!__known_upgradable_packages.ContainsKey(packages[i].GetHash()))
                        __known_upgradable_packages.Add(packages[i].GetHash(), packages[i]);
                    else
                    {
                        packages[i] = __known_upgradable_packages[packages[i].GetHash()];
                    }
                }
                return packages;
            }
            catch (Exception e)
            {
                AppTools.Log("Error finding updates on manager " + Name + ": \n" + e.ToString());
                return new UpgradablePackage[] { };
            }
        }

        /// <summary>
        /// Returns an array of Package objects that represent the installed reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <returns></returns>
        public async Task<Package[]> GetInstalledPackages()
        {
            try
            {
                var packages = await GetInstalledPackages_UnSafe();
                for (int i = 0; i < packages.Length; i++)
                {
                    if (!__known_installed_packages.ContainsKey(packages[i].GetHash()))
                        __known_installed_packages.Add(packages[i].GetHash(), packages[i]);
                    else
                    {
                        packages[i] = __known_installed_packages[packages[i].GetHash()];
                    }
                }
                return packages;
            }
            catch (Exception e)
            {
                AppTools.Log("Error finding installed packages on manager " + Name + ": \n" + e.ToString());
                return new Package[] { };
            }
        }

        /// <summary>
        /// Returns a PackageDetails object that represents the details for the given Package object.
        /// This method is fail-safe and will return a valid but empty PackageDetails object with the package 
        /// id if an error occurs.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public async Task<PackageDetails> GetPackageDetails(Package package)
        {
            try
            {
                return await GetPackageDetails_UnSafe(package);
            }
            catch (Exception e)
            {
                AppTools.Log("Error getting package details on manager " + Name + " for package id=" + package.Id + ": \n" + e.ToString());
                return new PackageDetails(package);
            }
        }

        /// <summary>
        /// Returns the available versions to install for the given package. 
        /// If the manager does not support listing the versions, an empty array will be returned.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="package">The package from which to load its versions</param>
        /// <returns>An array of stings containing the found versions, an empty array if none.</returns>
        public async Task<string[]> GetPackageVersions(Package package)
        {
            try
            {
                if (package.Manager.Capabilities.SupportsCustomVersions)
                    return await GetPackageVersions_Unsafe(package);
                else
                    return new string[0];
            }
            catch (Exception e)
            {
                AppTools.Log("Error getting package versions on manager " + Name + " for package id=" + package.Id + ": \n" + e.ToString());
                return new string[0];
            }
        }

        /// <summary>
        /// Returns the available versions to install for the given package. 
        /// If the manager does not support listing the versions, an empty array must be returned.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package from which to load its versions</param>
        /// <returns>An array of stings containing the found versions, an empty array if none.</returns>
        protected abstract Task<string[]> GetPackageVersions_Unsafe(Package package);

        /// <summary>
        /// Returns the available packages to install for the given query.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="query">The query string to search for</param>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<Package[]> FindPackages_UnSafe(string query);

        /// <summary>
        /// Returns the available updates reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of UpgradablePackage objects</returns>
        protected abstract Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe();

        /// <summary>
        /// Returns an array of Package objects containing the installed packages reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<Package[]> GetInstalledPackages_UnSafe();

        /// <summary>
        /// Returns the specific details and info for the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package for which to load the details</param>
        /// <returns>A PackageDetails with the package details loaded</returns>
        public abstract Task<PackageDetails> GetPackageDetails_UnSafe(Package package);


        /// <summary>
        /// Returns the command-line parameters to install the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be installed</param>
        /// <param name="options">The options in which it is going to be installed</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetInstallParameters(Package package, InstallationOptions options);


        /// <summary>
        /// Returns the command-line parameters to update the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be updated</param>
        /// <param name="options">The options in which it is going to be updated</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetUpdateParameters(Package package, InstallationOptions options);

        /// <summary>
        /// Returns the command-line parameters to uninstall the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be uninstalled</param>
        /// <param name="options">The options in which it is going to be uninstalled</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetUninstallParameters(Package package, InstallationOptions options);

        /// <summary>
        /// Decides and returns the verdict of the install operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was installed</param>
        /// <param name="options">The options with which the package was installed. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the installation</returns>
        public abstract OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);


        /// <summary>
        /// Decides and returns the verdict of the update operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was updated</param>
        /// <param name="options">The options with which the package was updated. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the update</returns>
        public abstract OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);

        /// <summary>
        /// Decides and returns the verdict of the uninstall operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was uninstalled</param>
        /// <param name="options">The options with which the package was uninstalled. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the uninstall</returns>
        public abstract OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);

        /// <summary>
        /// Refreshes the Package Manager sources/indexes
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns></returns>
        public abstract Task RefreshSources();

        /// <summary>
        /// Returns the main source for the manager, even if the manager does not support custom sources.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>A ManagerSource object representing the main source/index</returns>
        public abstract ManagerSource GetMainSource();
    }

    public abstract class PackageManagerWithSources : PackageManager
    {
        public ManagerSource[] Sources { get; set; }
        public ManagerSource[] KnownSources { get; set; }

        public Dictionary<string, ManagerSource> SourceReference = new();
        public virtual async Task<ManagerSource[]> GetSources()
        {
            try
            {
                ManagerSource[] sources = await GetSources_UnSafe();
                SourceReference.Clear();
                foreach (ManagerSource source in sources)
                    SourceReference.Add(source.Name, source);
                return sources;
            }
            catch (Exception e)
            {
                AppTools.Log("Error finding sources for manager " + Name + ": \n" + e.ToString());
                return new ManagerSource[] { };
            }
        }

        public abstract ManagerSource[] GetKnownSources();
        public abstract string[] GetAddSourceParameters(ManagerSource source);
        public abstract string[] GetRemoveSourceParameters(ManagerSource source);
        public abstract OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);
        
        public abstract OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);
        protected abstract Task<ManagerSource[]> GetSources_UnSafe();
    }

    public class ManagerStatus
    {
        public string Version = "";
        public bool Found = false;
        public string ExecutablePath = "";
        public ManagerStatus()
        { }

    }
    public class ManagerProperties
    {
        public string Name { get; set; } = "Unset";
        public string Description { get; set; } = "Unset";
        public string IconId { get; set; } = "Unset";
        public string ColorIconId { get; set; } = "Unset";
        public string ExecutableCallArgs { get; set; } = "Unset";
        public string ExecutableFriendlyName { get; set; } = "Unset";
        public string InstallVerb { get; set; } = "Unset";
        public string UpdateVerb { get; set; } = "Unset";
        public string UninstallVerb { get; set; } = "Unset";

    }
    public struct ManagerCapabilities
    {
        public bool CanRunAsAdmin = false;
        public bool CanSkipIntegrityChecks = false;
        public bool CanRunInteractively = false;
        public bool CanRemoveDataOnUninstall = false;
        public bool SupportsCustomVersions = false;
        public bool SupportsCustomArchitectures = false;
        public Architecture[] SupportedCustomArchitectures = new Architecture[0];
        public bool SupportsCustomScopes = false;
        public bool SupportsPreRelease = false;
        public bool SupportsCustomLocations = false;
        public bool SupportsCustomSources = false;
        public ManagerSource.Capabilities Sources { get; set; }
        public ManagerCapabilities()
        {
            Sources = new ManagerSource.Capabilities();
        }
    }

    public class ManagerSource
    {
        public virtual string IconId { get { return Manager.Properties.IconId; } }
        public bool IsVirtualManager = false;
        public struct Capabilities
        {
            public bool KnowsUpdateDate { get; set; } = false;
            public bool KnowsPackageCount { get; set; } = false;
            public bool MustBeInstalledAsAdmin { get; set; } = false;
            public Capabilities()
            { }
        }

        public PackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; }
        public int? PackageCount { get; }
        public string UpdateDate { get; }

        public ManagerSource(PackageManager manager, string name, Uri url = null, int? packageCount = 0, string updateDate = null)
        {
            Manager = manager;
            Name = name;
            Url = url;
            if (manager.Capabilities.Sources.KnowsPackageCount)
                PackageCount = packageCount;
            if (manager.Capabilities.Sources.KnowsUpdateDate)
                UpdateDate = updateDate;
        }

        public override string ToString()
        {
            if (Manager.Capabilities.SupportsCustomSources)
                return Manager.Name + ": " + Name;
            else
                return Manager.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is not ManagerSource)
                return false;
            else
                return (obj as ManagerSource).Manager == Manager && (obj as ManagerSource).Name == Name && (obj as ManagerSource).Url == Url;
        }
    }
}
