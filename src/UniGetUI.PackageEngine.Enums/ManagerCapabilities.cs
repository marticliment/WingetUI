using System.Runtime.InteropServices;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;

namespace UniGetUI.PackageEngine.ManagerClasses.Manager
{
    public struct SourceCapabilities
    {
        public bool KnowsUpdateDate { get; set; } = false;
        public bool KnowsPackageCount { get; set; } = false;
        public bool MustBeInstalledAsAdmin { get; set; } = false;
        public SourceCapabilities()
        { }
    }

    public struct ManagerCapabilities
    {
        public bool IsDummy = false;
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
        public bool SupportsCustomPackageIcons = false;
        public bool SupportsCustomPackageScreenshots = false;
        public SourceCapabilities Sources { get; set; }
        public ManagerCapabilities()
        {
            Sources = new SourceCapabilities();
        }

        public ManagerCapabilities(bool IsDummy)
        {
            Sources = new SourceCapabilities();
            this.IsDummy = IsDummy;
        }
    }
}
