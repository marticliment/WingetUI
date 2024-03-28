using UniGetUI.ExternalLibraries.Pickers.Classes;
using UniGetUI.ExternalLibraries.Pickers.Enums;
using System;
using System.Collections.Generic;

namespace UniGetUI.ExternalLibraries.Pickers;

/// <summary>
/// Class responsible for file pick dialog.
/// </summary>
public class FileSavePicker
{
    /// <summary>
    /// Window handle where dialog should appear.
    /// </summary>
    private readonly IntPtr _windowHandle;

    /// <summary>
    /// File pick dialog.
    /// </summary>
    /// <param name="windowHandle">Window handle where dialog should appear.</param>
    public FileSavePicker(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <summary>
    /// Shows file pick dialog.
    /// </summary>
    /// <param name="typeFilters">List of extensions applied on dialog.</param>
    /// <param name="defaultName">Default name for the file.</param>
    /// <returns>Path to selected file or empty string.</returns>
    public string Show(List<string>? typeFilters = null, string defaultName = "")
    {
        return Helper.ShowSave(_windowHandle, FOS.FOS_FORCEFILESYSTEM, typeFilters, defaultName);
    }
}
