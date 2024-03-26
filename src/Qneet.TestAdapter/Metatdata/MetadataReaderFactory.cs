using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace Qneet.TestAdapter.Metatdata;

[SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
internal static class MetadataReaderFactory
{
    internal const ushort DosSignature = 0x5A4D;     // 'M' 'Z'
    internal const int PESignatureOffsetLocation = 0x3C;
    internal const uint PESignature = 0x00004550;    // PE00
    internal const int PESignatureSize = sizeof(uint);

    internal const int SizeOfCorHeader = 72;

    public static unsafe bool TryGetMetadaReader(byte* pointer, int size, bool isLoadedImage, [MaybeNullWhen(false)] out MetadataReader metadataReader)
    {
        var reader = new PEBinaryReader(pointer, size);

        SkipDosHeader(ref reader, out var isCoffOnly);

        var coffHeader = new CoffHeader(ref reader);

        PEHeader peHeader = default;
        if (!isCoffOnly)
        {
            peHeader = new PEHeader(ref reader);
        }

        var sectionHeaders = ReadSectionHeaders(coffHeader.NumberOfSections, ref reader);

        CorHeader corHeader = default;
        var hasCorHeader = false;
        if (!isCoffOnly)
        {
            if (TryCalculateCorHeaderOffset(peHeader.CorHeaderTableDirectory, sectionHeaders, isLoadedImage, out var offset))
            {
                reader.Seek(offset);

                corHeader = new CorHeader(ref reader);
                hasCorHeader = true;
            }
        }
        ref readonly var corHeaderRef = ref (hasCorHeader ? ref corHeader : ref Unsafe.NullRef<CorHeader>());
        CalculateMetadataLocation(sectionHeaders, isCoffOnly, isLoadedImage, in corHeaderRef, size, out var metadataStartOffset, out var metadataSize);

        if (metadataSize > 0 && metadataStartOffset > 0)
        {
            metadataReader = new MetadataReader(pointer + metadataStartOffset, metadataSize);
            return true;
        }

        metadataReader = default;
        return false;
    }

    [SuppressMessage("Design", "MA0012:Do not raise reserved exception type", Justification = "<Pending>")]
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

    private static ImmutableArray<SectionHeader> ReadSectionHeaders(int numberOfSections, ref PEBinaryReader reader)
    {
        if (numberOfSections < 0)
        {
            throw new BadImageFormatException("Invalid number of sections declared in PE header.");
        }

        var builder = ImmutableArray.CreateBuilder<SectionHeader>(numberOfSections);

        for (var i = 0; i < numberOfSections; i++)
        {
            builder.Add(new SectionHeader(ref reader));
        }

        return builder.MoveToImmutable();
    }

    private static bool TryCalculateCorHeaderOffset(DirectoryEntry corHeaderTableDirectory, ImmutableArray<SectionHeader> sectionHeaders, bool isLoadedImage, out int startOffset)
    {
        if (!TryGetDirectoryOffset(sectionHeaders, corHeaderTableDirectory, isLoadedImage, out startOffset, canCrossSectionBoundary: false))
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

    private static bool TryGetDirectoryOffset(ImmutableArray<SectionHeader> sectionHeaders, DirectoryEntry directory, bool isLoadedImage,
        out int offset, bool canCrossSectionBoundary)
    {
        var sectionIndex = GetContainingSectionIndex(sectionHeaders, directory.RelativeVirtualAddress);
        if (sectionIndex < 0)
        {
            offset = -1;
            return false;
        }

        var relativeOffset = directory.RelativeVirtualAddress - sectionHeaders[sectionIndex].VirtualAddress;
        if (!canCrossSectionBoundary && directory.Size > sectionHeaders[sectionIndex].VirtualSize - relativeOffset)
        {
            throw new BadImageFormatException("Section too small.");
        }

        offset = isLoadedImage ? directory.RelativeVirtualAddress : sectionHeaders[sectionIndex].PointerToRawData + relativeOffset;
        return true;
    }

    private static int GetContainingSectionIndex(ImmutableArray<SectionHeader> sectionHeaders, int relativeVirtualAddress)
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

    internal static int IndexOfSection(ImmutableArray<SectionHeader> sectionHeaders, string name)
    {
        for (var i = 0; i < sectionHeaders.Length; i++)
        {
            if (sectionHeaders[i].Name.Equals(name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static void CalculateMetadataLocation(ImmutableArray<SectionHeader> sectionHeaders,
        bool isCoffOnly, bool isLoadedImage, ref readonly CorHeader corHeader,
        long peImageSize, out int start, out int size)
    {
        if (isCoffOnly)
        {
            var cormeta = IndexOfSection(sectionHeaders, ".cormeta");
            if (cormeta == -1)
            {
                start = -1;
                size = 0;
                return;
            }

            if (isLoadedImage)
            {
                start = sectionHeaders[cormeta].VirtualAddress;
                size = sectionHeaders[cormeta].VirtualSize;
            }
            else
            {
                start = sectionHeaders[cormeta].PointerToRawData;
                size = sectionHeaders[cormeta].SizeOfRawData;
            }
        }
        else
        {
            if (Unsafe.IsNullRef(in corHeader))
            {
                start = 0;
                size = 0;
                return;
            }

            if (!TryGetDirectoryOffset(sectionHeaders, corHeader.MetadataDirectory, isLoadedImage, out start, canCrossSectionBoundary: false))
            {
                throw new BadImageFormatException("Missing data directory.");
            }

            size = corHeader.MetadataDirectory.Size;
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
