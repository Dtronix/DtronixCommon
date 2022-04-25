#define BUFFER_SEQUENCE_SAFETY
namespace DtronixCommon.Collections;

/// <summary>
/// This class is designed for performance and makes certain concessions to reduce checks.
/// 1. This class is not thread safe.  This must be sequentially used or locked to prevent
///    the class state from being corrupted.
/// 2. You must not return the same index multiple times.  If you do, it will not check
///    to ensure the index not already present and will corrupt the state.
/// </summary>
public class BufferSequence
{
    /// <summary>
    /// Range of indexes which have been consumed in the buffer sequence.
    /// </summary>
    /// <param name="Start">First index consumed.</param>
    /// <param name="End">End of the range continuous indexes.</param>
    public readonly record struct Range(int Start, int End);

    private readonly int _maxIndex;
    internal readonly SimpleLinkedList<int> Returned = new();
    private int _consumedTailIndex = -1;
    private int _headIndex = 0;
    public int AvailableCount { get; private set; }
    public int Consumed => _maxIndex - AvailableCount;
    public int Max => _maxIndex;
    internal int ConsumedTailIndex => _consumedTailIndex;
    internal int HeadIndex => _headIndex;

    public bool ShouldDefragment => Returned.Count > 0 && (double)Consumed / Returned.Count > 0.5d;

    /// <summary>
    /// Max number allowed for an index starting at zero.
    /// </summary>
    /// <param name="maxIndex">Max number of indexes to use. Zero based index.
    /// Setting 0 allows for a single index.
    /// Setting 9 allows for 10 indexes. 0-9.
    /// </param>
    public BufferSequence(int maxIndex)
    {
        AvailableCount = maxIndex + 1;
        _maxIndex = maxIndex;
    }

    private void Reset()
    {
        _headIndex = 0;
        _consumedTailIndex = -1;
        Returned.Clear();
    }

    /// <summary>
    /// Returns an index to the sequence for re-use
    /// </summary>
    /// <remarks>
    /// Sequence is designed for performance so no checks are performed if a number is returned multiple times
    /// and in-fact returning a single index multiple times will throw the sequence off and make it invalid.
    /// Perform checks as required prior to returning an index.
    /// </remarks>
    /// <param name="index">Index to return.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Return(in int index)
    {
#if BUFFER_SEQUENCE_SAFETY
        // Performance
        if (index < _headIndex || index > _consumedTailIndex)
            throw new ArgumentException("Index outside of the range of returnable values.", nameof(index));

        var currentCheck = Returned.First;
        while (currentCheck != null)
        {
            if (currentCheck.ValueRef == index)
            {
                throw new ArgumentException($"Index {index} has already been returned.", nameof(index));
            }

            currentCheck = currentCheck.Next;
        }
#endif

        // Reset the sequence since we returned everything.
        if (AvailableCount == Max)
        {
            Reset();
            return;
        }

        // Return from the head first.
        if (index == _headIndex)
        {
            AvailableCount++;
            _headIndex++;
            return;
        }

        // If we return the end of the list, then decrement the tail and do nothing.
        if (index == _consumedTailIndex)
        {
            var endCurrent = Returned.Last;

            if (endCurrent == null)
            {
                Returned.AddLast(index);
                AvailableCount++;
                _consumedTailIndex--;
                return;
            }

            // If we are returning the tail index, see if we can cull older values
            // stored in the Returned values and move the _consumedTailIndex back.
            if (_consumedTailIndex == endCurrent.ValueRef + 1)
            {
                while (endCurrent != null && endCurrent.ValueRef == endCurrent.Previous?.ValueRef + 1)
                {
                    // Remove the ends until we get to the first gap.
                    endCurrent = endCurrent.Previous;
                }

                // If we are at the beginning, we have an empty list.  We should not ever reach here sinec 
                if (endCurrent == Returned.First)
                {
                    Reset();
                    return;
                }

                var removed = Returned.BreakAtNode(endCurrent, true);

                AvailableCount++;
                _consumedTailIndex -= removed + 1;
            }

            return;
        }

        AvailableCount++;

        var current = Returned.First;

        if (current == null)
        {
            Returned.AddFirst(index);
            return;
        }

        // Hot path for the beginning of the list.
        if (index < current.ValueRef)
        {
            Returned.AddFirst(index);
            return;
        }

        // Hot path for the end of the list.
        if (index > Returned.Last!.ValueRef)
        {
            Returned.AddLast(index);
            return;
        }

        // Improve this insertion method.
        while (current != null)
        {
            if (current.ValueRef > index)
            {
                Returned.AddBefore(current, index);
                return;
            }

            // Performance
            //if (current.ValueRef == index)
            //    throw new ArgumentException("Index is already present in the buffer.", nameof(index));

            current = current.Next;
        }

        throw new ArgumentOutOfRangeException(nameof(index), "Returned index does not fit inside sequence.");
    }

    /// <summary>
    /// Rents an index.  Returns 
    /// </summary>
    /// <returns>value >= 0 for the usable index. -1 if this sequence if full.</returns>
    public int Rent()
    {
        if (AvailableCount == 0)
            return -1;

        AvailableCount--;

        // If we have room at the head, return those values first.
        if (_headIndex > 0)
            return --_headIndex;

        // If we have a gap, return one.
        if (Returned.Count != 0)
        {
            ref var index = ref Returned.First!.ValueRef;
            Returned.Remove(Returned.First!);
            return index;
        }

        // If we are at the tail, then we are "full" and don't have any more room.
        if (_consumedTailIndex == _maxIndex)
            return -1;

        // Index can be >= to -1 at this point.
        return ++_consumedTailIndex;
    }

    /// <summary>
    /// Retrieves groups of the rented sequences.
    /// </summary>
    /// <returns>Groups of rented sequences.</returns>
    public IEnumerable<Range> RentedSequences()
    {
        var current = Returned.First;

        // We are full and should return the entire range in one call.
        if (current == null)
        {
            // If the tail is -1, then the index is empty.
            if (_consumedTailIndex == -1)
                yield break;

            yield return new Range(_headIndex, _consumedTailIndex + 1);
            yield break;
        }


        var currentIndex = _headIndex;
        var nextGapStart = current;
        while (nextGapStart != null)
        {
            yield return new Range(currentIndex, nextGapStart.ValueRef - 1);

            if (current.ValueRef + 1 == current.Next?.ValueRef)
            {
                do
                {
                    nextGapStart = nextGapStart.Next;
                } while (nextGapStart?.Next != null && nextGapStart.ValueRef + 1 == nextGapStart.Next?.ValueRef);

                currentIndex = nextGapStart.ValueRef + 1;

                // We are at the end and need to break out of the loop.
                if (nextGapStart.Next == null)
                    break;
            }
            else
            {
                currentIndex = nextGapStart.ValueRef + 1;
                nextGapStart = nextGapStart.Next;

                if (nextGapStart == null)
                    break;
            }
        }

        if (currentIndex > _consumedTailIndex)
            yield break;

        yield return new Range(currentIndex, _consumedTailIndex);
    }
}