using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Qneet.TestAdapter;

internal ref struct NamespaceCache(MetadataReader metadataReader)
{
    private readonly record struct CacheItem(StringHandle StringHandle, string Name)
    {
    }

    private readonly MetadataReader m_metadataReader = metadataReader;
    private CacheItem[] m_items = new CacheItem[16];
    private int m_pos = 0;

    public string GetName(StringHandle handle)
    {
        if (TryFind(handle, out var ns))
        {
            return ns.Name;
        }
        return AddItem(handle).Name;
    }

    private readonly bool TryFind(StringHandle handle, out CacheItem ns)
    {
        ref var item = ref MemoryMarshal.GetArrayDataReference(m_items);
        for (var i = 0; i < m_pos; i++)
        {
            if (item.StringHandle == handle)
            {
                ns = item;
                return true;
            }
            item = Unsafe.Add(ref item, 1);
        }

        ns = default;
        return false;
    }

    private CacheItem AddItem(StringHandle handle)
    {
        var str = m_metadataReader.GetString(handle);
        var ns = new CacheItem(handle, str);
        m_pos++;
        if (m_pos == m_items.Length)
        {
            Grow();
        }
        m_items[m_pos] = ns;
        return ns;
    }

    private void Grow()
    {
        var oldItems = m_items;
        var capacity = oldItems.Length;
        var nextCapacity = 2 * capacity;

        if ((uint)nextCapacity > (uint)Array.MaxLength)
        {
            nextCapacity = Math.Max(capacity + 1, Array.MaxLength);
        }

        var next = new CacheItem[nextCapacity];

        oldItems.AsSpan().CopyTo(next);
        m_items = next;
    }
}
