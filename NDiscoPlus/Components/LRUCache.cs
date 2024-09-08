using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus.Components;

/// <summary>
/// An LRU Cache implementation that does a linear search through it's values.
/// Cache hit speed depends on how recently the data was accessed.
/// Cache miss speed depends on the capacity of cache.
/// </summary>
public class LRUCache<TKey, TValue> where TKey : IEquatable<TKey>
{
    private int offset;
    private int count;
    private readonly TKey[] keys;
    private readonly TValue[] values;

    public int Count => count;
    public int Capacity => keys.Length;

    // Use byte to limit capacity to max 255
    // We want the capacity to be low as we do linear searches through the object
    public LRUCache(byte capacity)
    {
        offset = 0;
        count = 0;

        keys = new TKey[capacity];
        values = new TValue[capacity];
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueBuilder)
    {
        if (TryGet(key, out TValue? value))
            return value;

        TValue newValue = valueBuilder(key);
        Add(key, newValue);
        return newValue;
    }

    private bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        Debug.Assert(count <= Capacity);
        for (int i = 0; i < count; i++)
        {
            // iterate backwards from the most recently used item
            // most recently item is at index (offset - 1)
            // this is done so that the the items that are accessed most often can be fetched first
            // On cache:
            //   - hit: should be faster than a dictionary lookup
            //   - miss: will be slower depending on cache capacity
            int index = (offset - 1) - i;
            if (index < 0) // use if case instead since modulo returns a negative value
                index += keys.Length;

            if (key.Equals(keys[index]))
            {
                // value found
                value = values[index];
                return true;
            }
        }

        // value not found
        value = default;
        return false;
    }

    private void Add(TKey key, TValue value)
    {
        keys[offset] = key;
        values[offset] = value;

        offset++;
        if (count < Capacity)
            count++;
    }
}
