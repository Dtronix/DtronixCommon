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


        Assert.That(linkedList.Count, Is.EqualTo(5));
        Assert.That(linkedList.Last, Is.EqualTo(nodes.Last()));

        linkedList.BreakAtNode(nodes[2], true);

        Assert.That(linkedList.Count, Is.EqualTo(2));
        Assert.That(linkedList.Last, Is.EqualTo(nodes[1]));
    }

    [Test]
    public void BreakNodeBefore()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        Assert.That(linkedList.Count, Is.EqualTo(5));
        Assert.That(linkedList.Last, Is.EqualTo(nodes.Last()));

        linkedList.BreakAtNode(nodes[2], false);

        Assert.That(linkedList.Count, Is.EqualTo(2));
        Assert.That(linkedList.Last, Is.EqualTo(nodes[3]));
    }


    [Test]
    public void BreakNodeBeforeAtStart()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[0], false);

        Assert.That(linkedList.Count, Is.EqualTo(4));
        Assert.That(linkedList.First, Is.EqualTo(nodes[1]));
        Assert.That(linkedList.Last, Is.EqualTo(nodes[4]));
    }

    [Test]
    public void BreakNodeBeforeAtEnd()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[5];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[4], true);

        Assert.That(linkedList.Count, Is.EqualTo(4));
        Assert.That(linkedList.First, Is.EqualTo(nodes[0]));
        Assert.That(linkedList.Last, Is.EqualTo(nodes[3]));
    }

    [Test]
    public void BreakThreeItemNodeListAfter()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[3];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[1], true);

        Assert.That(linkedList.Count, Is.EqualTo(1));
        Assert.That(linkedList.First, Is.EqualTo(nodes[0]));
        Assert.That(linkedList.Last, Is.EqualTo(nodes[0]));
    }
    [Test]
    public void BreakThreeItemNodeListBefore()
    {
        var linkedList = new SimpleLinkedList<int>();
        var nodes = new SimpleLinkedListNode<int>[3];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = linkedList.AddLast(i);

        linkedList.BreakAtNode(nodes[1], false);

        Assert.That(linkedList.Count, Is.EqualTo(1));
        Assert.That(linkedList.First, Is.EqualTo(nodes[2]));
        Assert.That(linkedList.Last, Is.EqualTo(nodes[2]));
    }
}