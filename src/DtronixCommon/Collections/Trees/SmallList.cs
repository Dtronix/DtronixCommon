using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections.Trees
{
    public class SmallList<T>
    {
        private struct ListData
        {
            public T[] buf = new T[256];
            public T[] data;
            public int num;
            public int cap;
        }

        private ListData ld;

        public SmallList()
        {
            
        }

        public int size()
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
        
        public int find_index(T element)
        {
            throw new NotImplementedException();
        }

        public void clear()
        {
            throw new NotImplementedException();
        }

        public void reserve(int n)
        {
            throw new NotImplementedException();
        }

        public void pubsh_back(T element)
        {
            throw new NotImplementedException();
        }

        public T pop_back()
        {
            throw new NotImplementedException();
        }

        public void swap(SmallList<T> other)
        {
            throw new NotImplementedException();
        }

        public T[] data()
        {
            throw new NotImplementedException();
        }
    }
}
