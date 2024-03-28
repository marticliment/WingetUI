using UniGetUI.ExternalLibraries.Pickers.Classes;
using UniGetUI.ExternalLibraries.Pickers.Enums;
using System;

namespace UniGetUI.ExternalLibraries.Pickers;

/// <summary>
/// Class responsible for folder pick dialog.
/// </summary>
public class FolderPicker
{
    /// <summary>
    /// Window handle where dialog should appear.
    /// </summary>
    private readonly IntPtr _windowHandle;

    /// <summary>
    /// Folder pick dialog.
    /// </summary>
    /// <param name="windowHandle">Window handle where dialog should appear.</param>
    public FolderPicker(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <summary>
    /// Shows folder pick dialog.
    /// </summary>
    /// <returns>Path to selected folder or empty string.</returns>
    public string Show()
    {
        return Helper.ShowOpen(_windowHandle, FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
    }
}
