using System.Reflection.Metadata;

namespace Qneet.TestAdapter;

internal ref struct NamespaceCache(MetadataReader metadataReader)
{
    private readonly record struct CacheItem(StringHandle StringHandle, string Name)
    {
    }

    private readonly MetadataReader m_metadataReader = metadataReader;
    private Span<CacheItem> m_items = new CacheItem[16];
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
        foreach (var item in m_items.Slice(0, m_pos))
        {
            if (item.StringHandle == handle)
            {
                ns = item;
                return true;
            }
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
        var capacity = m_items.Length;
        var nextCapacity = 2 * capacity;

        if ((uint)nextCapacity > (uint)Array.MaxLength)
        {
            nextCapacity = Math.Max(capacity + 1, Array.MaxLength);
        }

        var next = new CacheItem[nextCapacity];

        m_items.CopyTo(next);
        m_items = next;
    }
}
