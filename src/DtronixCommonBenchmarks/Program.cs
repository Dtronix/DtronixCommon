using BenchmarkDotNet.Running;
using DtronixCommonBenchmarks.Allocations;
using DtronixCommonBenchmarks.Collections.Lists;
using DtronixCommonBenchmarks.Collections.Trees;

namespace DtronixCommonBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<AllocationBenchmarks>();
        }
    }
}
