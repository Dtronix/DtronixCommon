using DtronixCommon.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DtronixCommon.Collections.Trees;

namespace DtronixCommonSamples
{
    internal class Program
    {
        private class MyClass : IQuad
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public long Id { get; set; }
            public long X1 { get; set; }
            public long X2 { get; set; }
            public long Y1 { get; set; }
            public long Y2 { get; set; }
        }
        static void Main(string[] args)
        {
            
            var qt = new QuadTree<MyClass>(int.MaxValue, int.MaxValue, 8, 8);

            int id = 0;
            const int MinMax = 50000;
            var rand = new Random();

            var baseX = rand.Next(1, MinMax);
            var baseY = rand.Next(1, MinMax);
            var width = rand.Next(0, MinMax);
            var height = rand.Next(0, MinMax);
            var count = rand.Next(400, 500);

            Benchmark("Inserts", () =>
            {
                Console.WriteLine($"Writing {count * count} random quads.");

                for (int x = 0; x < count; x++)
                {
                    for (int y = 0; y < count; y++)
                    {
                        qt.Insert(
                            new MyClass()
                            {
                                Value1 = x,
                                Value2 = y,
                                X1 = baseX + x,
                                Y1 = baseY + y,
                                X2 = baseY + y + width,
                                Y2 = baseY + y + height,
                            });
                    }
                }
            });
            var ids = new List<MyClass>(count * count);
            for (int i = 0; i < 10; i++)
            {
                ids.Clear();

                Benchmark("Query", () =>
                {
                    var list = qt.Query(0, 0, long.MaxValue, long.MaxValue, -1);
                });
                Benchmark("Traverse Query", () =>
                {
                    qt.QueryTraverse(0, 0, long.MaxValue, long.MaxValue, id =>
                    {
                        ids.Add(id);
                    });
                });
            }


            /*
            id = 0;
            Benchmark("Remove Quadrant", () =>
            {
                var list = qt.Query(0, 0, 100, 100, -1);
                for (int i = 0; i < list.Size(); i++)
                {
                    qt.Remove(list.Get(i, 0));
                }
            });

            Benchmark("Cleanup", () =>
            {
                qt.Cleanup();
            });

            Benchmark("QueryList", () =>
            {
                var list = qt.Query(0, 0, 500, 500, -1);
            });

            Benchmark("QueryList2", () =>
            {
                var list = qt.Query(0, 0, 500, 500, -1);
            });

            Benchmark("QueryList3", () =>
            {
                var list = qt.Query(0, 0, 500, 500, -1);
            });*/
            Console.ReadLine();
        }

        static void Benchmark(string name, Action action)
        {
            Console.WriteLine($"{name} Started...");
            long startMemory = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();

            action();

            var complete = sw.ElapsedMilliseconds;

            long endMemory = GC.GetTotalMemory(true);

            Console.WriteLine($"{name} Completed. Elapsed: {complete:N1}ms; Memory Usage :{(endMemory - startMemory) / 1024:N1}KB");
        }
    }
}
