// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsPackageManager.Interop;

public class WinGetConfigurationException : Exception
{
    // WinGet Configuration error codes:
    // https://github.com/microsoft/winget-cli/blob/master/src/PowerShell/Microsoft.WinGet.Configuration.Engine/Exceptions/ErrorCodes.cs
    public const int WingetConfigErrorInvalidConfigurationFile = unchecked((int)0x8A15C001);
    public const int WingetConfigErrorInvalidYaml = unchecked((int)0x8A15C002);
    public const int WingetConfigErrorInvalidFieldType = unchecked((int)0x8A15C003);
    public const int WingetConfigErrorUnknownConfigurationFileVersion = unchecked((int)0x8A15C004);
    public const int WingetConfigErrorSetApplyFailed = unchecked((int)0x8A15C005);
    public const int WingetConfigErrorDuplicateIdentifier = unchecked((int)0x8A15C006);
    public const int WingetConfigErrorMissingDependency = unchecked((int)0x8A15C007);
    public const int WingetConfigErrorDependencyUnsatisfied = unchecked((int)0x8A15C008);
    public const int WingetConfigErrorAssertionFailed = unchecked((int)0x8A15C009);
    public const int WingetConfigErrorManuallySkipped = unchecked((int)0x8A15C00A);
    public const int WingetConfigErrorWarningNotAccepted = unchecked((int)0x8A15C00B);
    public const int WingetConfigErrorSetDependencyCycle = unchecked((int)0x8A15C00C);
    public const int WingetConfigErrorInvalidFieldValue = unchecked((int)0x8A15C00D);
    public const int WingetConfigErrorMissingField = unchecked((int)0x8A15C00E);

    // WinGet Configuration unit error codes:
    public const int WinGetConfigUnitNotFound = unchecked((int)0x8A15C101);
    public const int WinGetConfigUnitNotFoundRepository = unchecked((int)0x8A15C102);
    public const int WinGetConfigUnitMultipleMatches = unchecked((int)0x8A15C103);
    public const int WinGetConfigUnitInvokeGet = unchecked((int)0x8A15C104);
    public const int WinGetConfigUnitInvokeTest = unchecked((int)0x8A15C105);
    public const int WinGetConfigUnitInvokeSet = unchecked((int)0x8A15C106);
    public const int WinGetConfigUnitModuleConflict = unchecked((int)0x8A15C107);
    public const int WinGetConfigUnitImportModule = unchecked((int)0x8A15C108);
    public const int WinGetConfigUnitInvokeInvalidResult = unchecked((int)0x8A15C109);
    public const int WinGetConfigUnitSettingConfigRoot = unchecked((int)0x8A15C110);
    public const int WinGetConfigUnitImportModuleAdmin = unchecked((int)0x8A15C111);
}
