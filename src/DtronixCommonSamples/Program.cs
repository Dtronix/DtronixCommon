using DtronixCommon.Collections;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Threading;
using DtronixCommon.Collections.Trees;

namespace DtronixCommonSamples
{
    public class DesignViewVisual : IQuadTreeItem
    {
        public Vector128<float> Vector { get; }
        internal int InternalQuadTreeId = -1;

        public DesignViewVisual(Vector128<float> vector)
        {
            Vector = vector;
        }

        public override string ToString()
        {
            return $"MinX: {Vector[0]:F}, MinY: {Vector[1]:F}, MaxX: {Vector[2]:F}, MaxY: {Vector[3]:F}";
        }

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
            var qtf = new VectorFloatQuadTree<DesignViewVisual>(200_000, 200_000, 16, 12, 510 * 510);

            //var visual = new DesignViewVisual();
            //qtf.Insert(Vector128.Create(-1, -1, 1, 1f), visual);
            //qtf.Insert(Vector128.Create(-1, -1, 1, 1f), visual);


            var offsetX = 50;
            var offsetY = 0;
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            
            for (int y = 0; y < 8000; y++)
            {
                var rectX = Random.Shared.NextSingle() * Random.Shared.Next(1, 8000) + offsetX;
                var rectY = Random.Shared.NextSingle() * Random.Shared.Next(1, 8000) + offsetY;
                var rectMaxX = rectX + Random.Shared.Next(1, 50);
                var rectMaxY = rectY + Random.Shared.Next(1, 50);

                minX = Math.Min(minX, rectX);
                minY = Math.Min(minY, rectY);
                maxX = Math.Max(maxX, rectMaxX);
                maxY = Math.Max(maxY, rectMaxY);

                var vector = Vector128.Create(
                    rectX,
                    rectY,
                    rectMaxX,
                    rectMaxY);
                qtf.Insert(vector, new DesignViewVisual(vector));
            }
            
            /*
            for (int x = 0; x < 10; x++)
            {
                var rectX = (float)x * offsetX;
                var rectY = 0f;
                var rectMaxX = rectX + Random.Shared.Next(1, 50);
                var rectMaxY = rectY + Random.Shared.Next(1, 50);

                minX = Math.Min(minX, rectX);
                minY = Math.Min(minY, rectY);
                maxX = Math.Max(maxX, rectMaxX);
                maxY = Math.Max(maxY, rectMaxY);

                var vector = Vector128.Create(
                    rectX,
                    rectY,
                    rectMaxX,
                    rectMaxY);
                qtf.Insert(vector, new DesignViewVisual(vector));
            }*/
            var list = new List<DesignViewVisual>();
            //qtf.Clear();
            qtf.Walk(Vector128.Create(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue),
                ele =>
                {
                    list.Add(ele);
                    return true;
                });

            //var s = qtf.DirectionalExtents(
            //    Vector128.Create(float.MinValue, float.MinValue, 0, float.MaxValue),
            //    VectorFloatQuadTree<DesignViewVisual>.Direction.MinX);
            float minXSearchValue = float.MaxValue;
            float minXSearch = float.MaxValue;
            var hs = new HashSet<int>();
            while (true)
            {

                (minXSearch, var nodeId) = qtf.FindFirstLeafBounds(
                    Vector128.Create(float.MinValue, float.MinValue, minXSearch, float.MaxValue), 0, hs);

                if (nodeId == -1 || !hs.Add(nodeId))
                    break;

                minXSearchValue = MathF.Min(minXSearchValue, minXSearch);

                minXSearch = MathF.BitDecrement(minXSearch);

            }

            var list2 = new List<DesignViewVisual>();
            //qtf.Clear();
            //qtf.Walk2(Vector128.Create(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue),
            //    ele =>
            //    {
            //        list2.Add(ele);
            //        return true;
            //    },
            //    VectorFloatQuadTree<DesignViewVisual>.Direction.MinX);

        }
    }
}
