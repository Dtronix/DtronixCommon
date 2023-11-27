using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DtronixCommon.Collections.Lists;

/// <summary>
/// List of double with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
internal class DoubleList2 : IDisposable
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ValType
    {
        [FieldOffset(0)] // 1 byte
        internal double Value;
        [FieldOffset(0)] // 4 bytes
        internal int IntValue;

        public static implicit operator ValType(double d) => new ValType() { Value = d };
    }


    public class Cache
    {
        private ConcurrentQueue<Item> _cachedLists = new ConcurrentQueue<Item>();
        private readonly int _fieldCount;

        public class Item
        {
            public readonly long ExpireTime;
            public readonly DoubleList2 List;
            private readonly ConcurrentQueue<Item> _returnQueue;

            public Item(DoubleList2 list, ConcurrentQueue<Item> queue)
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
                return new Item(new DoubleList2(_fieldCount), _cachedLists);
            }

            return list;
        }
    }

    /// <summary>
    /// Contains the data.
    /// </summary>
    private ValType[]? _data;

    /// <summary>
    /// Number of fields which are used in the list.  This number is multuplied 
    /// </summary>
    private int _numFields = 0;

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
    /// Creates a new list of elements which each consist of integer fields.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// Capacity starts starts at 128.
    /// </summary>
    /// <param name="fieldCount">Number of fields </param>
    public DoubleList2(int fieldCount)
        : this(fieldCount, 128)
    {
    }

    /// <summary>
    /// Creates a new list of elements which each consist of integer fields with the specified number of elements.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="fieldCount"></param>
    /// <param name="capacity">Number of total elements this collection supports.  This ignores fieldCount.</param>
    public DoubleList2(int fieldCount, int capacity)
    {
        _numFields = fieldCount;
        _data = new ValType[capacity];
    }

    /// <summary>
    /// Returns the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Get(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        return _data![index * _numFields + field].Value;
    }

    /// <summary>
    /// Returns the range of values for the specified element.
    /// WARNING: Does not perform bounds checks.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="fieldStart">Starting position of the field.</param>
    /// <param name="fieldCount">
    /// Nubmer of fields to return.  Make sure to not let this run outside
    /// of the max and min ranges for fields.</param>
    /// <returns>Span of data for the range</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ValType> Get(int index, int fieldStart, int fieldCount)
    {
        return new ReadOnlySpan<ValType>(_data, index * _numFields + fieldStart, fieldCount);

       ref var type = ref Get2(index, fieldStart);

    }

    public ref ValType Get2(int index, int fieldStart)
    {
        return ref Unsafe.AddByteOffset(ref _data![0], new IntPtr((index * _numFields + fieldStart * 8)));
    }

    /// <summary>
    /// Returns an integer from the currently passed field.
    /// </summary>
    /// <param name="index">index of the element to retrieve</param>
    /// <param name="field">Field of the element to retrieve.</param>
    /// <returns>Interger of the specified element field.</returns>
    public int GetInt(int index, int field)
    {
        return (int)Get(index, field);
    }

    /// <summary>
    /// Sets the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    public void Set(int index, int field, double value)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        _data![index * _numFields + field].Value = value;
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
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = (InternalCount + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new ValType[newCap];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBack(ReadOnlySpan<ValType> values)
    {
        int newPos = (InternalCount + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new ValType[newCap];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        values.CopyTo(_data.AsSpan(InternalCount * _numFields));

        return InternalCount++;
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

    public void Increment(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        _data![index * _numFields + field].IntValue++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        _data![index * _numFields + field].IntValue--;
    }

    /// <summary>
    /// Inserts an element to a vacant position in the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int Insert()
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = _data![pos].IntValue;

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    /// <summary>
    /// Inserts an element to a vacant position in the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int Insert(ReadOnlySpan<ValType> values)
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = _data![pos].IntValue;

            // Return the free index.
            values.CopyTo(_data.AsSpan(index * _numFields));
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
        int pos = index * _numFields;
        _data![pos].IntValue = _freeElement;
        _freeElement = index;
    }

    /// <summary>
    /// Disposes of the list.
    /// </summary>
    public void Dispose()
    {
        _data = null;
    }
}
