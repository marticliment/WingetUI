using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.NpmManager;
internal sealed class NpmOperationProvider : BaseOperationProvider<Npm>
{
    public NpmOperationProvider(Npm manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = operation switch {
            OperationType.Install => [Manager.Properties.InstallVerb, $"{package.Id}@{(options.Version == string.Empty? package.Version: options.Version)}"],
            OperationType.Update => [Manager.Properties.UpdateVerb, $"{package.Id}@{package.NewVersion}"],
            OperationType.Uninstall => [Manager.Properties.UninstallVerb, package.Id],
            _ => throw new InvalidDataException("Invalid package operation")
        };
        parameters.Add(package.Id);

        if (options.CustomParameters != null)
            parameters.AddRange(options.CustomParameters);

        if (package.OverridenOptions.Scope == PackageScope.Global || (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.Global))
            parameters.Add("--global");

        if (options.PreRelease)
            parameters.AddRange(["--include", "dev"]);

        return parameters;
    }

    public override OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
