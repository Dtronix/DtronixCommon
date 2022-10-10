using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace DtronixCommonBenchmarks.Allocations;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class AllocationBenchmarks
{

    [Benchmark]
    public void ManagedMemory()
    {
        var dataLength = 8;

        var data = new float[dataLength];

        for (int i = 0; i < 20; i++)
        {
            dataLength *= 2;

            var dataTemp = new float[dataLength];
            Buffer.BlockCopy(data, 0, dataTemp, 0, data.Length);
            data = dataTemp;
        }

        data = null;
    }
    
    [Benchmark]
    public unsafe void NativeMemoryAllocSpanCopy()
    {
        nuint dataLength = 8;

        var mem = NativeMemory.Alloc(dataLength * sizeof(float));
        Span<float> data = new Span<float>(mem, (int)(dataLength * sizeof(float)));

        for (int i = 0; i < 20; i++)
        {
            dataLength *= 2;
            var newMem = NativeMemory.Alloc(dataLength * sizeof(float));

            Span<float> dataTemp = new Span<float>(newMem, (int)(dataLength * sizeof(float)));
            data.CopyTo(dataTemp);

            NativeMemory.Free(mem);

            mem = newMem;
            data = dataTemp;
        }

        NativeMemory.Free(mem);
    }

    [Benchmark]
    public unsafe void NativeMemoryRealloc()
    {
        nuint dataLength = 8;

        var mem = NativeMemory.Alloc(dataLength * sizeof(float));
        Span<float> data = new Span<float>(mem, (int)(dataLength * sizeof(float)));

        for (int i = 0; i <20; i++)
        {
            dataLength *= 2;

            mem = NativeMemory.Realloc(mem, (dataLength * sizeof(float)));
            Span<float> dataTemp = new Span<float>(mem, (int)(dataLength * sizeof(float)));

            data = dataTemp;
        }
        NativeMemory.Free(mem);
    }

}

