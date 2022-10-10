using DtronixCommon.Collections;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DtronixCommon.Collections.Trees;

namespace DtronixCommonSamples
{
    public class DesignViewVisual : IQuadTreeItem
    {
        internal int InternalQuadTreeId = -1;

        int IQuadTreeItem.QuadTreeId
        {
            get => InternalQuadTreeId;
            set => InternalQuadTreeId = value;
        }
    }
  
    internal class Program
    {
        private class MyClass : IQuadTreeItem
        {
            int IQuadTreeItem.QuadTreeId { get; set; } = -1;
        }
        static void Main(string[] args)
        {
            var qtf = new FloatQuadTree<DesignViewVisual>(float.MaxValue, float.MaxValue, 8, 8, 510 * 510);

            var visual = new DesignViewVisual();
            qtf.Insert(-1, -1, 1, 1, visual);
            qtf.Insert(-1, -1, 1, 1, visual);


            var offsetX = 2;
            var offsetY = 2;



            while (true)
            {
                for (int x = 0; x < 500; x++)
                {
                    for (int y = 0; y < 500; y++)
                    {
                        qtf.Insert(
                            x - offsetX + offsetX * x,
                            y - offsetY + offsetY * y,
                            x + offsetX + offsetX * x,
                            y + offsetY + offsetY * y, new DesignViewVisual());
                    }
                }

                qtf.Clear();
                //qtf.Walk(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue, ele => true);
            }


            Console.ReadLine();
        }
    }
}
