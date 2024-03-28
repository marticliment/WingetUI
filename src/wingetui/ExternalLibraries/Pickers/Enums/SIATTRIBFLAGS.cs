namespace UniGetUI.ExternalLibraries.Pickers.Enums;

// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishellitemarray-getattributes
internal enum SIATTRIBFLAGS
{
    SIATTRIBFLAGS_AND = 0x00000001, // if multiple items and the attirbutes together.
    SIATTRIBFLAGS_OR = 0x00000002, // if multiple items or the attributes together.
    SIATTRIBFLAGS_APPCOMPAT = 0x00000003, // Call GetAttributes directly on the ShellFolder for multiple attributes
}
