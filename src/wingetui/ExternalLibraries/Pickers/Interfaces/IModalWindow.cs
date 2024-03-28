using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UniGetUI.ExternalLibraries.Pickers.Guids;

namespace UniGetUI.ExternalLibraries.Pickers.Interfaces;

[ComImport(),
Guid(IIDGuid.IModalWindow),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IModalWindow
{

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
    PreserveSig]
    int Show([In] IntPtr parent);
}
