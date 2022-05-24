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
        private class MyClass : IQuadTreeItem
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            int IQuadTreeItem.QuadTreeId { get; set; }
        }
        static void Main(string[] args)
        {

            var qt = new LongQuadTree<MyClass>(int.MaxValue, int.MaxValue, 8, 8);
            var qtf = new FloatQuadTree<MyClass>(int.MaxValue, int.MaxValue, 8, 8);

            int id = 0;
            const int MinMax = 50000;
            var rand = new Random();

            var baseX = rand.Next(1, MinMax);
            var baseY = rand.Next(1, MinMax);
            var width = rand.Next(0, MinMax);
            var height = rand.Next(0, MinMax);
            var count = 500;


            var ids = new List<MyClass>(count * count);
            for (int i = 0; i < 2; i++)
            {
                Benchmark($"Inserts long {count * count} ", () =>
                {
                    for (int x = 0; x < count; x++)
                    {
                        for (int y = 0; y < count; y++)
                        {
                            qt.Insert(
                                baseX + x,
                                baseY + y,
                                baseY + y + width,
                                baseY + y + height,
                                new MyClass()
                                {
                                    Value1 = x,
                                    Value2 = y,
                                });
                        }
                    }
                });
                ids.Clear();

                Benchmark("Query long", () =>
                {
                    qt.Query(long.MinValue, long.MinValue, long.MaxValue, long.MaxValue, id =>
                    {
                        ids.Add(id);
                    });
                });
                Benchmark("Remove long", () =>
                {
                    foreach (var myClass in ids)
                    {
                        qt.Remove(myClass);
                    }
                });
            }

            for (int i = 0; i < 2; i++)
            {
                Benchmark($"Inserts Float {count * count} ", () =>
                {
                    for (int x = 0; x < count; x++)
                    {
                        for (int y = 0; y < count; y++)
                        {
                            qtf.Insert(
                                baseX + x,
                                baseY + y,
                                baseY + y + width,
                                baseY + y + height,
                                new MyClass()
                                {
                                    Value1 = x,
                                    Value2 = y,
                                });
                        }
                    }
                });
                ids.Clear();

                Benchmark("Query Float", () =>
                {
                    qtf.Query(long.MinValue, long.MinValue, long.MaxValue, long.MaxValue, id =>
                    {
                        ids.Add(id);
                    });
                });
                Benchmark("Remove Float", () =>
                {
                    foreach (var myClass in ids)
                    {
                        qtf.Remove(myClass);
                    }
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
