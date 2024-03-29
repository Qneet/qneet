using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal readonly struct DirectoryEntry
{
    public const uint SizeBytes = sizeof(int) * 2;

    public readonly int RelativeVirtualAddress;
    public readonly int Size;

    internal DirectoryEntry(ref PEBinaryReader reader)
    {
        RelativeVirtualAddress = reader.ReadInt32NoCheck();
        Size = reader.ReadInt32NoCheck();
    }
}
