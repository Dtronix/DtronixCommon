using System.Collections.Generic;
using System.Linq;
using DtronixCommon.Buffers;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Buffers;

public class AllocatedMemoryPoolTests
{
    [Test]
    public void AllocatesProperBucket()
    {
        var pool = new AllocatedMemoryPool<byte>(128, 2, true);

        Assert.AreEqual(16, pool.Rent(1).Length);
        Assert.AreEqual(16, pool.Rent(16).Length);
        Assert.AreEqual(32, pool.Rent(17).Length);
        Assert.AreEqual(32, pool.Rent(32).Length);
        Assert.AreEqual(64, pool.Rent(33).Length);
        Assert.AreEqual(64, pool.Rent(64).Length);
        Assert.AreEqual(128, pool.Rent(65).Length);
        Assert.AreEqual(128, pool.Rent(128).Length);
    }

    [Test]
    public void AllocatesNextSizeUpWhenFull()
    {
        var pool = new AllocatedMemoryPool<byte>(128, 1, true);

        Assert.AreEqual(16, pool.Rent(1).Length);
        Assert.AreEqual(32, pool.Rent(1).Length);
        Assert.AreEqual(1, pool.Rent(1).Length);
    }


    [Test]
    public void ReturnsUsedMemory()
    {
        var pool = new AllocatedMemoryPool<byte>(16, 1, true);

        var memory = pool.Rent(1);
        Assert.AreEqual(16, memory.Length);
        memory.Span[0] = 211;

        pool.Return(memory);

        var memory2 = pool.Rent(1);
        Assert.AreEqual(16, memory2.Length);
        Assert.AreEqual(211, memory2.Span[0]);
    }

    [Test]
    public void AllocatedMemoryDoesNotReturnToPool()
    {
        var pool = new AllocatedMemoryPool<byte>(16, 1, true);

        var memory = pool.Rent(1);
        Assert.AreEqual(16, memory.Length);
        memory.Span[0] = 211;

        // Should be the exact amount we requested.
        var instancedMemory = pool.Rent(1);
        Assert.AreEqual(1, instancedMemory.Length);

        // Dispose the original
        pool.Return(memory);

        // Dispose the instanced memory.  This one should not return to the pool.
        pool.Return(instancedMemory);

        // We should be returning the same instance as the original "memory".
        var memory3 = pool.Rent(1);
        Assert.AreEqual(16, memory3.Length);
        Assert.AreEqual(211, memory3.Span[0]);
    }
    
    [Test]
    public void MemoryDoesNotDoubleDispose()
    {
        var pool = new AllocatedMemoryPool<byte>(16, 1, true);

        var memory = pool.Rent(1);
        memory.Span[0] = 211;
        pool.Return(memory);
        pool.Return(memory);

        var memory2 = pool.Rent(8);
        Assert.AreEqual(211, memory2.Span[0]);
        var memory3 = pool.Rent(8);
        Assert.AreNotEqual(211, memory3.Span[0]);
    }
}
