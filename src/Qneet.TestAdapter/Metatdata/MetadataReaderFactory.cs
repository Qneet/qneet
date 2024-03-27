using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
internal static class MetadataReaderFactory
{
    private static ReadOnlySpan<byte> CormetaSectionName => ".cormeta"u8;

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
        for (var i = 0; i < sectionHeaders.Length; i++)
        {
            sectionHeaders[i] = new SectionHeader(ref reader);
        }

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
        ref var sectionHeader = ref GetContainingSectionIndex(sectionHeaders, directory.RelativeVirtualAddress);
        if (Unsafe.IsNullRef(in sectionHeader))
        {
            offset = -1;
            return false;
        }

        var relativeOffset = directory.RelativeVirtualAddress - sectionHeader.VirtualAddress;
        if (directory.Size > sectionHeader.VirtualSize - relativeOffset)
        {
            throw new BadImageFormatException("Section too small.");
        }

        offset = isLoadedImage ? directory.RelativeVirtualAddress : sectionHeader.PointerToRawData + relativeOffset;
        return true;
    }

    private static ref SectionHeader GetContainingSectionIndex(Span<SectionHeader> sectionHeaders, int relativeVirtualAddress)
    {
        for (var i = 0; i < sectionHeaders.Length; i++)
        {
            ref var sectionHeader = ref sectionHeaders[i];
            if (sectionHeader.VirtualAddress <= relativeVirtualAddress &&
                relativeVirtualAddress < sectionHeader.VirtualAddress + sectionHeader.VirtualSize)
            {
                return ref sectionHeader;
            }
        }

        return ref Unsafe.NullRef<SectionHeader>();
    }

    internal static unsafe ref SectionHeader FindCormetaSectionIndex(Span<SectionHeader> sectionHeaders)
    {
        var cormetaSectionName = MemoryMarshal.Read<long>(CormetaSectionName);
        for (var i = 0; i < sectionHeaders.Length; i++)
        {
            ref var sectionHeader = ref sectionHeaders[i];
            if (sectionHeader.NameUtf8.Size != 8)
            {
                continue;
            }

            if (Unsafe.Read<long>(sectionHeader.NameUtf8.Pointer) == cormetaSectionName)
            {
                return ref sectionHeader;
            }
        }

        return ref Unsafe.NullRef<SectionHeader>();
    }

    private static void CalculateMetadataLocation(Span<SectionHeader> sectionHeaders,
        bool isCoffOnly, bool isLoadedImage, ref readonly DirectoryEntry metadataDirectory,
        long peImageSize, out int start, out int size)
    {
        if (isCoffOnly)
        {
            ref var cormetaSection = ref FindCormetaSectionIndex(sectionHeaders);
            if (Unsafe.IsNullRef(in cormetaSection))
            {
                start = -1;
                size = 0;
                return;
            }

            if (isLoadedImage)
            {
                start = cormetaSection.VirtualAddress;
                size = cormetaSection.VirtualSize;
            }
            else
            {
                start = cormetaSection.PointerToRawData;
                size = cormetaSection.SizeOfRawData;
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
