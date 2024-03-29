using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct PEHeader
{
    /// <remarks>
    /// Aka IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR.
    /// </remarks>
    public readonly DirectoryEntry CorHeaderTableDirectory;

    private const uint OffsetOfChecksum =
        sizeof(short) +                              // Magic
        sizeof(byte) +                               // MajorLinkerVersion
        sizeof(byte) +                               // MinorLinkerVersion
        sizeof(int) +                                // SizeOfCode
        sizeof(int) +                                // SizeOfInitializedData
        sizeof(int) +                                // SizeOfUninitializedData
        sizeof(int) +                                // AddressOfEntryPoint
        sizeof(int) +                                // BaseOfCode
#pragma warning disable IDE0055
        sizeof(long) +                               // PE32:  BaseOfData (int), ImageBase (int)
                                                     // PE32+: ImageBase (long)
#pragma warning restore IDE0055
        sizeof(int) +                                // SectionAlignment
        sizeof(int) +                                // FileAlignment
        sizeof(short) +                              // MajorOperatingSystemVersion
        sizeof(short) +                              // MinorOperatingSystemVersion
        sizeof(short) +                              // MajorImageVersion
        sizeof(short) +                              // MinorImageVersion
        sizeof(short) +                              // MajorSubsystemVersion
        sizeof(short) +                              // MinorSubsystemVersion
        sizeof(int) +                                // Win32VersionValue
        sizeof(int) +                                // SizeOfImage
        sizeof(int);                                 // SizeOfHeaders

    internal static uint Size(bool is32Bit) =>
        (uint)(OffsetOfChecksum +
        sizeof(int) +                                // Checksum
        sizeof(short) +                              // Subsystem
        sizeof(short) +                              // DllCharacteristics
        (4 * (is32Bit ? sizeof(int) : sizeof(long))) + // SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit
        sizeof(int) +                                // LoaderFlags
        sizeof(int) +                                // NumberOfRvaAndSizes
        (16 * sizeof(long)));                           // directory entries

    [SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
    internal PEHeader(ref PEBinaryReader reader)
    {
        var magic = (PEMagic)reader.ReadUInt16();
        if (magic is not PEMagic.PE32 and not PEMagic.PE32Plus)
        {
            throw new BadImageFormatException("Unknown PE Magic value.");
        }

        var size = Size(magic == PEMagic.PE32);
        reader.CheckBounds(size - sizeof(short));
        var skipCount = size - sizeof(short) - (2 * sizeof(long));
        reader.SkipNoCheck(skipCount);
        CorHeaderTableDirectory = new DirectoryEntry(ref reader);

        // ReservedDirectory (should be 0, 0)
        reader.SkipNoCheck<long>();
    }
}
