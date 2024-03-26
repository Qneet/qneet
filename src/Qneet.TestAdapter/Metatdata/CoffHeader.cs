using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CoffHeader
{
    /// <summary>
    /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
    /// </summary>
    public short NumberOfSections { get; }

    internal const int Size =
        sizeof(short) + // Machine
        sizeof(short) + // NumberOfSections
        sizeof(int) +   // TimeDateStamp:
        sizeof(int) +   // PointerToSymbolTable
        sizeof(int) +   // NumberOfSymbols
        sizeof(short) + // SizeOfOptionalHeader:
        sizeof(ushort); // Characteristics

    internal CoffHeader(ref PEBinaryReader reader)
    {
        reader.Skip<ushort>();
        NumberOfSections = reader.ReadInt16();
        //reader.Skip((3 * sizeof(int)) + (2 * sizeof(ushort)));
        reader.Skip(16);
    }
}
