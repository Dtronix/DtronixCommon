using System;
using BenchmarkDotNet.Attributes;
using DtronixCommon.Collections.Lists;

namespace DtronixCommonBenchmarks.Collections.Lists;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class ListBenchmarks
{
    private FloatList _list;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _list = new FloatList(10);
        var item = _list.Insert();
        var index = 0;

        for (int i = 0; i < 4; i++)
        {
            _list.Set(item, index++, Random.Shared.NextSingle() * 50);
        }

    }

    [Benchmark]
    public void RetrievalMultipleTimes()
    {
        var index = 0;
        float addedValue = 0;
        for (int i = 0; i < 10; i++)
        {
            addedValue += _list.Get(0, index++);
        }

    }

    [Benchmark]
    public void RetrievalOnce()
    {
        var index = 0;
        var items = _list.Get(0, 0, 10);
        float addedValue = 0;
        for (int i = 0; i < 5; i++)
        {
            addedValue += items[i];
        }

    }

}

