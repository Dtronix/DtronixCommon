using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DtronixCommon.Collections.Trees;


public class GenericList<T>
    where T : class
{
    private const int  InitialSize = 128;
    private T[] _data = new T[InitialSize];
    private int _num = 0;
    private int _cap = 128;
    private int _freeElement = -1;
    private int[] _freeElements = new int[InitialSize];


    /// <summary>
    /// Returns the number of elements in the list.
    /// </summary>
    /// <returns></returns>
    public int Size()
    {
        return _num;
    }

    /// <summary>
    /// Returns the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public T Get(int index)
    {
        Debug.Assert(index >= 0 && index < _num);
        return Unsafe.As<T>(_data[index]);
    }

    /// <summary>
    /// Sets the value of the specified field for the nth element.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    public void Set(int index, T value)
    {
        Debug.Assert(index >= 0 && index < _num);
        _data[index] = value;
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        _num = 0;
        _freeElement = -1;
        _freeElements = new int[InitialSize];
    }

    /// <summary>
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = _num + 1;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _cap)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            object[] newArray = new object[newCap];
            Array.Copy(_data, 0, newArray, 0, _cap);
            _data = newArray;

            // Set the old capacity to the new capacity.
            _cap = newCap;
        }

        return _num++;
    }

    /// <summary>
    /// Removes the element at the back of the list.
    /// </summary>
    public void PopBack()
    {
        // Just decrement the list size.
        Debug.Assert(_num > 0);
        --_num;
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
            // Set the free index to the next free index.
            _freeElement = (int)_data[index];

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    public int Insert(T value)
    {
        var insertId = Insert();
        Set(insertId, value);
        return insertId;
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="n"></param>
    public void Erase(int n)
    {
        // Push the element to the free list.
        _data[n] = _freeElement;
        _freeElement = n;
    }
}

