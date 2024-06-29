using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.Core.Tools;

namespace UniGetUI.PackageEngine.ManagerClasses.Manager
{
    public abstract class PackageManager : SingletonBase<PackageManager>, ISourceProvider, IPackageDetailsProvider, IPackageManager
    {
        public ManagerProperties Properties { get; set; } = new(IsDummy: true);
        public ManagerCapabilities Capabilities { get; set; } = new(IsDummy: true);
        public ManagerStatus Status { get; set; } = new() { Found = false };
        public string Name { get; set; } = "Unset";
        public IManagerSource DefaultSource { get; set; }
        public static string[] FALSE_PACKAGE_NAMES = [""];
        public static string[] FALSE_PACKAGE_IDS = [""];
        public static string[] FALSE_PACKAGE_VERSIONS = [""];
        public bool ManagerReady { get; set; } = false;
        public ManagerLogger TaskLogger { get; }

        public ISourceProvider SourceProvider { get; set; }
        public ISourceFactory SourceFactory { get => SourceProvider.SourceFactory; }

        public IPackageDetailsProvider? PackageDetailsProvider { get; set; }
        private readonly bool __base_constructor_called = false;

        public PackageManager()
        {
            DefaultSource = Properties.DefaultSource;
            Name = Properties.Name;
            __base_constructor_called = true;
            TaskLogger = new ManagerLogger(this);
            SourceProvider = new NullSourceProvider(this);
            PackageDetailsProvider = new NullPackageDetailsProvider(this);
        }


        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        /// <returns></returns>
        public virtual async Task InitializeAsync()
        {
            // BEGIN integrity check
            if (!__base_constructor_called)
                throw new Exception($"The Manager {Properties.Name} has not called the base constructor.");
            else if (Capabilities.IsDummy)
                throw new Exception($"The current instance of PackageManager with name ${Properties.Name} does not have a valid Capabilities object");
            else if (Properties.IsDummy)
                throw new Exception($"The current instance of PackageManager with name ${Properties.Name} does not have a valid Properties object");
            else if (Capabilities.SupportsCustomSources && SourceProvider == null)
                throw new Exception($"Manager {Name} has been declared as SupportsCustomSources but has no helper associated with it");
            // END integrity check

            DefaultSource = Properties.DefaultSource;
            Name = Properties.Name;
            try
            {
                Status = await LoadManager();

                if (IsReady() && SourceProvider != null)
                {
                    Task<IManagerSource[]> SourcesTask = GetSources();
                    Task winner = await Task.WhenAny(
                        SourcesTask,
                        Task.Delay(10000));
                    if (winner == SourcesTask)
                    {
                        ManagerReady = true;
                    }
                    else
                    {
                        ManagerReady = true;
                        Logger.Warn(Name + " sources took too long to load, using known sources as default");
                    }
                }
                ManagerReady = true;

                string LogData = "▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄" +
                               "\n█▀▀▀▀▀▀▀▀▀▀▀▀▀ MANAGER LOADED ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀" +
                               "\n█ Name: " + Name +
                               "\n█ Enabled: " + IsEnabled().ToString() +
                               (IsEnabled() ? 
                               "\n█ Found: " + Status.Found.ToString() +
                               (Status.Found ? 
                               "\n█ Fancye exe name: " + Properties.ExecutableFriendlyName +
                               "\n█ Executable path: " + Status.ExecutablePath +
                               "\n█ Call arguments: " + Properties.ExecutableCallArgs +
                               "\n█ Version: \n" + "█   " + Status.Version.Replace("\n", "\n█   ")
                               :
                               "\n█ THE MANAGER WAS NOT FOUND. PERHAPS IT IS NOT " +
                               "\n█ INSTALLED OR IT HAS BEEN MISCONFIGURED "
                               )
                               :
                               "\n█ THE MANAGER IS DISABLED"
                               ) +
                               "\n▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀";

                Logger.Info(LogData);
            }
            catch (Exception e)
            {
                ManagerReady = true; // We need this to unblock the main thread
                Logger.Error("Could not initialize Package Manager " + Name);
                Logger.Error(e);
            }
        }


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
            return !Settings.Get("Disable" + Name);
        }

