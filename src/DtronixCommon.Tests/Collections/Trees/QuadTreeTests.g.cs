// ----------------------------
// This file is auto generated.
// Any modifications to this file will be overridden
// ----------------------------
using DtronixCommon.Collections.Trees;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections.Trees;

public class FloatQuadTreeTests : QuadTreeTestBase
{
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

        Assert.AreEqual(0, items.Count);
    }

    [Test]
    public void MultipleQueriesReturnSameValue()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                qt.Insert(-x, -y, x, y, new TestQuadTreeItem());
            }
        }

        var items = qt.Query(-1000, -1000, 1000, 1000);
        var items2 = qt.Query(-1000, -1000, 1000, 1000);

        Assert.AreEqual(100, items.Count);
        Assert.AreEqual(100, items2.Count);


    }
}

public class LongQuadTreeTests : QuadTreeTestBase
{
    private LongQuadTree<TestQuadTreeItem> DefaultQuadTree()
    {
        return new LongQuadTree<TestQuadTreeItem>(1000, 1000, 8, 8);
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

        Assert.AreEqual(0, items.Count);
    }

    [Test]
    public void MultipleQueriesReturnSameValue()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                qt.Insert(-x, -y, x, y, new TestQuadTreeItem());
            }
        }

        var items = qt.Query(-1000, -1000, 1000, 1000);
        var items2 = qt.Query(-1000, -1000, 1000, 1000);

        Assert.AreEqual(100, items.Count);
        Assert.AreEqual(100, items2.Count);


    }
}

public class IntQuadTreeTests : QuadTreeTestBase
{
    private IntQuadTree<TestQuadTreeItem> DefaultQuadTree()
    {
        return new IntQuadTree<TestQuadTreeItem>(1000, 1000, 8, 8);
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

        Assert.AreEqual(0, items.Count);
    }

    [Test]
    public void MultipleQueriesReturnSameValue()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                qt.Insert(-x, -y, x, y, new TestQuadTreeItem());
            }
        }

        var items = qt.Query(-1000, -1000, 1000, 1000);
        var items2 = qt.Query(-1000, -1000, 1000, 1000);

        Assert.AreEqual(100, items.Count);
        Assert.AreEqual(100, items2.Count);


    }
}

public class DoubleQuadTreeTests : QuadTreeTestBase
{
    private DoubleQuadTree<TestQuadTreeItem> DefaultQuadTree()
    {
        return new DoubleQuadTree<TestQuadTreeItem>(1000, 1000, 8, 8);
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

        Assert.AreEqual(0, items.Count);
    }

    [Test]
    public void MultipleQueriesReturnSameValue()
    {
        var qt = DefaultQuadTree();
        var item = new TestQuadTreeItem();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                qt.Insert(-x, -y, x, y, new TestQuadTreeItem());
            }
        }

        var items = qt.Query(-1000, -1000, 1000, 1000);
        var items2 = qt.Query(-1000, -1000, 1000, 1000);

        Assert.AreEqual(100, items.Count);
        Assert.AreEqual(100, items2.Count);


    }
}

