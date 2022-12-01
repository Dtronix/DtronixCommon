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
            BenchmarkRunner.Run<ListBenchmarks>();
            //BenchmarkRunner.Run<SettingPropertiesBenchmarks>();
        }
    }
}
