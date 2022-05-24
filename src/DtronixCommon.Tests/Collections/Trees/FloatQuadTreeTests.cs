using System.Collections.Generic;
using System.Linq;
using DtronixCommon.Collections;
using DtronixCommon.Collections.Trees;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections.Trees;

public class FloatQuadTreeTests
{
    private class TestQuadTreeItem : IQuadTreeItem
    {
        public int QuadTreeId { get; set; } = -1;
    }

    private FloatQuadTree<TestQuadTreeItem> DefaultQuadTree()
    {
        return new FloatQuadTree<TestQuadTreeItem>(1000, 1000, 8, 8);
    }

    [Test]
    public void ItemsReceiveIds()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        var item2 = new TestQuadTreeItem();
        qt.Insert(0, 0, 0, 0, item);
        qt.Insert(0, 0, 0, 0, item2);

        Assert.AreEqual(0, item.QuadTreeId);
        Assert.AreEqual(1, item2.QuadTreeId);
    }

    [Test]
    public void ItemInsert()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        qt.Insert(0, 0, 0, 0, item);

        Assert.AreEqual(0, item.QuadTreeId);
    }

    [Test]
    public void ItemInsertBeyondBounds()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        qt.Insert(50, 50, 60, 60, item);

        Assert.AreEqual(0, item.QuadTreeId);
    }

    [Test]
    public void ItemQueriedBeyondBounds()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        qt.Insert(50, 50, 60, 60, item);

        var items = qt.Query(0, 0, 5, 5);


    }



}