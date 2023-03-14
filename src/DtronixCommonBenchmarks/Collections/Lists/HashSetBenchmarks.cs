using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DtronixCommon.Collections.Lists;

namespace DtronixCommonBenchmarks.Collections.Lists;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class HashSetBenchmarks
{
    private HashSet<int> _hash;
    private int[] _list;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _list = new int[20];
        _hash = new HashSet<int>(_list.Length);
        for (int i = 0; i < _list.Length; i++)
        {
            _list[i] = Random.Shared.Next(int.MinValue, int.MaxValue);
            _hash.Add(_list[i]);
        }
    }

    [Benchmark]
    public void HashRetrieval()
    {
        for (int i = 0; i < _list.Length; i++)
        {
            var contains = _hash.Contains(_list[i]);
        }
    }

    [Benchmark]
    public void ArrayRetrieval()
    {
        for (int i = 0; i < _list.Length; i++)
        {
            for (int j = 0; j < _list.Length; j++)
            {
                var contains = _list[i] == _list[j];
            }
        }
    }
}

