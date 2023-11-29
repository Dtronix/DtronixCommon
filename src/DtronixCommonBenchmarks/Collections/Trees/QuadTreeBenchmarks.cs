﻿using System;
using BenchmarkDotNet.Attributes;
using DtronixCommon.Collections.Lists;
using DtronixCommon.Collections.Trees;

namespace DtronixCommonBenchmarks.Collections.Trees;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class QuadTreeBenchmarks
{

    private FloatQuadTree<Item> _quadTreeF;
    private DoubleQuadTree<Item> _quadTreeD;
    private DoubleQuadTree3<Item> _quadTreeD2;
    private FloatQuadTree<Item> _quadTreeFFull;
    private DoubleQuadTree<Item> _quadTreeDFull;
    private DoubleQuadTree3<Item> _quadTreeD2Full;

    private class Item : IQuadTreeItem
    {
        public int QuadTreeId { get; set; } = -1;
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var offsetX = 5;
        var offsetY = 5;
        _quadTreeD = new DoubleQuadTree<Item>(10000, 10000, 8, 8, 200);
        _quadTreeD2 = new DoubleQuadTree3<Item>(10000, 10000, 8, 8, 200);
        _quadTreeF = new FloatQuadTree<Item>(10000, 10000, 8, 8, 200);
        _quadTreeFFull = new FloatQuadTree<Item>(10000, 10000, 8, 8, 1024);
        for (int x = 0; x < 50; x++)
        {
            for (int y = 0; y < 50; y++)
            {
                _quadTreeFFull.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }

        _quadTreeDFull = new DoubleQuadTree<Item>(10000, 10000, 8, 8, 1024);
        for (int x = 0; x < 50; x++)
        {
            for (int y = 0; y < 50; y++)
            {
                _quadTreeDFull.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }

        _quadTreeD2Full = new DoubleQuadTree3<Item>(10000, 10000, 8, 8, 1024);
        for (int x = 0; x < 50; x++)
        {
            for (int y = 0; y < 50; y++)
            {
                _quadTreeD2Full.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }

    }

    //[Benchmark]
    public void WalkFloat()
    {
        _quadTreeFFull.Walk(-5000, -5000, 5000, 5000, item => true);
    }

    //[Benchmark]
    public void InsertFloat()
    {
        
        var offsetX = 5;
        var offsetY = 5;
        
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                _quadTreeF.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }
        _quadTreeF.Clear();
    }  
    
    [Benchmark]
    public void InsertDouble()
    {
        
        var offsetX = 5;
        var offsetY = 5;
        
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                _quadTreeD.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }
        _quadTreeD.Clear();
    }

    [Benchmark]
    public void InsertDouble3()
    {

        var offsetX = 5;
        var offsetY = 5;

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                _quadTreeD2.Insert(
                    x - offsetX + offsetX * x,
                    y - offsetY + offsetY * y,
                    x + offsetX + offsetX * x,
                    y + offsetY + offsetY * y, new Item());
            }
        }
        _quadTreeD.Clear();
    }


    [Benchmark]
    public void WalkDouble()
    {
        _quadTreeDFull.Walk(-5000, -5000, 5000, 5000, item => true);
    }

    [Benchmark]
    public void WalkDouble3()
    {
        _quadTreeD2Full.Walk(-5000, -5000, 5000, 5000, item => true);
    }

}

