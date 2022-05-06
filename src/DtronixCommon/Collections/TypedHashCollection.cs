using System.Collections;

namespace DtronixCommon.Collections;

/// <summary>
/// Thread safe collection which groups items by their base type.  Will reject duplicates.
/// </summary>
/// <typeparam name="TBase">Base class which items will inherit from.</typeparam>
public class TypeGroupedHashCollection<TBase> : ICollection<TBase>
{
    public class FinalizedGroup
    {
        public Type Type;
        public TBase[] Items;
    }
    private HashSet<TBase> _hashItems;
    private Dictionary<Type, List<TBase>> _typedItems;
    //TODO Look at implementing a lite list which can produce the array directly via a Span slice.

    public int Count => _hashItems.Count;
    public bool IsReadOnly => false;

    public TypeGroupedHashCollection()
    {
        _hashItems = new HashSet<TBase>();
        _typedItems = new Dictionary<Type, List<TBase>>();
    }

 
    /// <summary>
    /// Returns items which match the exact type.
    /// </summary>
    /// <param name="type">Type to return.</param>
    /// <returns>Array of items which match the specified type.</returns>
    public TBase[]? GetItemsByType(Type type)
    {
        if (!_typedItems.TryGetValue(type, out var result))
            return default;
            
        return result.ToArray();
    }

    /// <summary>
    /// Get the items as they exist in their type grouping.
    /// </summary>
    /// <returns>Grouped items.</returns>
    public IReadOnlyDictionary<Type, List<TBase>> GetGroupedItems()
    {
        return _typedItems;
    }

    /// <summary>
    /// Adds item to the collection.
    /// </summary>
    /// <param name="item">Item to add.</param>
    /// <returns>True if the item was added, false otherwise.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool AddItem(TBase item)
    {
        lock (_hashItems)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!_hashItems.Add(item))
                return false;

            var type = item.GetType();
            if (!_typedItems.TryGetValue(type, out var list))
            {
                list = new List<TBase>();
                _typedItems.Add(type, list);
            }

            list.Add(item);
            return true;
        }
    }

    public void AddItems(TBase[] items)
    {
        lock (_hashItems)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                if (!_hashItems.Add(item))
                    continue;

                var type = item.GetType();
                if (!_typedItems.TryGetValue(type, out var list))
                {
                    list = new List<TBase>();
                    _typedItems.Add(type, list);
                }

                list.Add(item);
            }
        }
    }

    public IEnumerator<TBase> GetEnumerator()
    {
        return _hashItems.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _hashItems.GetEnumerator();
    }
    public void Add(TBase item)
    {
        AddItem(item);
    }

    public void Clear()
    {
        lock (_hashItems)
        {
            _hashItems.Clear();
            foreach (var typedItem in _typedItems)
                typedItem.Value.Clear();
            _typedItems.Clear();
        }
    }

    public bool Contains(TBase item)
    {
        return _hashItems.Contains(item);
    }

    public void CopyTo(TBase[] array, int arrayIndex)
    {
        lock (_hashItems)
        {
            _hashItems.CopyTo(array, arrayIndex);
        }
    }

    public TypeGroupedHashCollection<TBase> Clone()
    {
        lock (_hashItems)
        {
            var collection = new TypeGroupedHashCollection<TBase>
            {
                _hashItems = new HashSet<TBase>(_hashItems),
                _typedItems = new Dictionary<Type, List<TBase>>()
            };

            foreach (var typedItem in _typedItems)
            {
                collection._typedItems.Add(typedItem.Key, new List<TBase>(typedItem.Value));
            }

            return collection;
        }
    }

    public FinalizedGroup[] GetFinalized()
    {
        lock (_hashItems)
        {
            var finalizedGroup = new FinalizedGroup[_typedItems.Count];

            var i = 0;
            foreach (var typedItem in _typedItems)
            {
                finalizedGroup[i++] = new FinalizedGroup()
                {
                    Type = typedItem.Key,
                    Items = typedItem.Value.ToArray()
                };
            }

            return finalizedGroup;
        }
    }

    public bool Remove(TBase item)
    {
        lock (_hashItems)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!_hashItems.Remove(item))
                return false;

            _typedItems[item.GetType()].Remove(item);

            return true;
        }
    }
}