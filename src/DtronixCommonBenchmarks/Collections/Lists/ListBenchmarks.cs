using System;
using BenchmarkDotNet.Attributes;
using DtronixCommon.Collections.Lists;

namespace DtronixCommonBenchmarks.Collections.Lists;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class ListBenchmarks
{
    private IntList _list;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _list = new IntList(10);
        var item = _list.Insert();
        var index = 0;

        for (int i = 0; i < 4; i++)
        {
            _list.Set(item, index++, Random.Shared.Next(1, Int32.MaxValue));
        }

    }

    //[Benchmark]
    public void RetrievalMultipleTimes()
    {
        var index = 0;
        float addedValue = 0;
        for (int i = 0; i < 10; i++)
        {
            addedValue += _list.Get(0, index++);
        }

    }

    //[Benchmark]
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

    [Benchmark]
    public void Get()
    {
        var value = _list.Get(0, 0);
    }

    [Benchmark]
    public void SetGetBitwise()
    {
        _list.Set(0, 0, (int)(_list.Get(0, 0) & ~0x80000000));
    }

    [Benchmark]
    public void SetFuncBitwise()
    {
        _list.SetFunc(0, 0, value => (int)(value & ~0x80000000));
    }

}

