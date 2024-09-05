#nullable enable
// ----------------------------
// This file is auto generated.
// Any modifications to this file will be overridden
// ----------------------------
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace DtronixCommon.Collections.Lists;


/// <summary>
/// List of float with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
public class FloatList : IDisposable
{
    public class Cache
    {
        private ConcurrentQueue<Item> _cachedLists = new ConcurrentQueue<Item>();
        private readonly int _fieldCount;

        public class Item
        {
            public readonly long ExpireTime;
            public readonly FloatList List;
            private readonly ConcurrentQueue<Item> _returnQueue;

            public Item(FloatList list, ConcurrentQueue<Item> queue)
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
                return new Item(new FloatList(_fieldCount), _cachedLists);
            }

            return list;
        }
    }

    /// <summary>
    /// Contains the data.
    /// </summary>
    public float[]? Data;

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
    public FloatList(int fieldCount)
        : this(fieldCount, 128)
    {
    }

    /// <summary>
    /// Creates a new list of elements which each consist of integer fields with the specified number of elements.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="fieldCount"></param>
    /// <param name="capacity">Number of total elements this collection supports.  This ignores fieldCount.</param>
    public FloatList(int fieldCount, int capacity)
    {
        _numFields = fieldCount;
        Data = new float[capacity];
    }

    /// <summary>
    /// Returns the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Get(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        return Data![index * _numFields + field];
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
    public ReadOnlySpan<float> Get(int index, int fieldStart, int fieldCount)
    {
        return new ReadOnlySpan<float>(Data, index * _numFields + fieldStart, fieldCount);
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
    public void Set(int index, int field, float value)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field] = value;
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
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new float[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBack(ReadOnlySpan<float> values)
    {
        int newPos = (InternalCount + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new float[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBackCount(ReadOnlySpan<float> values, int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new float[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        var id = InternalCount;
        InternalCount += count;
        return id;
    }


    /// <summary>
    /// Ensures that the list has enough space to accommodate a specified number of additional elements.
    /// </summary>
    /// <param name="count">The number of additional elements that the list needs to accommodate.</param>
    /// <returns>The current count of elements in the list before the operation.</returns>
    /// <remarks>
    /// If the list does not have enough space, it reallocates the buffer, doubling its size, to make room for the new elements.
    /// </remarks>
    public bool EnsureSpaceAvailable(int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new float[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
            return true;
        }
        return false;
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
        Data![index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field]--;
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
            _freeElement = (int)Data![pos];

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
    public int Insert(ReadOnlySpan<float> values)
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = (int)Data![pos];

            // Return the free index.
            values.CopyTo(Data.AsSpan(index * _numFields));
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
        Data![pos] = _freeElement;
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

/// <summary>
/// List of double with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
public class DoubleList : IDisposable
{
    public class Cache
    {
        private ConcurrentQueue<Item> _cachedLists = new ConcurrentQueue<Item>();
        private readonly int _fieldCount;

        public class Item
        {
            public readonly long ExpireTime;
            public readonly DoubleList List;
            private readonly ConcurrentQueue<Item> _returnQueue;

            public Item(DoubleList list, ConcurrentQueue<Item> queue)
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
                return new Item(new DoubleList(_fieldCount), _cachedLists);
            }

            return list;
        }
    }

    /// <summary>
    /// Contains the data.
    /// </summary>
    public double[]? Data;

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
    public DoubleList(int fieldCount)
        : this(fieldCount, 128)
    {
    }

    /// <summary>
    /// Creates a new list of elements which each consist of integer fields with the specified number of elements.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="fieldCount"></param>
    /// <param name="capacity">Number of total elements this collection supports.  This ignores fieldCount.</param>
    public DoubleList(int fieldCount, int capacity)
    {
        _numFields = fieldCount;
        Data = new double[capacity];
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
        return Data![index * _numFields + field];
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
    public ReadOnlySpan<double> Get(int index, int fieldStart, int fieldCount)
    {
        return new ReadOnlySpan<double>(Data, index * _numFields + fieldStart, fieldCount);
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
        Data![index * _numFields + field] = value;
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
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new double[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBack(ReadOnlySpan<double> values)
    {
        int newPos = (InternalCount + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new double[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBackCount(ReadOnlySpan<double> values, int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new double[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        var id = InternalCount;
        InternalCount += count;
        return id;
    }


    /// <summary>
    /// Ensures that the list has enough space to accommodate a specified number of additional elements.
    /// </summary>
    /// <param name="count">The number of additional elements that the list needs to accommodate.</param>
    /// <returns>The current count of elements in the list before the operation.</returns>
    /// <remarks>
    /// If the list does not have enough space, it reallocates the buffer, doubling its size, to make room for the new elements.
    /// </remarks>
    public bool EnsureSpaceAvailable(int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new double[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
            return true;
        }
        return false;
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
        Data![index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field]--;
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
            _freeElement = (int)Data![pos];

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
    public int Insert(ReadOnlySpan<double> values)
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = (int)Data![pos];

            // Return the free index.
            values.CopyTo(Data.AsSpan(index * _numFields));
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
        Data![pos] = _freeElement;
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

/// <summary>
/// List of int with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
public class IntList : IDisposable
{
    public class Cache
    {
        private ConcurrentQueue<Item> _cachedLists = new ConcurrentQueue<Item>();
        private readonly int _fieldCount;

        public class Item
        {
            public readonly long ExpireTime;
            public readonly IntList List;
            private readonly ConcurrentQueue<Item> _returnQueue;

            public Item(IntList list, ConcurrentQueue<Item> queue)
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
                return new Item(new IntList(_fieldCount), _cachedLists);
            }

            return list;
        }
    }

    /// <summary>
    /// Contains the data.
    /// </summary>
    public int[]? Data;

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
    public IntList(int fieldCount)
        : this(fieldCount, 128)
    {
    }

    /// <summary>
    /// Creates a new list of elements which each consist of integer fields with the specified number of elements.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="fieldCount"></param>
    /// <param name="capacity">Number of total elements this collection supports.  This ignores fieldCount.</param>
    public IntList(int fieldCount, int capacity)
    {
        _numFields = fieldCount;
        Data = new int[capacity];
    }

    /// <summary>
    /// Returns the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Get(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        return Data![index * _numFields + field];
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
    public ReadOnlySpan<int> Get(int index, int fieldStart, int fieldCount)
    {
        return new ReadOnlySpan<int>(Data, index * _numFields + fieldStart, fieldCount);
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
    public void Set(int index, int field, int value)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field] = value;
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
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new int[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBack(ReadOnlySpan<int> values)
    {
        int newPos = (InternalCount + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new int[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBackCount(ReadOnlySpan<int> values, int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new int[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        var id = InternalCount;
        InternalCount += count;
        return id;
    }


    /// <summary>
    /// Ensures that the list has enough space to accommodate a specified number of additional elements.
    /// </summary>
    /// <param name="count">The number of additional elements that the list needs to accommodate.</param>
    /// <returns>The current count of elements in the list before the operation.</returns>
    /// <remarks>
    /// If the list does not have enough space, it reallocates the buffer, doubling its size, to make room for the new elements.
    /// </remarks>
    public bool EnsureSpaceAvailable(int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new int[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
            return true;
        }
        return false;
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
        Data![index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field]--;
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
            _freeElement = (int)Data![pos];

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
    public int Insert(ReadOnlySpan<int> values)
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = (int)Data![pos];

            // Return the free index.
            values.CopyTo(Data.AsSpan(index * _numFields));
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
        Data![pos] = _freeElement;
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

/// <summary>
/// List of long with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
public class LongList : IDisposable
{
    public class Cache
    {
        private ConcurrentQueue<Item> _cachedLists = new ConcurrentQueue<Item>();
        private readonly int _fieldCount;

        public class Item
        {
            public readonly long ExpireTime;
            public readonly LongList List;
            private readonly ConcurrentQueue<Item> _returnQueue;

            public Item(LongList list, ConcurrentQueue<Item> queue)
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
                return new Item(new LongList(_fieldCount), _cachedLists);
            }

            return list;
        }
    }

    /// <summary>
    /// Contains the data.
    /// </summary>
    public long[]? Data;

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
    public LongList(int fieldCount)
        : this(fieldCount, 128)
    {
    }

    /// <summary>
    /// Creates a new list of elements which each consist of integer fields with the specified number of elements.
    /// 'fieldCount' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="fieldCount"></param>
    /// <param name="capacity">Number of total elements this collection supports.  This ignores fieldCount.</param>
    public LongList(int fieldCount, int capacity)
    {
        _numFields = fieldCount;
        Data = new long[capacity];
    }

    /// <summary>
    /// Returns the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Get(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        return Data![index * _numFields + field];
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
    public ReadOnlySpan<long> Get(int index, int fieldStart, int fieldCount)
    {
        return new ReadOnlySpan<long>(Data, index * _numFields + fieldStart, fieldCount);
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
    public void Set(int index, int field, long value)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field] = value;
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
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new long[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBack(ReadOnlySpan<long> values)
    {
        int newPos = (InternalCount + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new long[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        return InternalCount++;
    }

    /// <summary>
    /// Inserts an element to the back of the list and adds the passed values to the data.
    /// </summary>
    /// <returns></returns>
    public int PushBackCount(ReadOnlySpan<long> values, int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new long[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
        }

        values.CopyTo(Data.AsSpan(InternalCount * _numFields));

        var id = InternalCount;
        InternalCount += count;
        return id;
    }


    /// <summary>
    /// Ensures that the list has enough space to accommodate a specified number of additional elements.
    /// </summary>
    /// <param name="count">The number of additional elements that the list needs to accommodate.</param>
    /// <returns>The current count of elements in the list before the operation.</returns>
    /// <remarks>
    /// If the list does not have enough space, it reallocates the buffer, doubling its size, to make room for the new elements.
    /// </remarks>
    public bool EnsureSpaceAvailable(int count)
    {
        int newPos = (InternalCount + count) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > Data!.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            var newArray = new long[newCap];
            Array.Copy(Data, newArray, Data.Length);
            Data = newArray;
            return true;
        }
        return false;
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
        Data![index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < InternalCount && field >= 0 && field < _numFields);
        Data![index * _numFields + field]--;
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
            _freeElement = (int)Data![pos];

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
    public int Insert(ReadOnlySpan<long> values)
    {
        // If there's a free index in the free list, pop that and use it.
        if (_freeElement != -1)
        {
            int index = _freeElement;
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = (int)Data![pos];

            // Return the free index.
            values.CopyTo(Data.AsSpan(index * _numFields));
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
        Data![pos] = _freeElement;
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
