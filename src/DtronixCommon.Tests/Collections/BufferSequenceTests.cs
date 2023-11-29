using System.Collections.Generic;
using System.Linq;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections;

public class BufferSequenceTests
{
    private void VerifySequence(BufferSequence.Range range, int expectedStart, int expectedEnd)
    {
        Assert.That(range.Start, Is.EqualTo(expectedStart), "Start");
        Assert.That(range.End, Is.EqualTo(expectedEnd), "End");
    }

    [Test]
    public void HeadIndexResets()
    {
        var bs = new BufferSequence(9);

        Assert.That(bs.Rent(), Is.GreaterThan(-1));
        Assert.That(bs.Rent(), Is.GreaterThan(-1));

        Assert.That(bs.HeadIndex, Is.EqualTo(0));
        bs.Return(0);
        Assert.That(bs.HeadIndex, Is.EqualTo(1));
    }

    [Test]
    public void HeadIsRentedAfterReturn()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            Assert.That(bs.Rent(), Is.GreaterThan(-1));

        bs.Return(0);
        Assert.That(bs.Rent(), Is.EqualTo(0));
    }

    [Test]
    public void TailIsTrimmed()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            Assert.That(bs.Rent(), Is.GreaterThan(-1));

        Assert.That(bs.ConsumedTailIndex, Is.EqualTo(9));
        bs.Return(9);
        Assert.That(bs.ConsumedTailIndex, Is.EqualTo(8));
    }

    [Test]
    public void TailIsReused()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();
        bs.Return(9);
        Assert.That(bs.Rent(), Is.EqualTo(9));
    }

    [Test]
    public void RentsInOrder()
    {
        var bs = new BufferSequence(9);
        var value = bs.Rent();
        var value2 = bs.Rent();
        Assert.That(value, Is.EqualTo(0));
        Assert.That(value2, Is.EqualTo(1));
        Assert.That(bs.Rent(), Is.EqualTo(2));

        bs.Return(value);
        Assert.That(bs.Rent(), Is.EqualTo(0));

        bs.Return(value2);
        Assert.That(bs.Rent(), Is.EqualTo(1));
    }

    [Test]
    public void RentReturnsFull()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 11; i++)
            bs.Rent();

        Assert.That(bs.Rent(), Is.EqualTo(-1));
    }

    [Test]
    public void ReturnFullSequence()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            bs.Rent();

        var result = bs.RentedSequences().ToArray();

        Assert.That(result, Has.Length.EqualTo(1));
        VerifySequence(result[0], 0, 10);
    }

    [Test]
    public void StopsRentingWhenFull()
    {
        var items = new List<int>();
        var bs = new BufferSequence(9);
        for (int i = 0; i < 10; i++)
            items.Add(bs.Rent());

        Assert.That(bs.Rent(), Is.EqualTo(-1));
    }

    [Test]
    public void ReturnFullSequenceWithGapAtStart()
    {
        var bs = new BufferSequence(9);
        for (int i = 0; i < 11; i++)
            bs.Rent();

        bs.Return(0);
        var result = bs.RentedSequences().ToArray();

        Assert.That(result, Has.Length.EqualTo(1));
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

        Assert.That(result, Has.Length.EqualTo(1));
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

            Assert.That(result, Has.Length.EqualTo(2));

            VerifySequence(result[0], 0, i - 1);

            VerifySequence(result[1], i + 1, 9);
            Assert.That(bs.Rent(), Is.EqualTo(i));
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

        Assert.That(result, Has.Length.EqualTo(5));

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
            Assert.That(bs.Rent(), Is.GreaterThan(-1));

        bs.Return(9);
        bs.Return(3);
        bs.Return(1);
        bs.Return(7);
        bs.Return(5);

        Assert.That(bs.Rent(), Is.EqualTo(1));
        Assert.That(bs.Rent(), Is.EqualTo(3));
        Assert.That(bs.Rent(), Is.EqualTo(5));
        Assert.That(bs.Rent(), Is.EqualTo(7));
        Assert.That(bs.Rent(), Is.EqualTo(9));
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

        Assert.That(result, Has.Length.EqualTo(2));
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

        Assert.That(bs.ConsumedTailIndex, Is.EqualTo(9));

        bs.Return(9);

        Assert.That(bs.ConsumedTailIndex, Is.EqualTo(6));
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

        Assert.That(bs.AvailableCount, Is.EqualTo(3));

        bs.Return(9);

        Assert.That(bs.AvailableCount, Is.EqualTo(4));
    }

    [Test]
    public void ReturningLastBufferResets()
    {
        var bs = new BufferSequence(9);
        bs.Rent();
        Assert.That(bs.HeadIndex, Is.EqualTo(0));
        Assert.That(bs.ConsumedTailIndex, Is.EqualTo(0));
        bs.Return(0);
        Assert.That(bs.Returned.count, Is.EqualTo(0));
        Assert.That(bs.ConsumedTailIndex, Is.EqualTo(-1));

    }
}
