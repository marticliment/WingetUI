using System.Runtime.InteropServices;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.ScoopManager;
internal sealed class ScoopOperationProvider : BaseOperationProvider<Scoop>
{
    public ScoopOperationProvider(Scoop manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.Add($"{package.Source.Name}/{package.Id}");

        if (package.OverridenOptions.Scope == PackageScope.Global || (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.Global))
        {
            // Scoop requires admin rights to install global packages
            package.OverridenOptions.RunAsAdministrator = true;
            parameters.Add("--global");
        }

        if(options.CustomParameters is not null)
            parameters.AddRange(options.CustomParameters);

        if (operation is OperationType.Uninstall)
        {
            if (options.RemoveDataOnUninstall)
                parameters.Add("--purge");
        }
        else
        {
            if (options.SkipHashCheck)
                parameters.Add("--skip");

            parameters.AddRange(options.Architecture switch
            {
                Architecture.X64 => ["--arch", "64bit"],
                Architecture.X86 => ["--arch", "32bit"],
                Architecture.Arm64 => ["--arch", "arm64"],
                _ => []
            });
        }

        return parameters;
    }

    public override OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        string output_string = string.Join("\n", processOutput);
        if (package.OverridenOptions.Scope != PackageScope.Global && output_string.Contains("Try again with the --global (or -g) flag instead"))
        {
            package.OverridenOptions.Scope = PackageScope.Global;
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        if (package.OverridenOptions.RunAsAdministrator != true
            && (output_string.Contains("requires admin rights")
                || output_string.Contains("requires administrator rights")
                || output_string.Contains("you need admin rights to install global apps")))
        {
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        if (operation is OperationType.Uninstall)
        {
            if (output_string.Contains("was uninstalled"))
                return OperationVeredict.Succeeded;

            return OperationVeredict.Failed;
        }
        else
        {
            if (output_string.Contains("ERROR"))
                return OperationVeredict.Failed;

            return OperationVeredict.Succeeded;
        }
    }
}
