using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Interfaces
{
    public interface IPackageManager : ISourceProvider, IPackageDetailsProvider, IOperationProvider
    {
        public ManagerProperties Properties { get; }
        public ManagerCapabilities Capabilities { get; }
        public ManagerStatus Status { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public IManagerSource DefaultSource { get; }
        public bool ManagerReady { get; }
        public IManagerLogger TaskLogger { get; }

        public ISourceProvider? SourceProvider { get; }
        public IPackageDetailsProvider? PackageDetailsProvider { get; }
        public IOperationProvider? OperationProvider { get; }

        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        public Task InitializeAsync();

        /// <summary>
        /// Returns true if the manager is enabled, false otherwise
        /// </summary>
        public bool IsEnabled();

        /// <summary>
        /// Returns true if the manager is enabled and available (the required executable files were found). Returns false otherwise
        /// </summary>
        public bool IsReady();

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public Task<IPackage[]> FindPackages(string query);

        /// <summary>
        /// Returns an array of UpgradablePackage objects that represent the available updates reported by the manager.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public Task<IPackage[]> GetAvailableUpdates();

        /// <summary>
        /// Returns an array of Package objects that represent the installed reported by the manager.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public Task<IPackage[]> GetInstalledPackages();

        /// <summary>
        /// Refreshes the Package Manager sources/indexes
        /// Each manager MUST implement this method.
        /// </summary>
        public Task RefreshPackageIndexes();

        public IManagerSource GetSourceOrDefault(string SourceName);
        public IManagerSource? GetSourceIfExists(string SourceName);
    }
}
