using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections.Trees;

public class SmallList<T> : IDisposable
{
    private class ListData
    {
        public T[] buf = new T[fixed_cap];
        public T[] data;
        public int num = 0;
        public int cap = fixed_cap;
    }

    private ListData ld;

    private const int fixed_cap = 256;

    public SmallList()
    {

    }

    public SmallList(SmallList<T> other)
    {
        if (other.ld.cap == fixed_cap)
        {
            ld = other.ld;
            ld.data = ld.buf;
        }
        else
        {
            reserve(other.ld.num);
            for (int j = 0; j < other.size(); ++j)
                ld.data[j] = other.ld.data[j];
            ld.num = other.ld.num;
            ld.cap = other.ld.cap;
        }
    }



    public int size()
    {
        return ld.num;
    }

    public ref T this[int n]
    {
        get
        {
            Debug.Assert(n >= 0 && n < ld.num);
            return ref ld.data[n];
        }
    }

    public int find_index(T element)
    {
        for (int j = 0; j < ld.num; ++j)
        {
            if (ld.data[j]?.Equals(element) == true)
                return j;
        }

        return -1;
    }

    public void clear()
    {
        ld.num = 0;
    }

    public void reserve(int n)
    {
        if (n > ld.cap)
        {
            ld.data = new T[n];
            Buffer.BlockCopy(ld.buf, 0, ld.data, 0, ld.cap);
            ld.cap = n;
        }
    }

    public void push_back(T element)
    {
        if (ld.num >= ld.cap)
            reserve(ld.cap * 2);
        ld.data[ld.num++] = element;
    }

    public T pop_back()
    {
        return ld.data[--ld.num];
    }

    public void swap(SmallList<T> other)
    {
        ListData ld1 = ld;
        ListData ld2 = other.ld;

        bool use_fixed1 = ld1.data == ld1.buf;
        bool use_fixed2 = ld2.data == ld2.buf;

        (ld1, ld2) = (ld2, ld1);

        if (use_fixed1)
            ld2.data = ld2.buf;
        if (use_fixed2)
            ld1.data = ld1.buf;
    }

    public T[] data()
    {
        return ld.data;
    }

    public void Dispose()
    {
        if (ld.data != ld.buf)
            ld.data = null!;
    }
}

