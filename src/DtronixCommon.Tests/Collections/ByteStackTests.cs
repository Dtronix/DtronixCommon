using System;
using System.Linq;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections;

public class ByteStackTests
{
 
    [Test]
    public void PushAndPopValue()
    {
        var queue = new ByteStack();
        queue.Push(50);
        Assert.AreEqual(50, queue.Pop());
    }


    [Test]
    public void PushAndPopValues()
    {
        var queue = new ByteStack();
        queue.Push(50);
        queue.Push(20);
        queue.Push(90);

        Assert.AreEqual(90, queue.Pop());
        Assert.AreEqual(20, queue.Pop());
        Assert.AreEqual(50, queue.Pop());
    }

    [Test]
    public void PushesAndPopsValues()
    {
        var queue = new ByteStack(5);
        queue.Push(50);
        Assert.AreEqual(50, queue.Pop());

        queue.Push(20);
        queue.Push(90);
        Assert.AreEqual(90, queue.Pop());
        Assert.AreEqual(20, queue.Pop());

        queue.Push(1);
        queue.Push(2);
        queue.Push(3);
        queue.Push(4);
        queue.Push(5);
        Assert.AreEqual(5, queue.Pop());
        Assert.AreEqual(4, queue.Pop());
        Assert.AreEqual(3, queue.Pop());
        Assert.AreEqual(2, queue.Pop());
        Assert.AreEqual(1, queue.Pop());
    }


    [Test]
    public void TryPushReturnsFalseWhenFull()
    {
        var queue = new ByteStack(1);
        queue.Push(50);
        Assert.IsFalse(queue.TryPush(20));
    }


    [Test]
    public void PushThrowsWhenFull()
    {
        var queue = new ByteStack(1);
        queue.Push(50);
        Assert.Throws<InvalidOperationException>(() => queue.Push(20));
    }

    [Test]
    public void PopReturnsFalseOnEmpty()
    {
        var queue = new ByteStack();
        Assert.IsFalse(queue.TryPop(out var free));
    }

    [Test]
    public void PopsOffInitial()
    {
        var queue = new ByteStack(new byte[] {1, 24, 73}, 3);
        Assert.AreEqual(73, queue.Pop());
        Assert.AreEqual(24, queue.Pop());
        Assert.AreEqual(1, queue.Pop());
    }
}
