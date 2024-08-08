using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExternalLibraries.Pickers.Enums;
using ExternalLibraries.Pickers.Guids;
using ExternalLibraries.Pickers.Structures;

namespace ExternalLibraries.Pickers.Interfaces;

[ComImport(),
Guid(IIDGuid.IFileDialog),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialog : IModalWindow
{
    // Defined on IModalWindow - repeated here due to requirements of COM interop layer
    // --------------------------------------------------------------------------------
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
    PreserveSig]
    new int Show([In] IntPtr parent);

    // IFileDialog-Specific interface members
    // --------------------------------------------------------------------------------
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFileTypeIndex([In] uint iFileType);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetFileTypeIndex(out uint piFileType);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Unadvise([In] uint dwCookie);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetOptions([In] FOS fos);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetOptions(out FOS pfos);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, FDAP fdap);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Close([MarshalAs(UnmanagedType.Error)] int hr);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetClientGuid([In] ref Guid guid);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ClearClientData();

    // Not supported:  IShellItemFilter is not defined, converting to IntPtr
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
}
