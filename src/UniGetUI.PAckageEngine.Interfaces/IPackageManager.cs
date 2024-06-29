using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.IconEngine;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Interfaces
{
    public interface IPackageManager : ISourceProvider, IPackageDetailsProvider
    {
        public ManagerProperties Properties { get; set; }
        public ManagerCapabilities Capabilities { get; set; }
        public ManagerStatus Status { get; set; }
        public string Name { get; set; }
        public IManagerSource DefaultSource { get; set; }
        public bool ManagerReady { get; set; }
        public ManagerLogger TaskLogger { get; }

        public ISourceProvider? SourceProvider { get; }
        public IPackageDetailsProvider? PackageDetailsProvider { get; }


        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        /// <returns></returns>
        public Task InitializeAsync();

        /// <summary>
        /// Returns true if the manager is enabled, false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled();

        /// <summary>
        /// Returns true if the manager is enabled and available (the required executable files were found). Returns false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsReady();

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may 
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<IPackage[]> FindPackages(string query);

        /// <summary>
        /// Returns an array of UpgradablePackage objects that represent the available updates reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<IPackage[]> GetAvailableUpdates();

        /// <summary>
        /// Returns an array of Package objects that represent the installed reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <returns></returns>
        public Task<IPackage[]> GetInstalledPackages();

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
        public Task RefreshPackageIndexes();
        public IManagerSource GetSourceOrDefault(string SourceName);
        public IManagerSource? GetSourceIfExists(string SourceName);

        public void LogOperation(Process process, string output);
    }
}
