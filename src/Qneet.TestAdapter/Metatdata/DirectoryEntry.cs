using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "<Pending>")]
[SuppressMessage("Design", "MA0104:Do not create a type with a name from the BCL", Justification = "<Pending>")]
public readonly struct DirectoryEntry
{
    public readonly int RelativeVirtualAddress;
    public readonly int Size;

    public DirectoryEntry(int relativeVirtualAddress, int size)
    {
        RelativeVirtualAddress = relativeVirtualAddress;
        Size = size;
    }

    internal DirectoryEntry(ref PEBinaryReader reader)
    {
        RelativeVirtualAddress = reader.ReadInt32();
        Size = reader.ReadInt32();
    }
}
