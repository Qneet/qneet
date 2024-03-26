using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct SectionHeader
{
    /// <summary>
    /// The name of the section.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The total size of the section when loaded into memory.
    /// If this value is greater than <see cref="SizeOfRawData"/>, the section is zero-padded.
    /// This field is valid only for PE images and should be set to zero for object files.
    /// </summary>
    public int VirtualSize { get; }

    /// <summary>
    /// For PE images, the address of the first byte of the section relative to the image base when the
    /// section is loaded into memory. For object files, this field is the address of the first byte before
    /// relocation is applied; for simplicity, compilers should set this to zero. Otherwise,
    /// it is an arbitrary value that is subtracted from offsets during relocation.
    /// </summary>
    public int VirtualAddress { get; }

    /// <summary>
    /// The size of the section (for object files) or the size of the initialized data on disk (for image files).
    /// For PE images, this must be a multiple of FileAlignment />.
    /// If this is less than <see cref="VirtualSize"/>, the remainder of the section is zero-filled.
    /// Because the <see cref="SizeOfRawData"/> field is rounded but the <see cref="VirtualSize"/> field is not,
    /// it is possible for <see cref="SizeOfRawData"/> to be greater than <see cref="VirtualSize"/> as well.
    ///  When a section contains only uninitialized data, this field should be zero.
    /// </summary>
    public int SizeOfRawData { get; }

    /// <summary>
    /// The file pointer to the first page of the section within the COFF file.
    /// For PE images, this must be a multiple of FileAlignment />.
    /// For object files, the value should be aligned on a 4 byte boundary for best performance.
    /// When a section contains only uninitialized data, this field should be zero.
    /// </summary>
    public int PointerToRawData { get; }


    internal const int NameSize = 8;

    internal const int Size =
        NameSize +
        sizeof(int) +   // VirtualSize
        sizeof(int) +   // VirtualAddress
        sizeof(int) +   // SizeOfRawData
        sizeof(int) +   // PointerToRawData
        sizeof(int) +   // PointerToRelocations
        sizeof(int) +   // PointerToLineNumbers
        sizeof(short) + // NumberOfRelocations
        sizeof(short) + // NumberOfLineNumbers
        sizeof(int);    // SectionCharacteristics

    internal SectionHeader(ref PEBinaryReader reader)
    {
        Name = reader.ReadNullPaddedUTF8(NameSize);
        VirtualSize = reader.ReadInt32();
        VirtualAddress = reader.ReadInt32();
        SizeOfRawData = reader.ReadInt32();
        PointerToRawData = reader.ReadInt32();
        reader.Skip(4 + 4 + 2 + 2 + 4);
    }
}
