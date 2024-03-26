using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CorHeader
{
    public readonly DirectoryEntry MetadataDirectory;

    internal CorHeader(ref PEBinaryReader reader)
    {
        reader.Skip(4 + 2 + 2);
        MetadataDirectory = new DirectoryEntry(ref reader);
        reader.Skip(4 + 4 + (8 * 6));
    }
}
