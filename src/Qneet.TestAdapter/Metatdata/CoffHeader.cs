using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CoffHeader
{
    /// <summary>
    /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
    /// </summary>
    public readonly short NumberOfSections;

    internal const uint Size =
        sizeof(short) + // Machine
        sizeof(short) + // NumberOfSections
        sizeof(int) +   // TimeDateStamp:
        sizeof(int) +   // PointerToSymbolTable
        sizeof(int) +   // NumberOfSymbols
        sizeof(short) + // SizeOfOptionalHeader:
        sizeof(ushort); // Characteristics

    internal CoffHeader(ref PEBinaryReader reader)
    {
        reader.SkipNoCheck<ushort>();
        NumberOfSections = reader.ReadInt16NoCheck();
        reader.SkipNoCheck(16);
    }
}
