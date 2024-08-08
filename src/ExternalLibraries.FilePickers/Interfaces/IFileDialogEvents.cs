using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExternalLibraries.Pickers.Enums;
using ExternalLibraries.Pickers.Guids;

namespace ExternalLibraries.Pickers.Interfaces;

[ComImport,
Guid(IIDGuid.IFileDialogEvents),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialogEvents
{
    // NOTE: some of these callbacks are cancelable - returning S_FALSE means that 
    // the dialog should not proceed (e.g. with closing, changing folder); to 
    // support this, we need to use the PreserveSig attribute to enable us to return
    // the proper HRESULT
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
    PreserveSig]
    HRESULT OnFileOk([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
    PreserveSig]
    HRESULT OnFolderChanging([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psiFolder);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void OnFolderChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void OnSelectionChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void OnShareViolation([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void OnTypeChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void OnOverwrite([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out FDE_OVERWRITE_RESPONSE pResponse);
}
