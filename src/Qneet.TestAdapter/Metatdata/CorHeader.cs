using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CorHeader
{
    public readonly DirectoryEntry MetadataDirectory;

    internal const uint Size =
        sizeof(int) +
        sizeof(ushort) +
        sizeof(ushort) +
        DirectoryEntry.SizeBytes +
        4 + 4 + (DirectoryEntry.SizeBytes * 6);

    internal CorHeader(ref PEBinaryReader reader)
    {
        reader.SkipNoCheck(4 + 2 + 2);
        MetadataDirectory = new DirectoryEntry(ref reader);
        reader.SkipNoCheck(4 + 4 + (DirectoryEntry.SizeBytes * 6));
    }
}
