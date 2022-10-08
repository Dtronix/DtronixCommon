using System;
using BenchmarkDotNet.Attributes;
using DtronixCommon.Collections.Lists;
using DtronixCommon.Collections.Trees;

namespace DtronixCommonBenchmarks.Collections.Trees;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class QuadTreeBenchmarks
{
    private FloatQuadTree<Item> _quadTree;
    private FloatQuadTree<Item> _emptyQuadTree;

    private class Item : IQuadTreeItem
    {
        public int QuadTreeId { get; set; } = -1;
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var offsetX = 5;
        var offsetY = 5;
        _quadTree = new FloatQuadTree<Item>(10000, 10000, 8, 8, 1024);
        _emptyQuadTree = new FloatQuadTree<Item>(10000, 10000, 8, 8, 200);
        for (int x = 0; x < 50; x++)
        {
            for (int y = 0; y < 50; y++)
            {
                _quadTree.Insert(
                    x - offsetX + offsetX * x, 
                    y - offsetY + offsetY * y, 
                    x + offsetX + offsetX * x, 
                    y + offsetY + offsetY * y, new Item());
            }
        }
    }

    [Benchmark]
    public void Walk()
    {
        _quadTree.Walk(-5000, -5000, 5000, 5000, item => true);
    }

    [Benchmark]
    public void Insert()
    {
        
        var offsetX = 5;
        var offsetY = 5;
        
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                _emptyQuadTree.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }
        _emptyQuadTree.Clear();
    }

}

