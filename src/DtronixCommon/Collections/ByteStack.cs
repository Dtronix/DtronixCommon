namespace DtronixCommon.Collections;

/// <summary>
/// Queue which has a maximum size of 256 values and a range of: 0 &#8804; value &#8804; 256.
/// Not thread safe.
/// </summary>
/// <remarks>
/// This class is intended to be used within a non-threaded or locked environment to ensure sequential
/// pushing and popping of the stack.
/// </remarks>
#if SET_INTERNAL
internal
#else
public 
#endif
class ByteStack
{
    /// <summary>
    /// Index of the last free index.
    /// </summary>
    private int _freeIndex;

    /// <summary>
    /// Array of the items.
    /// </summary>
    private readonly byte[] _queueBuffer;

    /// <summary>
    /// Number of items in the queue.
    /// </summary>
    public int Count => _freeIndex + 1;

    /// <summary>
    /// Initializes the queue with the specified max size.
    /// </summary>
    /// <param name="maxSize">Maximum number of entities for this queue. 0 &#8804; maxSize &#8804; 256.</param>
    public ByteStack(int maxSize = 256)
        : this(new byte[maxSize], 0)
    {
    }

    /// <summary>
    /// Initializes the queue with the specified max size.
    /// </summary>
    /// <param name="contents">Initial contents of the stack. Length must be 0 &#8804; maxSize &#8804; 256.</param>
    /// <param name="count">Number to set the initial count to.</param>
    public ByteStack(byte[] contents, int count)
    {
        if (contents.Length > 256 || contents.Length < 1)
            throw new IndexOutOfRangeException("Max size must be greater than or equal to 1 and less than or equal to 256");

        if (count > contents.Length || count < 0)
            throw new IndexOutOfRangeException("Count must be greater than zero and less than or equal to the contents length.");

        _freeIndex = count - 1;
        _queueBuffer = contents;
    }

    /// <summary>
    /// Tries to pop the last item off the end of the queue.
    /// </summary>
    /// <param name="value">Value popped off the end of the queue</param>
    /// <returns>True on successful pop. False if the queue is empty.</returns>
    public bool TryPop(out byte value)
    {
        if (_freeIndex == -1)
        {
            value = 0;
            return false;
        }

        value = _queueBuffer[_freeIndex--];
        return true;
    }

    /// <summary>
    /// Pops the last item off the end of the queue.
    /// </summary>
    /// <returns>Popped value.</returns>
    /// <exception cref="Exception">Throws when queue is empty.</exception>
    public byte Pop()
    {
        if (TryPop(out var value))
            return value;

        throw new Exception("Queue is empty.");
    }

    /// <summary>
    /// Pushes a value onto the queue.
    /// </summary>
    /// <param name="value">Value to push.</param>
    /// <exception cref="InvalidOperationException">If </exception>
    public void Push(byte value)
    {
        if (!TryPush(value))
            throw new InvalidOperationException(
                "Tried to return index when the buffer is full.");
    }

    /// <summary>
    /// Tries to push a value onto the queue.
    /// </summary>
    /// <param name="value">Value to push</param>
    /// <returns>True on successful push.  False if the queue is full.</returns>
    public bool TryPush(byte value)
    {
        if (_freeIndex >= _queueBuffer.Length - 1)
            return false;

        _queueBuffer[++_freeIndex] = value;
        return true;
    }
}
