using UniGetUI.ExternalLibraries.Pickers.Enums;
using UniGetUI.ExternalLibraries.Pickers.Interfaces;
using UniGetUI.ExternalLibraries.Pickers.Structures;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace UniGetUI.ExternalLibraries.Pickers.Classes;

internal static class Helper
{
    /// <summary>
    /// Shows FileOpenDialog.
    /// </summary>
    /// <param name="windowHandle">Window handle where dialog should appear.</param>
    /// <param name="fos">File open dialog options.</param>
    /// <param name="typeFilters">List of extensions applied on dialog.</param>
    /// <returns>Path to selected file, folder or empty string.</returns>
    internal static string ShowOpen(nint windowHandle, FOS fos, List<string>? typeFilters = null)
    {
        var dialog = new FileOpenDialog();
        try
        {
            dialog.SetOptions(fos);

            if (typeFilters != null)
            {
                typeFilters.Insert(0, string.Join("; ", typeFilters));
                var filterSpecs = typeFilters.Select(f => new COMDLG_FILTERSPEC(f)).ToArray();

                dialog.SetFileTypes((uint)filterSpecs.Length, filterSpecs);
            }

            if (dialog.Show(windowHandle) != 0)
                return string.Empty;

            dialog.GetResult(out var item);
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
            return path;
        }
        finally
        {
#pragma warning disable CA1416 
            Marshal.ReleaseComObject(dialog);
#pragma warning restore CA1416
        }
    }

    internal static string ShowSave(nint windowHandle, FOS fos, List<string>? typeFilters = null, string name = "")
    {
        var dialog = new FileSaveDialog();
        try
        {
            dialog.SetOptions(fos);

            if (typeFilters != null)
            {
                var filterSpecs = typeFilters.Select(f => new COMDLG_FILTERSPEC(f)).ToArray();

                dialog.SetFileTypes((uint)filterSpecs.Length, filterSpecs);
            }

            if (!string.IsNullOrEmpty(name))
                dialog.SetFileName(name);

            if (dialog.Show(windowHandle) != 0)
                return string.Empty;

            dialog.GetResult(out var item);
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
            return path;
        }
        finally
        {
#pragma warning disable CA1416 
            Marshal.ReleaseComObject(dialog);
#pragma warning restore CA1416
        }
    }
}
