using System.Runtime.InteropServices;
using UniGetUI.ExternalLibraries.Pickers.Guids;

namespace UniGetUI.ExternalLibraries.Pickers.Classes;

// ---------------------------------------------------
// .NET classes representing runtime callable wrappers
[ComImport,
ClassInterface(ClassInterfaceType.None),
TypeLibType(TypeLibTypeFlags.FCanCreate),
Guid(CLSIDGuid.FileOpenDialog)]
internal class FileOpenDialogRCW
{
}