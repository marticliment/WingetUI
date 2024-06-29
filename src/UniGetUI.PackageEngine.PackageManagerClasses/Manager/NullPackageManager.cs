using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Classes;
using UniGetUI.Core.IconEngine;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.Devices.Bluetooth.Advertisement;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class NullPackageManager : SingletonBase<NullPackageManager>, IPackageManager
    {
        public ManagerProperties Properties { get; set; }
        public ManagerCapabilities Capabilities { get; set; }
        public ManagerStatus Status { get; set; }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IManagerSource DefaultSource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ManagerReady { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ManagerLogger TaskLogger => throw new NotImplementedException();

        public ISourceProvider? SourceProvider => throw new NotImplementedException();

        public IPackageDetailsProvider? PackageDetailsProvider => throw new NotImplementedException();

        public ISourceFactory SourceFactory => throw new NotImplementedException();

        private NullPackageManager()
        {
            Properties = new ManagerProperties()
            {

            };

            Capabilities = new ManagerCapabilities()
            {

            };

            Status = new ManagerStatus()
            {
            };
        }

        public Task<IPackage[]> FindPackages(string query)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetAddSourceParameters(IManagerSource source)
        {
            throw new NotImplementedException();
        }

        public Task<IPackage[]> GetAvailableUpdates()
        {
            throw new NotImplementedException();
        }

        public Task<IPackage[]> GetInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetInstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetInstallParameters(IPackage package, IInstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public Task GetPackageDetails(IPackageDetails details)
        {
            throw new NotImplementedException();
        }

        public Task<CacheableIcon?> GetPackageIconUrl(IPackage package)
        {
            throw new NotImplementedException();
        }

        public Task<Uri[]> GetPackageScreenshotsUrl(IPackage package)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetPackageVersions(IPackage package)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetRemoveSourceParameters(IManagerSource source)
        {
            throw new NotImplementedException();
        }

        public IManagerSource? GetSourceIfExists(string SourceName)
        {
            throw new NotImplementedException();
        }

        public IManagerSource GetSourceOrDefault(string SourceName)
        {
            throw new NotImplementedException();
        }

        public Task<IManagerSource[]> GetSources()
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetUninstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetUninstallParameters(IPackage package, IInstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetUpdateOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetUpdateParameters(IPackage package, IInstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public Task InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled()
        {
            throw new NotImplementedException();
        }

        public bool IsReady()
        {
            throw new NotImplementedException();
        }

        public void LogOperation(Process process, string output)
        {
            throw new NotImplementedException();
        }

        public Task RefreshPackageIndexes()
        {
            throw new NotImplementedException();
        }
    }
}
