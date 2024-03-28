// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT;

namespace WindowsPackageManager.Interop;

/// <summary>
/// Factory for creating winget COM objects using manual activation
/// to have them in an elevated context.
/// </summary>
/// <remarks>
/// This needs to be called from an elevated context, or the winget
/// server will reject the connection.
///
/// The WinGetServerManualActivation_CreateInstance function used here is defined in
/// https://github.com/microsoft/winget-cli/blob/master/src/WinGetServer/WinGetServerManualActivation_Client.cpp
///
/// This class is based on what the winget cmdlets do. See
/// https://github.com/microsoft/winget-cli/blob/master/src/PowerShell/Microsoft.WinGet.Client/Helpers/ComObjectFactory.cs
/// </remarks>
public class WindowsPackageManagerElevatedFactory : WindowsPackageManagerFactory
{
    // The only CLSID context supported by the DLL we call is Prod.
    // If we want to use Dev classes we have to use a Dev version of the DLL.
    public WindowsPackageManagerElevatedFactory()
        : base(ClsidContext.Prod)
    {
    }

    protected override unsafe T CreateInstance<T>(Guid clsid, Guid iid)
    {
        void* pUnknown = null;

        try
        {
            var hr = WinGetServerManualActivation_CreateInstance(in clsid, in iid, 0, out pUnknown);
            Marshal.ThrowExceptionForHR(hr);
            return MarshalInterface<T>.FromAbi((IntPtr)pUnknown);
        }
        finally
        {
            // CoCreateInstance and FromAbi both AddRef on the native object.
            // Release once to prevent memory leak.
            if (pUnknown is not null)
            {
                Marshal.Release((IntPtr)pUnknown);
            }
        }
    }

    [DllImport("winrtact.dll", ExactSpelling = true)]
    private static unsafe extern int WinGetServerManualActivation_CreateInstance(
        in Guid clsid,
        in Guid iid,
        uint flags,
        out void* instance);
}
