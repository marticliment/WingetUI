using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExternalLibraries.Pickers.Enums;
using ExternalLibraries.Pickers.Guids;

namespace ExternalLibraries.Pickers.Interfaces;

[ComImport,
Guid(IIDGuid.IShellItem),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    // Not supported: IBindCtx
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
}
