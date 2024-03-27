using System.Runtime.InteropServices;

namespace Qneet.TestAdapter.Metatdata;

[StructLayout(LayoutKind.Auto)]
internal unsafe readonly struct MemoryBlock(byte* pointer, int size)
{
    public readonly byte* Pointer = pointer;
    public readonly int Size = size;
}
