﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections;

/// <summary>
/// https://stackoverflow.com/a/48354356
/// </summary>
public class IntList
{
    private int[] _data = new int[128];
    private int _numFields = 0;
    private int _num = 0;
    private int _cap = 128;
    private int _freeElement = -1;


    /// <summary>
    ///Creates a new list of elements which each consist of integer fields.
    /// 'startNumFields' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name="startNumFields"></param>
    public IntList(int startNumFields)
    {
        _numFields = startNumFields;
    }

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
    /// <param name="n"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public int Get(int n, int field)
    {
        Debug.Assert(n >= 0 && n < _num && field >= 0 && field < _numFields);
        return _data[n * _numFields + field];
    }

    /// <summary>
    /// Sets the value of the specified field for the nth element.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="field"></param>
    /// <param name="val"></param>
    public void Set(int n, int field, int val)
    {
        Debug.Assert(n >= 0 && n < _num && field >= 0 && field < _numFields);
        _data[n * _numFields + field] = val;
    }

    /// <summary>
    /// Clears the list, making it empty.
    /// </summary>
    public void Clear()
    {
        _num = 0;
        _freeElement = -1;
    }

    /// <summary>
    /// Inserts an element to the back of the list and returns an index to it.
    /// </summary>
    /// <returns></returns>
    public int PushBack()
    {
        int newPos = (_num + 1) * _numFields;

        // If the list is full, we need to reallocate the buffer to make room
        // for the new element.
        if (newPos > _cap)
        {
            // Use double the size for the new capacity.
            int newCap = newPos * 2;

            // Allocate new array and copy former contents.
            int[] newArray = new int[newCap];
            Array.Copy(_data, newArray, _cap);
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
            int pos = index * _numFields;

            // Set the free index to the next free index.
            _freeElement = _data[pos];

            // Return the free index.
            return index;
        }

        // Otherwise insert to the back of the array.
        return PushBack();
    }

    /// <summary>
    /// Removes the nth element in the list.
    /// </summary>
    /// <param name="n"></param>
    public void Erase(int n)
    {
        // Push the element to the free list.
        int pos = n * _numFields;
        _data[pos] = _freeElement;
        _freeElement = n;
    }
}

