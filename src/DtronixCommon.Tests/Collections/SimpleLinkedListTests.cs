using System.Linq;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections;

public class SimpleLinkedListTests
{
 
    [Test]
    public void BreakNodeAfter()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);


        Assert.AreEqual(5, linkedList.Count);
        Assert.AreEqual(nodes.Last(), linkedList.Last);

        linkedList.BreakAtNode(nodes[2], true);

        Assert.AreEqual(2, linkedList.Count);
        Assert.AreEqual(nodes[1], linkedList.Last);
    }

    [Test]
    public void BreakNodeBefore()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        Assert.AreEqual(5, linkedList.Count);
        Assert.AreEqual(nodes.Last(), linkedList.Last);

        linkedList.BreakAtNode(nodes[2], false);

        Assert.AreEqual(2, linkedList.Count);
        Assert.AreEqual(nodes[3], linkedList.Last);
    }


    [Test]
    public void BreakNodeBeforeAtStart()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[0], false);

        Assert.AreEqual(4, linkedList.Count);
        Assert.AreEqual(nodes[1], linkedList.First);
        Assert.AreEqual(nodes[4], linkedList.Last);
    }

    [Test]
    public void BreakNodeBeforeAtEnd()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[4], true);

        Assert.AreEqual(4, linkedList.Count);
        Assert.AreEqual(nodes[0], linkedList.First);
        Assert.AreEqual(nodes[3], linkedList.Last);
    }

    [Test]
    public void BreakThreeItemNodeListAfter()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[3];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[1], true);

        Assert.AreEqual(1, linkedList.Count);
        Assert.AreEqual(nodes[0], linkedList.First);
        Assert.AreEqual(nodes[0], linkedList.Last);
    }
    [Test]
    public void BreakThreeItemNodeListBefore()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[3];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[1], false);

        Assert.AreEqual(1, linkedList.Count);
        Assert.AreEqual(nodes[2], linkedList.First);
        Assert.AreEqual(nodes[2], linkedList.Last);
    }
}