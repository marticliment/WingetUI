using UniGetUI.ExternalLibraries.Pickers.Classes;
using UniGetUI.ExternalLibraries.Pickers.Enums;
using System;
using System.Collections.Generic;

namespace UniGetUI.ExternalLibraries.Pickers;

/// <summary>
/// Class responsible for file pick dialog.
/// </summary>
public class FileOpenPicker
{
    /// <summary>
    /// Window handle where dialog should appear.
    /// </summary>
    private readonly IntPtr _windowHandle;

    /// <summary>
    /// File pick dialog.
    /// </summary>
    /// <param name="windowHandle">Window handle where dialog should appear.</param>
    public FileOpenPicker(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <summary>
    /// Shows file pick dialog.
    /// </summary>
    /// <param name="typeFilters">List of extensions applied on dialog.</param>
    /// <returns>Path to selected file or empty string.</returns>
    public string Show(List<string>? typeFilters = null)
    {
        return Helper.ShowOpen(_windowHandle, FOS.FOS_FORCEFILESYSTEM, typeFilters);
    }
}
