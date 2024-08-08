using UniGetUI.Core.Classes;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.ManagerClasses.Manager
{
    public abstract class PackageManager : SingletonBase<PackageManager>, ISourceProvider, IPackageDetailsProvider, IPackageManager
    {
        public ManagerProperties Properties { get; set; } = new(IsDummy: true);
        public ManagerCapabilities Capabilities { get; set; } = new(IsDummy: true);
        public ManagerStatus Status { get; set; } = new() { Found = false };
        public string Name { get => Properties.Name ?? "Unset"; }
        public string DisplayName { get => Properties.DisplayName ?? Name; }
        public IManagerSource DefaultSource { get => Properties.DefaultSource; }

        public static string[] FALSE_PACKAGE_NAMES = [""];
        public static string[] FALSE_PACKAGE_IDS = [""];
        public static string[] FALSE_PACKAGE_VERSIONS = [""];
        public bool ManagerReady { get; set; }
        public IManagerLogger TaskLogger { get; }

        public ISourceProvider SourceProvider { get; set; }
        public ISourceFactory SourceFactory { get => SourceProvider.SourceFactory; }
        public IEnumerable<ManagerDependency> Dependencies { get; protected set; } = [];

        public IPackageDetailsProvider? PackageDetailsProvider { get; set; }
        public IOperationProvider OperationProvider { get; set; }

        private readonly bool __base_constructor_called;

        public PackageManager()
        {
            __base_constructor_called = true;
            TaskLogger = new ManagerLogger(this);
            SourceProvider = new NullSourceProvider(this);
            PackageDetailsProvider = new NullPackageDetailsProvider(this);
            OperationProvider = new NullOperationProvider(this);
        }

        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            // BEGIN integrity check
            if (!__base_constructor_called)
            {
                throw new InvalidOperationException($"The Manager {Properties.Name} has not called the base constructor.");
            }

            if (Capabilities.IsDummy)
            {
                throw new InvalidOperationException($"The current instance of PackageManager with name ${Properties.Name} does not have a valid Capabilities object");
            }

            if (Properties.IsDummy)
            {
                throw new InvalidOperationException($"The current instance of PackageManager with name ${Properties.Name} does not have a valid Properties object");
            }

            if (Capabilities.SupportsCustomSources && SourceProvider is NullSourceProvider)
            {
                throw new InvalidOperationException($"Manager {Name} has been declared as SupportsCustomSources but has no helper associated with it");
            }

            if (OperationProvider is NullOperationProvider)
            {
                throw new InvalidOperationException($"Manager {Name} does not have an OperationProvider");
            }
            // END integrity check

            Properties.DefaultSource.RefreshSourceNames();
            foreach(var source in Properties.KnownSources) source.RefreshSourceNames();

            try
            {
                Status = await LoadManager();

                if (IsReady() && Capabilities.SupportsCustomSources)
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
                               "\n█ Fancy exe name: " + Properties.ExecutableFriendlyName +
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
        protected abstract Task<ManagerStatus> LoadManager();

        /// <summary>
        /// Returns true if the manager is enabled, false otherwise
        /// </summary>
        public bool IsEnabled()
        {
            return !Settings.Get("Disable" + Name);
        }

        /// <summary>
        /// Returns true if the manager is enabled and available (the required executable files were found). Returns false otherwise
        /// </summary>
        public bool IsReady()
        {
            return IsEnabled() && Status.Found;
        }

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public async Task<IPackage[]> FindPackages(string query)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet FindPackages was called"); return []; }
            try
            {
                Package[] packages = await FindPackages_UnSafe(query).WaitAsync(TimeSpan.FromSeconds(60));
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
        public async Task<IPackage[]> GetAvailableUpdates()
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetAvailableUpdates was called"); return []; }
            try
            {
                await RefreshPackageIndexes().WaitAsync(TimeSpan.FromSeconds(60));
                Package[] packages = await GetAvailableUpdates_UnSafe().WaitAsync(TimeSpan.FromSeconds(60));
                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageCacher.GetUpgradablePackage(packages[i]);
                }

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
        public async Task<IPackage[]> GetInstalledPackages()
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetInstalledPackages was called"); return []; }
            try
            {
                Package[] packages = await GetInstalledPackages_UnSafe().WaitAsync(TimeSpan.FromSeconds(60));
                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageCacher.GetInstalledPackage(packages[i]);
                }

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
        protected abstract Task<Package[]> FindPackages_UnSafe(string query);

        /// <summary>
        /// Returns the available updates reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of UpgradablePackage objects</returns>
        protected abstract Task<Package[]> GetAvailableUpdates_UnSafe();

        /// <summary>
        /// Returns an array of Package objects containing the installed packages reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<Package[]> GetInstalledPackages_UnSafe();

        /// <summary>
        /// Refreshes the Package Manager sources/indexes
        /// Each manager MUST implement this method.
        /// </summary>
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
        /// <exception cref="InvalidOperationException"></exception>
        private void AssertSourceCompatibility(string MethodName)
        {
            if (!Capabilities.SupportsCustomSources)
            {
                throw new InvalidOperationException($"Manager {Name} does not support custom sources but yet {MethodName} method was called.\n {Environment.StackTrace}");
            }

            if (SourceProvider == null)
            {
                throw new InvalidOperationException($"Manager {Name} does support custom sources but yet the source helper is null");
            }
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
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetSources was called"); return []; }
            try
            {
                AssertSourceCompatibility("GetSources");
                IManagerSource[] result = await SourceProvider.GetSources().WaitAsync(TimeSpan.FromSeconds(60));
                Logger.Debug($"Loaded {result.Length} sources for manager {Name}");
                return result;
            }
            catch (Exception e)
            {
                Logger.Error("Error finding sources for manager " + Name);
                Logger.Error(e);
                return [];
            }
        }
#pragma warning restore CS8602
        // END SOURCE-RELATED METHODS

        // BEGIN PACKAGEDEAILS-RELATED METHODS
        private void AssertPackageDetailsCompatibility(string methodName)
        {
            if (PackageDetailsProvider == null)
            {
                throw new InvalidOperationException($"Manager {Name} does not have a valid PackageDetailsProvider helper, when attemtping to call {methodName}");
            }
        }
#pragma warning disable CS8602

        public async Task GetPackageDetails(IPackageDetails details)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetPackageDetails was called"); return; }
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
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetPackageVersions was called"); return []; }
            try
            {
                AssertPackageDetailsCompatibility("GetPackageVersions");
                if (package.Manager.Capabilities.SupportsCustomVersions)
                {
                    return await PackageDetailsProvider.GetPackageVersions(package);
                }

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
        // END PACKAGEDETAILS-RELATED METHODS


        // BEGIN OPERATION-RELATED METHODS
        public IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
        {
            try
            {
                var parameters = OperationProvider.GetOperationParameters(package, options, operation);
                Logger.Info($"Loaded operation parameters for package id={package.Id} on manager {Name} and operation {operation}: " + string.Join(' ', parameters));
                return parameters;
            }
            catch (Exception ex)
            {
                Logger.Error($"A fatal error ocurred while loading operation parameters for package id={package.Id} on manager {Name} and operation {operation}");
                Logger.Error(ex);
                return [];
            }
        }

        public OperationVeredict GetOperationResult(IPackage package, OperationType operation, IEnumerable<string> processOutput, int returnCode)
        {
            try
            {
                return OperationProvider.GetOperationResult(package, operation, processOutput, returnCode);
            }
            catch (Exception ex)
            {
                Logger.Error($"A fatal error ocurred while loading operation parameters for package id={package.Id} on manager {Name} and operation {operation}");
                Logger.Error(ex);
                return OperationVeredict.Failed;
            }
        }
        // END OPERATION-RELATED METHODS
#pragma warning restore CS8602
    }

    internal sealed class NullSourceProvider : BaseSourceProvider<IPackageManager>
    {
        public NullSourceProvider(IPackageManager manager) : base(manager)
        {
        }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            throw new InvalidOperationException("Package manager does not support adding sources");
        }
        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            throw new InvalidOperationException("Package manager does not support removing sources");
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

    internal sealed class NullPackageDetailsProvider : BasePackageDetailsProvider<IPackageManager>
    {
#pragma warning disable CS1998
        public NullPackageDetailsProvider(IPackageManager manager) : base(manager)
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
    }

    internal sealed class NullOperationProvider : BaseOperationProvider<IPackageManager>
    {
        public NullOperationProvider(IPackageManager manager) : base(manager)
        {
        }

        public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
        {
            return Array.Empty<string>();
        }

        public override OperationVeredict GetOperationResult(IPackage package, OperationType operation, IEnumerable<string> processOutput, int returnCode)
        {
            return OperationVeredict.Failed;
        }
    }
#pragma warning restore CS1998
}
