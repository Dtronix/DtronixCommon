using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections.Trees
{
    public class FreeList<T>
    {
        // TODO: Review this further.
        [StructLayout(LayoutKind.Explicit)]
        public struct FreeElement
        {
            [FieldOffset(0)] public T element;
            [FieldOffset(0)] public int next;
        }

        private SmallList<FreeElement> data;
        private int first_free;

        public FreeList()
        {
            
        }

        public int insert(T element)
        {
            throw new NotImplementedException();
        }


        public void erase(int n)
        {
            throw new NotImplementedException();
        }


        public void clear()
        {
            throw new NotImplementedException();
        }

        public int range()
        {
            throw new NotImplementedException();
        }

        public T this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void reserve(int n)
        {
            throw new NotImplementedException();
        }
        public void swap(SmallList<T> other)
        {
            throw new NotImplementedException();
        }

    }
}
