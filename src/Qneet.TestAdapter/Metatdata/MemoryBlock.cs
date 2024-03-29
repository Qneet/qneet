using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal unsafe readonly struct MemoryBlock(byte* pointer, uint size)
{
    public readonly byte* Pointer = pointer;
    public readonly uint Size = size;
}
