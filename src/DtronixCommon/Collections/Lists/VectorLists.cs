using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DtronixCommon.Collections.Lists;
/// <summary>
/// List of float with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
public class VectorFloatList : IDisposable
{
    public class Cache
    {
        private ConcurrentQueue<Item> _cachedLists = new ConcurrentQueue<Item>();
        private readonly int _fieldCount;

        public class Item
        {
            public readonly long ExpireTime;
            public readonly VectorFloatList List;
            private readonly ConcurrentQueue<Item> _returnQueue;

            public Item(VectorFloatList list, ConcurrentQueue<Item> queue)
            {
                List = list;
                _returnQueue = queue;
            }

            public void Return()
            {
                List.InternalCount = 0;
                List._freeElement = -1;
                _returnQueue.Enqueue(this);
            }
        }

        public Cache(int fieldCount)
        {
            _fieldCount = fieldCount;
        }

        public Item Get()
        {
            if (!_cachedLists.TryDequeue(out var list))
            {
                return new Item(new VectorFloatList(_fieldCount), _cachedLists);
            }

            return list;
        }
    }

    /// <summary>
    /// Contains the data.
    /// </summary>
    public Vector128<float>[] Data;
    
    /// <summary>
    /// Current number of elements the list contains.
    /// </summary>
    internal int InternalCount = 0;

    /// <summary>
    /// Index of the last free element in the array.  -1 if there are no free elements.
    /// </summary>
    private int _freeElement = -1;

    /// <summary>
    /// Number of elements the list contains.
    /// </summary>
    public int Count => InternalCount;

    /// <summary>
    /// Creates a new list of elements which each consist of integer fields with the specified number of elements.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="capacity">Number of total elements this collection supports.  This ignores fieldCount.</param>
    public VectorFloatList(int capacity)
    {
        Data = new Vector128<float>[capacity];
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        InternalCount = 0;
        _freeElement = -1;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBack(in Vector128<float> values)
    {
        int newPos = InternalCount + 1;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new Vector128<float>[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        Data[InternalCount] = values;

        return InternalCount++;
    }

    /// <summary>
    /// Ensures that the list has enough space to accommodate a specified number of additional elements.
    /// </summary>
    /// <param name="count">The number of additional elements that the list needs to accommodate.</param>
    /// <returns>The current count of elements in the list before the operation.</returns>
    /// <remarks>
    /// If the list does not have enough space, it reallocates the buffer, doubling its size, to make room for the new elements.
    /// </remarks>
    public void EnsureSpaceAvailable(int count)
    {
        int newPos = InternalCount + count;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new Vector128<float>[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }
    }

    /// <summary>
    /// Removes the element at the back of the list.
    /// </summary>
    public void PopBack()
    {
        // Just decrement the list size.
        Debug.Assert(InternalCount > 0);
        --InternalCount;
    }

    /// <summary>
    /// Inserts an element to a vacant position in the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int Insert(in Vector128<float> values)
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;

            // Set the free index to the next free index.
            _freeElement = (int)Data[index][0];

            // Return the free index.
            Data[index] = values;
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack(values);
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="index"></param>
    public void Erase(int index)
    {
        // Push the element to the free list.
        Data[index] = Vector128.CreateScalarUnsafe<float>(_freeElement);
        _freeElement = index;
    }

    /// <summary>
    /// Disposes of the list.
    /// </summary>
    public void Dispose()
    {
        Data = null;
    }
}
