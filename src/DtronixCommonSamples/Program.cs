using DtronixCommon.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DtronixCommonSamples
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var qt = new LongQuadtree(int.MaxValue, int.MaxValue, 8, 8);

            int id = 0;
            const int MinMax = 50000;
            var rand = new Random();

            var baseX = rand.Next(1, MinMax);
            var baseY = rand.Next(1, MinMax);
            var width = rand.Next(0, MinMax);
            var height = rand.Next(0, MinMax);
            var count = rand.Next(20, 500);

            Benchmark("Inserts", () =>
            {
                Console.WriteLine($"Writing {count * count} random quads.");

                for (int x = 0; x < count; x++)
                {
                    for (int y = 0; y < count; y++)
                    {
                        qt.Insert(baseX + x, baseY + y, baseY + y + width, baseY + y + height);
                    }
                }
            });

            for (int i = 0; i < 500; i++)
            {

                
                LongList list = null;
                Benchmark("Removal Query", () =>
                {
                    list = qt.Query(0, 0, long.MaxValue, long.MaxValue, -1);
                });
                
                /*
                Benchmark("Removals", () =>
                {
                    Console.WriteLine($"Deleting {list.Size()} random quads.");

                    for (int i = 0; i < list.Size(); i++)
                    {
                        var id = list.Get(i, 0);

                        qt.Remove(id);
                    }
                });*/
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
