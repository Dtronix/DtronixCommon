using DtronixCommon.Collections;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DtronixCommon.Collections.Lists;
using DtronixCommon.Collections.Trees;
using DtronixCommonBenchmarks.Collections.Trees;

namespace DtronixCommonSamples
{
    class Item : IQuadTreeItem
    {
        public int QuadTreeId { get; set; } = -1;
    }


    internal class Program
    {
        private static DoubleQuadTree2<Item> _quadTreeD2Full;

        private class MyClass : IQuadTreeItem
        {
            int IQuadTreeItem.QuadTreeId { get; set; } = -1;
        }
        static void Main(string[] args)
        {
            var b = new QuadTreeBenchmarks();
            b.GlobalSetup();

            b.InsertDouble3();

            Console.ReadLine();
        }
    }
}
