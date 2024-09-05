using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DtronixCommonBenchmarks.Collections.Lists;
using DtronixCommonBenchmarks.Collections.Trees;
using DtronixCommonBenchmarks.Reflection;

namespace DtronixCommonBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            var summary = BenchmarkRunner.Run<QuadTreeBenchmarks>(config, args);
            Console.ReadLine();
        }
    }
}
