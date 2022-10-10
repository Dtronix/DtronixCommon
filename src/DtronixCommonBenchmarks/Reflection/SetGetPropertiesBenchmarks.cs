using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using DtronixCommon.Collections.Lists;
using DtronixCommon.Collections.Trees;
using DtronixCommon.Reflection;

namespace DtronixCommonBenchmarks.Reflection;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class SettingPropertiesBenchmarks
{
    private PropBacking _propBacking;
    private ItemList<PropBacking> _list;


    private class PropBackingExplicitImpl : PropBacking
    {

    }

    private abstract class PropBacking : IQuadTreeItem
    {
        internal int _quadTreeId;

        public int QuadTreeId
        {
            get => _quadTreeId;
            set => _quadTreeId = value;
        }
    }
    private class ItemList<T>
        where T : IQuadTreeItem
    {
        private readonly Func<T, int> _getter;
        private readonly Action<T, int> _setter;

        public ItemList()
        {
            var prop = typeof(T).GetProperty("QuadTreeId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            _getter = prop.GetBackingField().CreateGetter<T, int>();
            _setter = prop.GetBackingField().CreateSetter<T, int>();
        }

        public void PropAuto_Set(T prop)
        {
            prop.QuadTreeId = 21125;
        }
        public void PropAuto_Get(T prop)
        {
            var prop2 = prop.QuadTreeId;
        }

        public void Internal_Set(T prop)
        {
            
            _setter(prop, 21125);
        }
        public void Internal_Get(T prop)
        {
            var prop2 = _getter(prop);
        }
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _propBacking = new PropBackingExplicitImpl();
        _list = new ItemList<PropBacking>();
    }

    [Benchmark]
    public void PropAuto_Set()
    {
        _list.PropAuto_Set(_propBacking);
    }

    [Benchmark]
    public void PropAuto_Get()
    {
        _list.PropAuto_Get(_propBacking);
    }

    [Benchmark]
    public void Internal_Set()
    {
        _list.Internal_Set(_propBacking);
    }

    [Benchmark]
    public void Internal_Get()
    {
        _list.Internal_Get(_propBacking);
    }



}

