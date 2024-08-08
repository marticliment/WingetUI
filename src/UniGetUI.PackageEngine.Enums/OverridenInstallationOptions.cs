using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.Structs;
public struct OverridenInstallationOptions
{
    public PackageScope? Scope;
    public bool? RunAsAdministrator;

    public OverridenInstallationOptions(PackageScope? scope = null, bool? runAsAdministrator = null)
    {
        Scope = scope;
        RunAsAdministrator = runAsAdministrator;
    }
}