        /// <summary>
        /// Returns true if the manager is enabled and available (the required executable files were found). Returns false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsReady()
        {
            return IsEnabled() && Status.Found;
        }

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may 
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<IPackage[]> FindPackages(string query)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet FindPackages was called"); return []; };
            try
            {
                IPackage[] packages = await FindPackages_UnSafe(query).WaitAsync(TimeSpan.FromSeconds(60));
                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageCacher.GetAvailablePackage(packages[i]);
                }
                Logger.Info($"Found {packages.Length} available packages from {Name} with the query {query}");
                return packages;
            }
            catch (Exception e)
            {
                Logger.Error("Error finding packages on manager " + Name + " with query " + query);
                Logger.Error(e);
                return [];
            }
        }

        /// <summary>
        /// Returns an array of UpgradablePackage objects that represent the available updates reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<IPackage[]> GetAvailableUpdates()
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetAvailableUpdates was called"); return []; };
            try
            {
                await RefreshPackageIndexes().WaitAsync(TimeSpan.FromSeconds(60));
                IPackage[] packages = await GetAvailableUpdates_UnSafe().WaitAsync(TimeSpan.FromSeconds(60));
                for (int i = 0; i < packages.Length; i++)
                    packages[i] = PackageCacher.GetUpgradablePackage(packages[i]);
                Logger.Info($"Found {packages.Length} available updates from {Name}");
                return packages;
            }
            catch (Exception e)
            {
                Logger.Error("Error finding updates on manager " + Name);
                Logger.Error(e);
                return [];
            }
        }

        /// <summary>
        /// Returns an array of Package objects that represent the installed reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <returns></returns>
        public async Task<IPackage[]> GetInstalledPackages()
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetInstalledPackages was called"); return []; };
            try
            {
                IPackage[] packages = await GetInstalledPackages_UnSafe().WaitAsync(TimeSpan.FromSeconds(60));
                for (int i = 0; i < packages.Length; i++)
                    packages[i] = PackageCacher.GetInstalledPackage(packages[i]);
                Logger.Info($"Found {packages.Length} installed packages from {Name}");
                return packages;
            }
            catch (Exception e)
            {
                Logger.Error("Error finding installed packages on manager " + Name);
                Logger.Error(e); 
                return [];
            }
        }


        /// <summary>
        /// Returns the available packages to install for the given query.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="query">The query string to search for</param>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<IPackage[]> FindPackages_UnSafe(string query);

        /// <summary>
        /// Returns the available updates reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of UpgradablePackage objects</returns>
        protected abstract Task<IPackage[]> GetAvailableUpdates_UnSafe();

        /// <summary>
        /// Returns an array of Package objects containing the installed packages reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<IPackage[]> GetInstalledPackages_UnSafe();


        /// <summary>
        /// Returns the command-line parameters to install the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be installed</param>
        /// <param name="options">The options in which it is going to be installed</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetInstallParameters(IPackage package, IInstallationOptions options);


        /// <summary>
        /// Returns the command-line parameters to update the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be updated</param>
        /// <param name="options">The options in which it is going to be updated</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetUpdateParameters(IPackage package, IInstallationOptions options);

        /// <summary>
        /// Returns the command-line parameters to uninstall the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be uninstalled</param>
        /// <param name="options">The options in which it is going to be uninstalled</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetUninstallParameters(IPackage package, IInstallationOptions options);

        /// <summary>
        /// Decides and returns the verdict of the install operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was installed</param>
        /// <param name="options">The options with which the package was installed. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the installation</returns>
        public abstract OperationVeredict GetInstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output);


        /// <summary>
        /// Decides and returns the verdict of the update operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was updated</param>
        /// <param name="options">The options with which the package was updated. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the update</returns>
        public abstract OperationVeredict GetUpdateOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output);

        /// <summary>
        /// Decides and returns the verdict of the uninstall operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was uninstalled</param>
        /// <param name="options">The options with which the package was uninstalled. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the uninstall</returns>
        public abstract OperationVeredict GetUninstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output);

        /// <summary>
        /// Refreshes the Package Manager sources/indexes
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns></returns>
#pragma warning disable CS1998
        public virtual async Task RefreshPackageIndexes()
        {
            Logger.Debug($"Manager {Name} has not implemented RefreshPackageIndexes");
        }
#pragma warning restore CS1998

        // BEGIN SOURCE-RELATED METHODS

        /// <summary>
        /// Will check if the Manager supports custom sources, and throw an exception if not
        /// </summary>
        /// <param name="MethodName"></param>
        /// <exception cref="Exception"></exception>
        private void AssertSourceCompatibility(string MethodName)
        {
            if (!Capabilities.SupportsCustomSources)
                throw new Exception($"Manager {Name} does not support custom sources but yet {MethodName} method was called.\n {Environment.StackTrace}");
            else if (SourceProvider == null)
                throw new Exception($"Manager {Name} does support custom sources but yet the source helper is null");
        }
#pragma warning disable CS8602
        public IManagerSource GetSourceOrDefault(string SourceName)
        {
            AssertSourceCompatibility("GetSourceFromName");
            return SourceProvider.SourceFactory.GetSourceOrDefault(SourceName);
        }
        public IManagerSource? GetSourceIfExists(string SourceName)
        {
            AssertSourceCompatibility("GetSourceIfExists");
            return SourceProvider.SourceFactory.GetSourceIfExists(SourceName);
        }
        public string[] GetAddSourceParameters(IManagerSource source)
        {
            AssertSourceCompatibility("GetAddSourceParameters");
            return SourceProvider.GetAddSourceParameters(source);
        }
        public string[] GetRemoveSourceParameters(IManagerSource source)
        {
            AssertSourceCompatibility("GetRemoveSourceParameters");
            return SourceProvider.GetRemoveSourceParameters(source);
        }
        public OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            AssertSourceCompatibility("GetAddSourceOperationVeredict");
            return SourceProvider.GetAddSourceOperationVeredict(source, ReturnCode, Output);
        }
        public OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            AssertSourceCompatibility("GetRemoveSourceOperationVeredict");
            return SourceProvider.GetRemoveSourceOperationVeredict(source, ReturnCode, Output);
        }

        public virtual async Task<IManagerSource[]> GetSources()
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetSources was called"); return []; };
            try
            {
                AssertSourceCompatibility("GetSources");
                IManagerSource[] result = await SourceProvider.GetSources().WaitAsync(TimeSpan.FromSeconds(60));
                Logger.Debug($"Loaded {result.Length} sources for manager {Name}");
                return result;
            }
            catch (Exception e)
            {
                Logger.Error($"Error finding sources for manager " + Name);
                Logger.Error(e);
                return [];
            }
        }
