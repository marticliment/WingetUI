using System.Runtime.InteropServices;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.WingetManager;
internal sealed class WinGetOperationProvider : BaseOperationProvider<WinGet>
{
    public WinGetOperationProvider(WinGet manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.AddRange(["--id", $"\"{package.Id}\"", "--exact"]);
        parameters.AddRange(["--source", package.Source.IsVirtualManager ? "winget" : package.Source.Name]);
        parameters.AddRange(["--accept-source-agreements", "--disable-interactivity"]);

        // package.Scope is meaningless in WinGet packages. Default is unspecified, hence the _ => [].
        parameters.AddRange(options.InstallationScope switch {
            PackageScope.User => ["--scope", "user"],
            PackageScope.Machine => ["--scope", "machine"],
            _ => []
        });

        if (operation is OperationType.Uninstall && package.Version != "Unknown")
        {
            parameters.AddRange(["--version", $"{package.Version}"]);
        }
        else if (operation is OperationType.Install && package.Version != "Unknown")
        {
            parameters.AddRange(["--version", options.Version != "" ? $"{options.Version}" : $"{package.Version}"]);
        }

        parameters.Add(options.InteractiveInstallation ? "--interactive" : "--silent");
        parameters.AddRange(options.CustomParameters);

        if(operation is OperationType.Update)
        {
            if (package.Name.Contains("64-bit") || package.Id.ToLower().Contains("x64"))
            {
                options.Architecture = Architecture.X64;
            }
            else if (package.Name.Contains("32-bit") || package.Id.ToLower().Contains("x86"))
            {
                options.Architecture = Architecture.X86;
            }
            parameters.Add("--include-unknown");
        }

        if(operation is not OperationType.Uninstall)
        {
            parameters.AddRange(["--accept-package-agreements", "--force"]);

            if (options.SkipHashCheck)
                parameters.Add("--ignore-security-hash");

            if (options.CustomInstallLocation != "")
                parameters.AddRange(["--location", $"\"{options.CustomInstallLocation}\""]);

            parameters.AddRange(options.Architecture switch
            {
                Architecture.X86 => ["--architecture", "x86"],
                Architecture.X64 => ["--architecture", "x64"],
                Architecture.Arm64 => ["--architecture", "arm64"],
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
        // See https://github.com/microsoft/winget-cli/blob/master/doc/windows/package-manager/winget/returnCodes.md for reference
        uint uintCode = (uint)returnCode;

        if (uintCode == 0x8A150109)
        {
            // If the user is required to restart the system to complete the installation
            return OperationVeredict.RestartRequired;
        }

        if (uintCode == 0x8A150011)
        {
            // TODO: Needs skip checksum
            return OperationVeredict.Failed;
        }

        if (uintCode == 0x8A15002B || uintCode == 0x8A15010D || uintCode == 0x8A15004F || uintCode == 0x8A15010E)
        {
            // Application is already installed
            return OperationVeredict.Succeeded;
        }

        if(uintCode == 0x8A150056 && package.OverridenOptions.RunAsAdministrator != false && !CoreTools.IsAdministrator())
        {
            // Installer can't run elevated
            package.OverridenOptions.RunAsAdministrator = false;
            return OperationVeredict.AutoRetry;
        }

        if (uintCode == 0x8A150019 && package.OverridenOptions.RunAsAdministrator != true)
        {
            // Installer needs to run elevated
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
