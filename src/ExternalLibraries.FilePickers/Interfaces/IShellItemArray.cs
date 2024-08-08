using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExternalLibraries.Pickers.Enums;
using ExternalLibraries.Pickers.Guids;
using ExternalLibraries.Pickers.Structures;

namespace ExternalLibraries.Pickers.Interfaces;

[ComImport,
Guid(IIDGuid.IShellItemArray),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemArray
{
    // Not supported: IBindCtx
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, out IntPtr ppvOut);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetPropertyStore([In] int Flags, [In] ref Guid riid, out IntPtr ppv);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetPropertyDescriptionList([In] ref PROPERTYKEY keyType, [In] ref Guid riid, out IntPtr ppv);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetAttributes([In] SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetCount(out uint pdwNumItems);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetItemAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    // Not supported: IEnumShellItems (will use GetCount and GetItemAt instead)
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
}
