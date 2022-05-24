using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DtronixCommonCodeGeneration.Collections.Lists
{

    [Generator]
    public class ListGenerator : ISourceGenerator
    {
        private class Config
        {
            public string ClassName { get; set; }
            public string NumberType { get; set; }
            public string Visibility { get; set; }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var configs = new Config[]
            {
                new Config()
                {
                    ClassName = "FloatList",
                    NumberType = "float",
                    Visibility = "internal"
                },
                new Config()
                {
                    ClassName = "DoubleList",
                    NumberType = "double",
                    Visibility = "internal"
                },
                new Config()
                {
                    ClassName = "IntList",
                    NumberType = "int",
                    Visibility = "public"
                },
                new Config()
                {
                    ClassName = "LongList",
                    NumberType = "long",
                    Visibility = "internal"
                }
            };
            foreach (var config in configs)
            {
                var sourceBuilder = new StringBuilder(@"
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DtronixCommon.Collections.Lists;

/// <summary>
/// https://stackoverflow.com/a/48354356
/// </summary>
" + config.Visibility + @" class " + config.ClassName + @"
{
    private " + config.NumberType + @"[] _data = new " + config.NumberType + @"[128];
    private int _numFields = 0;
    private int _num = 0;
    private int _cap = 128;
    private int _freeElement = -1;

    /// <summary>
    ///Creates a new list of elements which each consist of integer fields.
    /// 'startNumFields' specifies the number of integer fields each element has.
    /// </summary>
    /// <param name=""startNumFields""></param>
    public " + config.ClassName + @"(int startNumFields)
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
    /// <param name=""n""></param>
    /// <param name=""field""></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public " + config.NumberType + @" Get(int n, int field)
    {
        Debug.Assert(n >= 0 && n < _num && field >= 0 && field < _numFields);
        return _data[n * _numFields + field];
    }

    public int GetInt(int n, int field)
    {
        return (int)Get(n, field);
    }
    /// <summary>
    /// Sets the value of the specified field for the nth element.
    /// </summary>
    /// <param name=""n""></param>
    /// <param name=""field""></param>
    /// <param name=""value""></param>
    public void Set(int n, int field, " + config.NumberType + @" value)
    {
        Debug.Assert(n >= 0 && n < _num && field >= 0 && field < _numFields);
        _data[n * _numFields + field] = value;
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
            " + config.NumberType + @"[] newArray = new " + config.NumberType + @"[newCap];
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

    public void Increment(int n, int field)
    {
        Debug.Assert(n >= 0 && n < _num && field >= 0 && field < _numFields);
        _data[n * _numFields + field]++;
    }


    public void Decrement(int n, int field)
    {
        Debug.Assert(n >= 0 && n < _num && field >= 0 && field < _numFields);
        _data[n * _numFields + field]--;
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
    /// <param name=""n""></param>
    public void Erase(int n)
    {
        // Push the element to the free list.
        int pos = n * _numFields;
        _data[pos] = _freeElement;
        _freeElement = n;
    }
}

");
                // inject the created source into the users compilation
                context.AddSource(config.ClassName + ".g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            }


        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}