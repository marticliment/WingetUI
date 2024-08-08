using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.Interfaces.ManagerProviders;
public interface IOperationProvider
{
    /// <summary>
    /// Returns the list of arguments that need to be passed to the Package Manager executable so
    /// that the requested operation is performed over the given package, with its corresponding
    /// installation options.
    /// </summary>
    public abstract IEnumerable<string> GetOperationParameters(
        IPackage package,
        IInstallationOptions options,
        OperationType operation
    );

    /// <summary>
    /// Returns the veredict of the given package operation, given the package, the operation type,
    /// the corresponding output and the return code.
    /// </summary>
    public abstract OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode
    );
}
