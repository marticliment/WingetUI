using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.PipManager;
internal sealed class PipOperationProvider : BaseOperationProvider<Pip>
{
    public PipOperationProvider(Pip manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(
        IPackage package,
        IInstallationOptions options,
        OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.AddRange([
            options.Version != string.Empty? $"{package.Id}=={options.Version}" : package.Id,
            "--no-input",
            "--no-color",
            "--no-python-version-warning",
            "--no-cache"
        ]);

        if (options.CustomParameters != null)
            parameters.AddRange(options.CustomParameters);

        if (operation is OperationType.Uninstall)
        {
            parameters.Add("--yes");
        }
        else
        {
            if (options.PreRelease)
                parameters.Add("--pre");

            if (package.OverridenOptions.Scope == PackageScope.User || (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.User))
                parameters.Add("--user");
        }

        return parameters;
    }

    public override OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        if (returnCode == 0)
        {
            return OperationVeredict.Succeeded;
        }

        string output_string = string.Join("\n", processOutput);

        if (output_string.Contains("--user") && package.OverridenOptions.Scope != PackageScope.User)
        {
            package.OverridenOptions.Scope = PackageScope.User;
            return OperationVeredict.AutoRetry;
        }
        return OperationVeredict.Failed;

    }
}
