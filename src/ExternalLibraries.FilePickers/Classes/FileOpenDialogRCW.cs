using System.Runtime.InteropServices;
using ExternalLibraries.Pickers.Guids;

namespace ExternalLibraries.Pickers.Classes;

// ---------------------------------------------------
// .NET classes representing runtime callable wrappers
[ComImport,
ClassInterface(ClassInterfaceType.None),
TypeLibType(TypeLibTypeFlags.FCanCreate),
Guid(CLSIDGuid.FileOpenDialog)]
internal class FileOpenDialogRCW
{
}