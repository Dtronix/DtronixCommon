using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections.Trees
{
    public class FreeList<T>
    {
        // TODO: Review this further.
        public struct FreeElement
        { 
            public T element;
            public int next;
        }

        private SmallList<FreeElement> data = new SmallList<FreeElement>();
        private int first_free = -1;

        public FreeList()
        {
            
        }

        public int insert(T element)
        {
            if (first_free != -1)
            {
                int index = first_free;
                first_free = data[first_free].next;
                data[index].element = element;
                return index;
            }
            else
            {
                var fe = new FreeElement
                {
                    element = element
                };
                data.push_back(fe);
                return data.size() - 1;
            }
        }


        public void erase(int n)
        {
            Debug.Assert(n >= 0 && n < data.size());
            data[n].next = first_free;
            first_free = n;
        }


        public void clear()
        {
            data.clear();
            first_free = -1;
        }

        public int range()
        {
            return data.size();
        }

        public ref T this[int n] => ref data[n].element;

        public void reserve(int n)
        {
            data.reserve(n);
        }
        public void swap(FreeList<T> other)
        {
            int temp = first_free;
            data.swap(other.data);
            first_free = other.first_free;
            other.first_free = temp;
        }

    }
}
