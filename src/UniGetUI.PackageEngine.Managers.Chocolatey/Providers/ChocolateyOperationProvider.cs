using System.Runtime.InteropServices;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.ChocolateyManager;
internal sealed class ChocolateyOperationProvider : BaseOperationProvider<Chocolatey>
{
    public ChocolateyOperationProvider(Chocolatey manager) : base(manager) { }

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
        parameters.AddRange([package.Id, "-y"]);

        if (options.CustomParameters is not null)
            parameters.AddRange(options.CustomParameters);

        if (options.InteractiveInstallation)
            parameters.Add("--notsilent");

        if(operation is OperationType.Install or OperationType.Update)
        {
            parameters.Add("--no-progress");

            if (options.Architecture == Architecture.X86)
                parameters.Add("--forcex86");

            if (options.PreRelease)
                parameters.Add("--prerelease");

            if (options.SkipHashCheck)
                parameters.AddRange(["--ignore-checksums", "--force"]);

            if (options.Version != "")
                parameters.AddRange([$"--version={options.Version}", "--allow-downgrade"]);
        }

        return parameters;
    }

    public override OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        if(returnCode is 3010)
        {
            return OperationVeredict.RestartRequired;
        }

        if (returnCode is 1641 or 1614 or 1605 or 0)
        {
            return OperationVeredict.Succeeded;
        }


        string output_string = string.Join("\n", processOutput);
        if (!package.OverridenOptions.RunAsAdministrator != true &&
            (output_string.Contains("Run as administrator")
            || output_string.Contains("The requested operation requires elevation")
            || output_string.Contains("ERROR: Exception calling \"CreateDirectory\" with \"1\" argument(s): \"Access to the path")) )
        {
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        return OperationVeredict.Failed;
    }
}
