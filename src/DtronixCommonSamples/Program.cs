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
            int IQuadTreeItem.QuadTreeId { get; set; }
        }
        static void Main(string[] args)
        {

            var qtf = new FloatQuadTree<MyClass>(float.MaxValue, float.MaxValue, 8, 8, 1024 * 1024 * 128);
            
            var offsetX = 2;
            var offsetY = 2;
            for (int x = 0; x < 500; x++)
            {
                for (int y = 0; y < 500; y++)
                {
                    qtf.Insert(
                        x - offsetX + offsetX * x,
                        y - offsetY + offsetY * y,
                        x + offsetX + offsetX * x,
                        y + offsetY + offsetY * y, new MyClass());
                }
            }

            while (true)
            {
                qtf.Walk(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue, ele => true);
            }

            return;
            var qtl = new LongQuadTree<MyClass>(long.MaxValue, long.MaxValue, 8, 8);
            var qti = new IntQuadTree<MyClass>(int.MaxValue, int.MaxValue, 8, 8);
            var qtd = new DoubleQuadTree<MyClass>(double.MaxValue, double.MaxValue, 8, 8);

            int id = 0;
            const int MinMax = 50000;
            var rand = new Random();

            var baseX = rand.Next(1, MinMax);
            var baseY = rand.Next(1, MinMax);
            var width = rand.Next(0, MinMax);
            var height = rand.Next(0, MinMax);
            var count = 5000;


            var ids = new List<MyClass>(count * count);
            for (int i = 0; i < 2; i++)
            {
                Benchmark($"Inserts int {count * count} ", () =>
                {
                    for (int x = 0; x < count * 10; x += 10)
                    {
                        for (int y = 0; y < count * 10; y += 10)
                        {
                            qti.Insert(
                                baseX + x,
                                baseY + y,
                                baseY + y + width,
                                baseY + y + height,
                                new MyClass());
                        }
                    }
                });
                ids.Clear();

                Benchmark("Query int", () =>
                {
                    qti.Query(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, id =>
                    {
                        ids.Add(id);
                        return true;
                    });
                });
                Benchmark("Remove int", () =>
                {
                    foreach (var myClass in ids)
                    {
                        qti.Remove(myClass);
                    }
                });

                Console.WriteLine();
                Benchmark($"Inserts long {count * count} ", () =>
                {
                    for (int x = 0; x < count * 10; x += 10)
                    {
                        for (int y = 0; y < count * 10; y += 10)
                        {
                            qti.Insert(
                                baseX + x,
                                baseY + y,
                                baseY + y + width,
                                baseY + y + height,
                                new MyClass());
                        }
                    }
                });
                ids.Clear();

                Benchmark("Query long", () =>
                {
                    qtl.Query(long.MinValue, long.MinValue, long.MaxValue, long.MaxValue, id =>
                    {
                        ids.Add(id);
                        return true;
                    });
                });
                Benchmark("Remove long", () =>
                {
                    foreach (var myClass in ids)
                    {
                        qtl.Remove(myClass);
                    }
                });


                Console.WriteLine();
                Benchmark($"Inserts float {count * count} ", () =>
                {
                    for (int x = 0; x < count * 10; x += 10)
                    {
                        for (int y = 0; y < count * 10; y += 10)
                        {
                            qti.Insert(
                                baseX + x,
                                baseY + y,
                                baseY + y + width,
                                baseY + y + height,
                                new MyClass());
                        }
                    }
                });
                ids.Clear();

                Benchmark("Query float", () =>
                {
                    qtf.Query(long.MinValue, long.MinValue, long.MaxValue, long.MaxValue, id =>
                    {
                        ids.Add(id);
                        return true;
                    });
                });
                Benchmark("Remove float", () =>
                {
                    foreach (var myClass in ids)
                    {
                        qtf.Remove(myClass);
                    }
                });

                Console.WriteLine();
                Benchmark($"Inserts double {count * count} ", () =>
                {
                    for (int x = 0; x < count * 10; x += 10)
                    {
                        for (int y = 0; y < count * 10; y += 10)
                        {
                            qti.Insert(
                                baseX + x,
                                baseY + y,
                                baseY + y + width,
                                baseY + y + height,
                                new MyClass());
                        }
                    }
                });
                ids.Clear();

                Benchmark("Query double", () =>
                {
                    qtd.Query(long.MinValue, long.MinValue, long.MaxValue, long.MaxValue, id =>
                    {
                        ids.Add(id);
                        return true;
                    });
                });
                Benchmark("Remove double", () =>
                {
                    foreach (var myClass in ids)
                    {
                        qtd.Remove(myClass);
                    }
                });


            }
            

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