#pragma warning restore CS8602
        // END SOURCE-RELATED METHODS








        // BEGIN PACKAGEDEAILS-RELATED METHODS
        private void AssertPackageDetailsCompatibility(string MethodName)
        {
            if (PackageDetailsProvider == null)
                throw new Exception($"Manager {Name} does not have a valid PackageDetailsProvider helper");
        }
#pragma warning disable CS8602
        /*public async Task<PackageDetails> GetPackageDetails(IPackage package)
        {
            var details = new PackageDetails(package);
            await GetPackageDetails(details);
            return details;
        }*/

        public async Task GetPackageDetails(IPackageDetails details)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetPackageDetails was called"); return; };
            try
            {
                AssertPackageDetailsCompatibility("GetPackageDetails");
                await PackageDetailsProvider.GetPackageDetails(details);
                Logger.Info($"Loaded details for package {details.Package.Id} on manager {Name}");
            }
            catch (Exception e)
            {
                Logger.Error("Error finding installed packages on manager " + Name);
                Logger.Error(e);
            }
        }

        public async Task<string[]> GetPackageVersions(IPackage package)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetPackageVersions was called"); return []; };
            try
            {
                AssertPackageDetailsCompatibility("GetPackageVersions");
                if (package.Manager.Capabilities.SupportsCustomVersions)
                    return await PackageDetailsProvider.GetPackageVersions(package);
                else
                    return [];
            }
            catch (Exception e)
            {
                Logger.Error($"Error finding available package versions for package {package.Id} on manager " + Name);
                Logger.Error(e);
                return [];
            }
        }

        public async Task<CacheableIcon?> GetPackageIconUrl(IPackage package)
        {
            try
            {
                AssertPackageDetailsCompatibility("GetPackageIcon");
                return await PackageDetailsProvider.GetPackageIconUrl(package);
            }
            catch (Exception e)
            {
                Logger.Error($"Error when loading the package icon for the package {package.Id} on manager " + Name);
                Logger.Error(e);
                return null;
            }
        }

        public async Task<Uri[]> GetPackageScreenshotsUrl(IPackage package)
        {
            try
            {
                AssertPackageDetailsCompatibility("GetPackageScreenshots");
                return await PackageDetailsProvider.GetPackageScreenshotsUrl(package);
            }
            catch (Exception e)
            {
                Logger.Error($"Error when loading the package icon for the package {package.Id} on manager " + Name);
                Logger.Error(e);
                return [];
            }
        }
#pragma warning restore CS8602
        // END PACKAGEDETAILS-RELATED METHODS



        public void LogOperation(Process process, string output)
        {
            output = Regex.Replace(output, "\n.{0,6}\n", "\n");
            CoreData.ManagerLogs += $"\n▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄";
            CoreData.ManagerLogs += $"\n█▀▀▀▀▀▀▀▀▀ [{DateTime.Now}] {this.Name} ▀▀▀▀▀▀▀▀▀▀▀";
            CoreData.ManagerLogs += $"\n█  Executable: {process.StartInfo.FileName}";
            CoreData.ManagerLogs += $"\n█  Arguments: {process.StartInfo.Arguments}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += output;
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += $"[{DateTime.Now}] Exit Code: {process.ExitCode}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += "\n";
        }

    }

    internal class NullSourceProvider : BaseSourceProvider<PackageManager>
    {
        public NullSourceProvider(PackageManager manager) : base(manager)
        {
        }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            throw new Exception("Package manager does not support adding sources");
        }
        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            throw new Exception("Package manager does not support removing sources");
        }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return OperationVeredict.Failed;
        }

        protected override async Task<IManagerSource[]> GetSources_UnSafe()
        {
            return await Task.Run(() => new IManagerSource[] { Manager.DefaultSource });
        }
    }

    internal class NullPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
#pragma warning disable CS1998
        public NullPackageDetailsProvider(PackageManager manager) : base(manager)
        {
        }

        protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
        {
            return;
        }

        protected override async Task<CacheableIcon?> GetPackageIcon_Unsafe(IPackage package)
        {
            return null;
        }

        protected override async Task<Uri[]> GetPackageScreenshots_Unsafe(IPackage package)
        {
            return [];
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
        {
            return [];
        }
#pragma warning restore CS1998
    }
}
