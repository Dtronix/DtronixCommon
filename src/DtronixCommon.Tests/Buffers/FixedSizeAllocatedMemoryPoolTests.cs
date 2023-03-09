using System.Collections.Generic;
using System.Linq;
using DtronixCommon.Buffers;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Buffers;

public class FixedSizeAllocatedMemoryPoolTests
{
    [Test]
    public void AllocatesProperBucket()
    {
        var pool = new FixedSizeAllocatedMemoryPool<byte>(128, 2, true);

        Assert.AreEqual(128, pool.Rent(1).Memory.Length);
        Assert.AreEqual(128, pool.Rent(16).Memory.Length);
    }
    
    [Test]
    public void ReturnsUsedMemory()
    {
        var pool = new FixedSizeAllocatedMemoryPool<byte>(16, 1, true);

        var memory = pool.Rent(1);
        Assert.AreEqual(16, memory.Memory.Length);
        memory.Memory.Span[0] = 211;
        memory.Dispose();

        var memory2 = pool.Rent(1);
        Assert.AreEqual(16, memory2.Memory.Length);
        Assert.AreEqual(211, memory2.Memory.Span[0]);
    }

    [Test]
    public void AllocatedMemoryDoesNotReturnToPool()
    {
        var pool = new FixedSizeAllocatedMemoryPool<byte>(16, 1, true);

        var memory = pool.Rent(1);
        Assert.AreEqual(16, memory.Memory.Length);
        memory.Memory.Span[0] = 211;

        // Should be the exact amount we requested.
        var instancedMemory = pool.Rent(1);
        Assert.AreEqual(1, instancedMemory.Memory.Length);

        // Dispose the original
        memory.Dispose();

        // Dispose the instanced memory.  This one should not return to the pool.
        instancedMemory.Dispose();

        // We should be returning the same instance as the original "memory".
        var memory3 = pool.Rent(1);
        Assert.AreEqual(16, memory3.Memory.Length);
        Assert.AreEqual(211, memory3.Memory.Span[0]);
    }

    [Test]
    public void MemoryDoesNotDoubleDispose()
    {
        var pool = new FixedSizeAllocatedMemoryPool<byte>(16, 1, true);

        var memory = pool.Rent(1);
        memory.Memory.Span[0] = 211;
        memory.Dispose();
        memory.Dispose();

        var memory2 = pool.Rent(8);
        Assert.AreEqual(211, memory2.Memory.Span[0]);
        var memory3 = pool.Rent(8);
        Assert.AreNotEqual(211, memory3.Memory.Span[0]);
    }
}
