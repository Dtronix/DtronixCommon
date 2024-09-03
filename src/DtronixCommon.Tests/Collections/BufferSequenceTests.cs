using System.Collections.Generic;
using System.Linq;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections;

public class BufferSequenceTests
{
    private void VerifySequence(BufferSequence.Range range, int expectedStart, int expectedEnd)
    {
        Assert.AreEqual(expectedStart, range.Start, "Start");
        Assert.AreEqual(expectedEnd, range.End, "End");
    }

    [Test]
    public void HeadIndexResets()
    {
        var bs = new BufferSequence(9);

        Assert.Greater(bs.Rent(), -1);
        Assert.Greater(bs.Rent(), -1);

        Assert.AreEqual(0, bs.HeadIndex);
        bs.Return(0);
        Assert.AreEqual(1, bs.HeadIndex);
    }

    [Test]
    public void HeadIsRentedAfterReturn()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            Assert.Greater(bs.Rent(), -1);

        bs.Return(0);
        Assert.AreEqual(0, bs.Rent());
    }

    [Test]
    public void TailIsTrimmed()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            Assert.Greater(bs.Rent(), -1);

        Assert.AreEqual(9, bs.ConsumedTailIndex);
        bs.Return(9);
        Assert.AreEqual(8, bs.ConsumedTailIndex);
    }

    [Test]
    public void TailIsReused()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();
        bs.Return(9);
        Assert.AreEqual(9, bs.Rent());
    }

    [Test]
    public void RentsInOrder()
    {
        var bs = new BufferSequence(9);
        var value = bs.Rent();
        var value2 = bs.Rent();
        Assert.AreEqual(0, value);
        Assert.AreEqual(1, value2);
        Assert.AreEqual(2, bs.Rent());

        bs.Return(value);
        Assert.AreEqual(0, bs.Rent());

        bs.Return(value2);
        Assert.AreEqual(1, bs.Rent());
    }

    [Test]
    public void RentReturnsFull()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 11; i++)
            bs.Rent();

        Assert.AreEqual(-1, bs.Rent());
    }

    [Test]
    public void ReturnFullSequence()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        var result = bs.RentedSequences().ToArray();

        Assert.AreEqual(1, result.Length);
        VerifySequence(result[0], 0, 10);
    }

    [Test]
    public void StopsRentingWhenFull()
    {
        var items = new List<int>();
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            items.Add(bs.Rent());

        Assert.AreEqual(-1 , bs.Rent());
    }

    [Test]
    public void ReturnFullSequenceWithGapAtStart()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 11; i++)
            bs.Rent();

        bs.Return(0);
        var result = bs.RentedSequences().ToArray();

        Assert.AreEqual(1, result.Length);
        VerifySequence(result[0], 1, 10);
    }

    [Test]
    public void ReturnFullSequenceWithGapAtEnd()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        bs.Return(9);
        var result = bs.RentedSequences().ToArray();

        Assert.AreEqual(1, result.Length);
        VerifySequence(result[0], 0, 8);
    }

    [Test]
    public void ReturnGappedSequence()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        for (int i = 1; i < 9; i++)
        {
            bs.Return(i);
            var result = bs.RentedSequences().ToArray();

            Assert.AreEqual(2, result.Length);

            VerifySequence(result[0], 0, i - 1);

            VerifySequence(result[1], i + 1, 9);
            Assert.AreEqual(i, bs.Rent());
        }
    }

    [Test]
    public void ReturnMultiGappedSequence()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        bs.Return(9);
        bs.Return(7);
        bs.Return(5);
        bs.Return(3);
        bs.Return(1);

        var result = bs.RentedSequences().ToArray();

        Assert.AreEqual(5, result.Length);

        VerifySequence(result[0], 0, 0);
        VerifySequence(result[1], 2, 2);
        VerifySequence(result[2], 4, 4);
        VerifySequence(result[3], 6, 6);
        VerifySequence(result[4], 8, 8);
    }

    [Test]
    public void RentRetrievesInOrder()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            Assert.Greater(bs.Rent(), -1);

        bs.Return(9);
        bs.Return(3);
        bs.Return(1);
        bs.Return(7);
        bs.Return(5);

        Assert.AreEqual(1, bs.Rent());
        Assert.AreEqual(3, bs.Rent());
        Assert.AreEqual(5, bs.Rent());
        Assert.AreEqual(7, bs.Rent());
        Assert.AreEqual(9, bs.Rent());
    }


    [Test]
    public void ReleaseCullsContinuousGaps()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        bs.Return(4);
        bs.Return(5);
        bs.Return(6);

        var result = bs.RentedSequences().ToArray();

        Assert.AreEqual(2, result.Length);
        VerifySequence(result[0], 0, 3);
        VerifySequence(result[1], 7, 9);
    }

    [Test]
    public void ReleaseCullsContinuousGapsAtEnd()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        bs.Return(5);

        bs.Return(7);
        bs.Return(8);

        Assert.AreEqual(9, bs.ConsumedTailIndex);

        bs.Return(9);

        Assert.AreEqual(6, bs.ConsumedTailIndex);
    }

    [Test]
    public void ReleaseCullsContinuousGapsAtEndAndUpdatesAvailableCount()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        bs.Return(5);

        bs.Return(7);
        bs.Return(8);

        Assert.AreEqual(3, bs.AvailableCount);

        bs.Return(9);

        Assert.AreEqual(4, bs.AvailableCount);
    }

    [Test]
    public void ReturningLastBufferResets()
    {
        var bs = new BufferSequence(9);
        bs.Rent();
        Assert.AreEqual(0, bs.HeadIndex);
        Assert.AreEqual(0, bs.ConsumedTailIndex);
        bs.Return(0);
        Assert.AreEqual(0, bs.Returned.count);
        Assert.AreEqual(-1, bs.ConsumedTailIndex);

    }
}