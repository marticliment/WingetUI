using System.Runtime.InteropServices;
using ExternalLibraries.Pickers.Guids;

namespace ExternalLibraries.Pickers.Classes;

// ---------------------------------------------------
// .NET classes representing runtime callable wrappers
[ComImport,
ClassInterface(ClassInterfaceType.None),
TypeLibType(TypeLibTypeFlags.FCanCreate),
Guid(CLSIDGuid.FileSaveDialog)]
internal class FileSaveDialogRCW
{
}
