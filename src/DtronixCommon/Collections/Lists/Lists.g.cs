// ----------------------------
// This file is auto generated.
// Any modifications to this file will be overridden
// ----------------------------
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DtronixCommon.Collections.Lists;

/// <summary>
/// List of float with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
internal class FloatList
{
    /// <summary>
    /// Contains the data.
    /// </summary>
    private float[] _data;

    /// <summary>
    /// Number of fields which are used in the list.  This number is multuplied 
    /// </summary>
    private int _numFields = 0;

    /// <summary>
    /// Current number of elements the list contains.
    /// </summary>
    private int _count = 0;

    /// <summary>
    /// Index of the last free element in the array.  -1 if there are no free elements.
    /// </summary>
    private int _freeElement = -1;

    /// <summary>
    /// Number of elements the list contains.
    /// </summary>
    public int Count => _count;

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
        _data = new float[capacity];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        return _data[index * _numFields + field];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field] = value;
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _freeElement = -1;
    }

    /// <summary>
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = (_count + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _data.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            float[] newArray = new float[newCap];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        return _count++;
    }

    /// <summary>
    /// Removes the element at the back of the list.
    /// </summary>
    public void PopBack()
    {
        // Just decrement the list size.
        Debug.Assert(_count > 0);
        --_count;
    }

    public void Increment(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]--;
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
            _freeElement = (int)_data[pos];

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="index"></param>
    public void Erase(int index)
    {
        // Push the element to the free list.
        int pos = index * _numFields;
        _data[pos] = _freeElement;
        _freeElement = index;
    }
}
/// <summary>
/// List of double with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
internal class DoubleList
{
    /// <summary>
    /// Contains the data.
    /// </summary>
    private double[] _data;

    /// <summary>
    /// Number of fields which are used in the list.  This number is multuplied 
    /// </summary>
    private int _numFields = 0;

    /// <summary>
    /// Current number of elements the list contains.
    /// </summary>
    private int _count = 0;

    /// <summary>
    /// Index of the last free element in the array.  -1 if there are no free elements.
    /// </summary>
    private int _freeElement = -1;

    /// <summary>
    /// Number of elements the list contains.
    /// </summary>
    public int Count => _count;

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
        _data = new double[capacity];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        return _data[index * _numFields + field];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field] = value;
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _freeElement = -1;
    }

    /// <summary>
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = (_count + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _data.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            double[] newArray = new double[newCap];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        return _count++;
    }

    /// <summary>
    /// Removes the element at the back of the list.
    /// </summary>
    public void PopBack()
    {
        // Just decrement the list size.
        Debug.Assert(_count > 0);
        --_count;
    }

    public void Increment(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]--;
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
            _freeElement = (int)_data[pos];

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="index"></param>
    public void Erase(int index)
    {
        // Push the element to the free list.
        int pos = index * _numFields;
        _data[pos] = _freeElement;
        _freeElement = index;
    }
}
/// <summary>
/// List of int with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
public class IntList
{
    /// <summary>
    /// Contains the data.
    /// </summary>
    private int[] _data;

    /// <summary>
    /// Number of fields which are used in the list.  This number is multuplied 
    /// </summary>
    private int _numFields = 0;

    /// <summary>
    /// Current number of elements the list contains.
    /// </summary>
    private int _count = 0;

    /// <summary>
    /// Index of the last free element in the array.  -1 if there are no free elements.
    /// </summary>
    private int _freeElement = -1;

    /// <summary>
    /// Number of elements the list contains.
    /// </summary>
    public int Count => _count;

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
        _data = new int[capacity];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        return _data[index * _numFields + field];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field] = value;
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _freeElement = -1;
    }

    /// <summary>
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = (_count + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _data.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            int[] newArray = new int[newCap];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        return _count++;
    }

    /// <summary>
    /// Removes the element at the back of the list.
    /// </summary>
    public void PopBack()
    {
        // Just decrement the list size.
        Debug.Assert(_count > 0);
        --_count;
    }

    public void Increment(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]--;
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
            _freeElement = (int)_data[pos];

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="index"></param>
    public void Erase(int index)
    {
        // Push the element to the free list.
        int pos = index * _numFields;
        _data[pos] = _freeElement;
        _freeElement = index;
    }
}
/// <summary>
/// List of long with varying size with a backing array.  Items erased are returned to be reused.
/// </summary>
/// <remarks>https://stackoverflow.com/a/48354356</remarks>
internal class LongList
{
    /// <summary>
    /// Contains the data.
    /// </summary>
    private long[] _data;

    /// <summary>
    /// Number of fields which are used in the list.  This number is multuplied 
    /// </summary>
    private int _numFields = 0;

    /// <summary>
    /// Current number of elements the list contains.
    /// </summary>
    private int _count = 0;

    /// <summary>
    /// Index of the last free element in the array.  -1 if there are no free elements.
    /// </summary>
    private int _freeElement = -1;

    /// <summary>
    /// Number of elements the list contains.
    /// </summary>
    public int Count => _count;

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
        _data = new long[capacity];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        return _data[index * _numFields + field];
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
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field] = value;
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _freeElement = -1;
    }

    /// <summary>
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = (_count + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _data.Length)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            long[] newArray = new long[newCap];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        return _count++;
    }

    /// <summary>
    /// Removes the element at the back of the list.
    /// </summary>
    public void PopBack()
    {
        // Just decrement the list size.
        Debug.Assert(_count > 0);
        --_count;
    }

    public void Increment(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]++;
    }

    public void Decrement(int index, int field)
    {
        Debug.Assert(index >= 0 && index < _count && field >= 0 && field < _numFields);
        _data[index * _numFields + field]--;
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
            _freeElement = (int)_data[pos];

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="index"></param>
    public void Erase(int index)
    {
        // Push the element to the free list.
        int pos = index * _numFields;
        _data[pos] = _freeElement;
        _freeElement = index;
    }
}
