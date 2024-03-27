using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
internal static class MetadataReaderFactory
{
    private static ReadOnlySpan<byte> CormetaSection => ".cormeta"u8;

    internal const ushort DosSignature = 0x5A4D;     // 'M' 'Z'
    internal const int PESignatureOffsetLocation = 0x3C;
    internal const uint PESignature = 0x00004550;    // PE00
    internal const int PESignatureSize = sizeof(uint);

    internal const int SizeOfCorHeader = 72;

    public static unsafe bool TryGetMetadaReader(byte* pointer, int size, bool isLoadedImage, [MaybeNullWhen(false)] out MetadataReader metadataReader)
    {
        var reader = new PEBinaryReader(pointer, size);

        SkipDosHeader(ref reader, out var isCoffOnly);

        var numberOfSections = new CoffHeader(ref reader).NumberOfSections;

        DirectoryEntry corHeaderTableDirectory = default;
        if (!isCoffOnly)
        {
            corHeaderTableDirectory = new PEHeader(ref reader).CorHeaderTableDirectory;
        }

        if (numberOfSections < 0)
        {
            throw new BadImageFormatException("Invalid number of sections declared in PE header.");
        }
        Span<SectionHeader> sectionHeaders = stackalloc SectionHeader[numberOfSections];
        ReadSectionHeaders(sectionHeaders, ref reader);

        DirectoryEntry metadataDirectory = default;
        var hasMetadataDirectory = false;
        if (!isCoffOnly)
        {
            if (TryCalculateCorHeaderOffset(corHeaderTableDirectory, sectionHeaders, isLoadedImage, out var offset))
            {
                reader.Seek(offset);

                metadataDirectory = new CorHeader(ref reader).MetadataDirectory;
                hasMetadataDirectory = true;
            }
        }
        ref readonly var metadataDirectoryRef = ref (hasMetadataDirectory ? ref metadataDirectory : ref Unsafe.NullRef<DirectoryEntry>());
        CalculateMetadataLocation(sectionHeaders, isCoffOnly, isLoadedImage, in metadataDirectoryRef, size, out var metadataStartOffset, out var metadataSize);

        if (metadataSize > 0 && metadataStartOffset > 0)
        {
            metadataReader = new MetadataReader(pointer + metadataStartOffset, metadataSize);
            return true;
        }

        metadataReader = default;
        return false;
    }

    private static void SkipDosHeader(ref PEBinaryReader reader, out bool isCOFFOnly)
    {
        // Look for DOS Signature "MZ"
        var dosSig = reader.ReadUInt16();

        if (dosSig != DosSignature)
        {
            // If image doesn't start with DOS signature, let's assume it is a
            // COFF (Common Object File Format), aka .OBJ file.
            // See CLiteWeightStgdbRW::FindObjMetaData in ndp\clr\src\MD\enc\peparse.cpp

            if (dosSig != 0 || reader.ReadUInt16() != 0xffff)
            {
                isCOFFOnly = true;
                reader.Seek(0);
            }
            else
            {
                // Might need to handle other formats. Anonymous or LTCG objects, for example.
                throw new BadImageFormatException("Unknown file format.");
            }
        }
        else
        {
            isCOFFOnly = false;
        }

        if (!isCOFFOnly)
        {
            // Skip the DOS Header
            reader.Seek(PESignatureOffsetLocation);

            var ntHeaderOffset = reader.ReadInt32();
            reader.Seek(ntHeaderOffset);

            // Look for PESignature "PE\0\0"
            var ntSignature = reader.ReadUInt32();
            if (ntSignature != PESignature)
            {
                throw new BadImageFormatException("Unknown file format.");
            }
        }
    }

    private static void ReadSectionHeaders(Span<SectionHeader> sections, ref PEBinaryReader reader)
    {

        for (var i = 0; i < sections.Length; i++)
        {
            sections[i] = new SectionHeader(ref reader);
        }
    }

    private static bool TryCalculateCorHeaderOffset(DirectoryEntry corHeaderTableDirectory, Span<SectionHeader> sectionHeaders, bool isLoadedImage, out int startOffset)
    {
        if (!TryGetDirectoryOffset(sectionHeaders, corHeaderTableDirectory, isLoadedImage, out startOffset))
        {
            startOffset = -1;
            return false;
        }

        if (corHeaderTableDirectory.Size < SizeOfCorHeader)
        {
            throw new BadImageFormatException("Invalid COR header size.");
        }

        return true;
    }

    private static bool TryGetDirectoryOffset(Span<SectionHeader> sectionHeaders, DirectoryEntry directory, bool isLoadedImage,
        out int offset)
    {
        var sectionIndex = GetContainingSectionIndex(sectionHeaders, directory.RelativeVirtualAddress);
        if (sectionIndex < 0)
        {
            offset = -1;
            return false;
        }

        var relativeOffset = directory.RelativeVirtualAddress - sectionHeaders[sectionIndex].VirtualAddress;
        if (directory.Size > sectionHeaders[sectionIndex].VirtualSize - relativeOffset)
        {
            throw new BadImageFormatException("Section too small.");
        }

        offset = isLoadedImage ? directory.RelativeVirtualAddress : sectionHeaders[sectionIndex].PointerToRawData + relativeOffset;
        return true;
    }

    private static int GetContainingSectionIndex(Span<SectionHeader> sectionHeaders, int relativeVirtualAddress)
    {
        for (var i = 0; i < sectionHeaders.Length; i++)
        {
            if (sectionHeaders[i].VirtualAddress <= relativeVirtualAddress &&
                relativeVirtualAddress < sectionHeaders[i].VirtualAddress + sectionHeaders[i].VirtualSize)
            {
                return i;
            }
        }

        return -1;
    }

    internal static unsafe int FindCormetaSectionIndex(Span<SectionHeader> sectionHeaders)
    {
        for (var i = 0; i < sectionHeaders.Length; i++)
        {
            ref var sectionHeader = ref sectionHeaders[i];
            if (sectionHeader.NameUtf8.Size != 8)
            {
                continue;
            }

            if (Unsafe.Read<long>(sectionHeader.NameUtf8.Pointer) == MemoryMarshal.Read<long>(CormetaSection))
            {
                return i;
            }
        }

        return -1;
    }

    private static void CalculateMetadataLocation(Span<SectionHeader> sectionHeaders,
        bool isCoffOnly, bool isLoadedImage, ref readonly DirectoryEntry metadataDirectory,
        long peImageSize, out int start, out int size)
    {
        if (isCoffOnly)
        {
            var cormetaIndex = FindCormetaSectionIndex(sectionHeaders);
            if (cormetaIndex == -1)
            {
                start = -1;
                size = 0;
                return;
            }

            ref var sectionHeader = ref sectionHeaders[cormetaIndex];
            if (isLoadedImage)
            {
                start = sectionHeader.VirtualAddress;
                size = sectionHeader.VirtualSize;
            }
            else
            {
                start = sectionHeader.PointerToRawData;
                size = sectionHeader.SizeOfRawData;
            }
        }
        else
        {
            if (Unsafe.IsNullRef(in metadataDirectory))
            {
                start = 0;
                size = 0;
                return;
            }

            if (!TryGetDirectoryOffset(sectionHeaders, metadataDirectory, isLoadedImage, out start))
            {
                throw new BadImageFormatException("Missing data directory.");
            }

            size = metadataDirectory.Size;
        }

        if (start < 0 ||
            start >= peImageSize ||
            size <= 0 ||
            start > peImageSize - size)
        {
            throw new BadImageFormatException("Invalid metadata section span.");
        }
    }
}
