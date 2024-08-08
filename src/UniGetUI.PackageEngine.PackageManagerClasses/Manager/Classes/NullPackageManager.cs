using System.Diagnostics;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class NullPackageManager : IPackageManager
    {
        public static NullPackageManager Instance = new();
        public ManagerProperties Properties { get; set; }
        public ManagerCapabilities Capabilities { get; set; }
        public ManagerStatus Status { get; set; }
        public string Name { get => Properties.Name; set { } }
        public string DisplayName { get => Properties.DisplayName ?? Properties.Name; set { } }
        public IManagerSource DefaultSource { get => Properties.DefaultSource; set { } }
        public bool ManagerReady { get => true; set { } }

        public IManagerLogger TaskLogger { get; set; }

        public ISourceProvider? SourceProvider { get; set; }

        public IPackageDetailsProvider? PackageDetailsProvider { get; set; }

        public ISourceFactory SourceFactory { get; set; }

        public IOperationProvider? OperationProvider { get; set; }

        public NullPackageManager()
        {
            TaskLogger = new ManagerLogger(this);
            var nullsource = NullSource.Instance;
            SourceProvider = new NullSourceProvider(this);
            PackageDetailsProvider = new NullPackageDetailsProvider(this);
            OperationProvider = new NullOperationProvider(this);
            SourceFactory = new SourceFactory(this);
            Properties = new ManagerProperties
            {
                IsDummy = true,
                Name = CoreTools.Translate("Unknown"),
                Description = "Unset",
                IconId = IconType.Help,
                ColorIconId = "Unset",
                ExecutableCallArgs = "Unset",
                ExecutableFriendlyName = "Unset",
                InstallVerb = "Unset",
                UpdateVerb = "Unset",
                UninstallVerb = "Unset",
                KnownSources = [nullsource],
                DefaultSource = nullsource,
            };

            Capabilities = new ManagerCapabilities();

            Status = new ManagerStatus
            {
                ExecutablePath = "C:/file.exe",
                Found = false,
                Version = "0"
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

        public IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetOperationResult(IPackage package, OperationType operation, IEnumerable<string> processOutput, int returnCode)
        {
            throw new NotImplementedException();
        }
    }
}
