namespace UniGetUI.Interface.Enums
{
    /// <summary>
    /// Represents the visual status of a package on a list
    /// </summary>
    public enum PackageTag
    {
        Default,
        AlreadyInstalled,
        IsUpgradable,
        Pinned,
        OnQueue,
        BeingProcessed,
        Failed,
        Unavailable
    }
}
