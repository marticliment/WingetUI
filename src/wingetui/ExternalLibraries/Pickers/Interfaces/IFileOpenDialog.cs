using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UniGetUI.ExternalLibraries.Pickers.Guids;
using UniGetUI.ExternalLibraries.Pickers.Structures;

namespace UniGetUI.ExternalLibraries.Pickers.Interfaces;

[ComImport(),
Guid(IIDGuid.IFileOpenDialog),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog : IFileDialog
{
    // Defined on IFileDialog - repeated here due to requirements of COM interop layer
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFileTypes([In] uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);

    // Defined by IFileOpenDialog
    // ---------------------------------------------------------------------------------
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
}

internal interface IFileSaveDialog : IFileDialog
{
    // Defined on IFileDialog - repeated here due to requirements of COM interop layer
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFileTypes([In] uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);

    // Defined by IFileOpenDialog
    // ---------------------------------------------------------------------------------
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
}
