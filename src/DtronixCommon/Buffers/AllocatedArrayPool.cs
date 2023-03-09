using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DtronixCommon.Collections;

namespace DtronixCommon.Buffers;

#if SET_INTERNAL
internal
#else
public
#endif
sealed partial class AllocatedArrayPool<T> : ArrayPool<T>
{
    /// <summary>The default maximum length of each array in the pool (2^20).</summary>
    private const int DefaultMaxArrayLength = 1024 * 1024;
    /// <summary>The default maximum number of arrays per bucket that are available for rent.</summary>
    private const int DefaultMaxNumberOfArraysPerBucket = 50;

    private readonly Bucket[] _buckets;

    internal AllocatedArrayPool(int maxArrayLength, int arraysPerBucket, bool pinned)
    {
        // Create the buckets.
        int maxBuckets = SelectBucketIndex(maxArrayLength);
        int arraySize = 0;
        for (int i = 0; i < maxBuckets; i++)
            arraySize += GetMaxSizeForBucket(i) * arraysPerBucket;

        var buffer = GC.AllocateUninitializedArray<T>(arraySize, pinned);

        var memoryBuffer = pinned
            ? MemoryMarshal.CreateFromPinnedArray(buffer, 0, buffer.Length)
            : new Memory<T>(buffer);

        var buckets = new Bucket[maxBuckets + 1];
        for (int i = 0; i < buckets.Length; i++)
        {
            var capacity = GetMaxSizeForBucket(i) * arraysPerBucket;
            arraySize -= capacity;
            buckets[i] = new Bucket(
                GetMaxSizeForBucket(i),
                arraysPerBucket,
                memoryBuffer.Slice(arraySize, capacity),
                pinned);
        }
        _buckets = buckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int SelectBucketIndex(int bufferSize)
    {
        // Buffers are bucketed so that a request between 2^(n-1) + 1 and 2^n is given a buffer of 2^n
        // Bucket index is log2(bufferSize - 1) with the exception that buffers between 1 and 16 bytes
        // are combined, and the index is slid down by 3 to compensate.
        // Zero is a valid bufferSize, and it is assigned the highest bucket index so that zero-length
        // buffers are not retained by the pool. The pool will return the Array.Empty singleton for these.
        return BitOperations.Log2((uint)bufferSize - 1 | 15) - 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetMaxSizeForBucket(int binIndex)
    {
        int maxSize = 16 << binIndex;
        Debug.Assert(maxSize >= 0);
        return maxSize;
    }

    public override T[] Rent(int minimumLength)
    {
        // Arrays can't be smaller than zero.  We allow requesting zero-length arrays (even though
        // pooling such an array isn't valuable) as it's a valid length array, and we want the pool
        // to be usable in general instead of using `new`, even for computed lengths.
        //ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        if (minimumLength == 0)
        {
            // No need for events with the empty array.  Our pool is effectively infinite
            // and we'll never allocate for rents and never store for returns.
            return Array.Empty<T>();
        }

        //ArrayPoolEventSource log = ArrayPoolEventSource.Log;
        T[]? buffer;

        int index = SelectBucketIndex(minimumLength);
        if (index < _buckets.Length)
        {
            // Search for an array starting at the 'index' bucket. If the bucket is empty, bump up to the
            // next higher bucket and try that one, but only try at most a few buckets.
            const int MaxBucketsToTry = 2;
            int i = index;
            do
            {
                // Attempt to rent from the bucket.  If we get a buffer from it, return it.
                buffer = _buckets[i].Rent();
                if (buffer != null)
                {
                    /*if (log.IsEnabled())
                    {
                        log.BufferRented(buffer.GetHashCode(), buffer.Length, Id, _buckets[i].Id);
                    }*/
                    return buffer;
                }
            }
            while (++i < _buckets.Length && i != index + MaxBucketsToTry);

            // The pool was exhausted for this buffer size.  Allocate a new buffer with a size corresponding
            // to the appropriate bucket.
            buffer = new T[_buckets[index]._bufferLength];
        }
        else
        {
            // The request was for a size too large for the pool.  Allocate an array of exactly the requested length.
            // When it's returned to the pool, we'll simply throw it away.
            buffer = new T[minimumLength];
        }

        /*if (log.IsEnabled())
        {
            int bufferId = buffer.GetHashCode();
            log.BufferRented(bufferId, buffer.Length, Id, ArrayPoolEventSource.NoBucketId);
            log.BufferAllocated(bufferId, buffer.Length, Id, ArrayPoolEventSource.NoBucketId, index >= _buckets.Length ?
                ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize :
                ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
        }*/

        return buffer;
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (array.Length == 0)
        {
            // Ignore empty arrays.  When a zero-length array is rented, we return a singleton
            // rather than actually taking a buffer out of the lowest bucket.
            return;
        }

        // Determine with what bucket this array length is associated
        int bucket = SelectBucketIndex(array.Length);

        // If we can tell that the buffer was allocated, drop it. Otherwise, check if we have space in the pool
        bool haveBucket = bucket < _buckets.Length;
        if (haveBucket)
        {
            // Clear the array if the user requests
            if (clearArray)
            {
                Array.Clear(array);
            }

            // Return the buffer to its bucket.  In the future, we might consider having Return return false
            // instead of dropping a bucket, in which case we could try to return to a lower-sized bucket,
            // just as how in Rent we allow renting from a higher-sized bucket.
            _buckets[bucket].Return(array);
        }

        // Log that the buffer was returned
        /*ArrayPoolEventSource log = ArrayPoolEventSource.Log;
        if (log.IsEnabled())
        {
            int bufferId = array.GetHashCode();
            log.BufferReturned(bufferId, array.Length, Id);
            if (!haveBucket)
            {
                log.BufferDropped(bufferId, array.Length, Id, ArrayPoolEventSource.NoBucketId, ArrayPoolEventSource.BufferDroppedReason.Full);
            }
        }*/
    }

    /// <summary>Provides a thread-safe bucket containing buffers that can be Rent'd and Return'd.</summary>
    private sealed class Bucket
    {
        private readonly int _bufferLength;
        private readonly bool _pinned;
        private readonly Memory<T>[] _buffers;
        private readonly ByteStack _freeStack;

        private SpinLock _lock; // do not make this readonly; it's a mutable struct

        /// <summary>
        /// Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
        /// </summary>
        internal Bucket(int bufferLength, int numberOfBuffers, Memory<T> memory, bool pinned)
        {
            if (numberOfBuffers > 256)
                throw new ArgumentOutOfRangeException(nameof(numberOfBuffers),
                    "Number of buffers must be equal to or less than 256");

            _lock = new SpinLock();
            _buffers = new Memory<T>[numberOfBuffers];
            var freeStackBytes = new byte[numberOfBuffers];
            _freeStack = new ByteStack(freeStackBytes, numberOfBuffers);

            var currentPos = 0;
            for (int i = 0; i < numberOfBuffers; i++)
            {
                freeStackBytes[i] = (byte)i;
                _buffers[i] = memory.Slice(currentPos, bufferLength);
                currentPos += bufferLength;
            }
            _bufferLength = bufferLength;
            _pinned = pinned;
        }

        /// <summary>Takes an memory from the bucket.  If the bucket is empty, allocates a new array.</summary>
        internal IMemoryOwner<T> Rent()
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _freeStack.TryPop(out )
                    if (_freeBuffers.TryDequeue(out var freeIndex))
                    return new OwnedMemory(this, _buffers[freeIndex], freeIndex);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            var byteBuffer = GC.AllocateUninitializedArray<T>(_bufferLength, _pinned);

            var memory = _pinned
                ? MemoryMarshal.CreateFromPinnedArray(byteBuffer, 0, byteBuffer.Length)
                : new Memory<T>(byteBuffer);
            return new OwnedMemory(this, memory, -1);
        }

        /// <summary>
        /// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
        /// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
        /// will be returned.
        /// </summary>
        private void Return(OwnedMemory memory)
        {
            //bool returned;

            // While holding the spin lock, if there's room available in the bucket,
            // put the buffer into the next available slot.  Otherwise, we just drop it.
            // The try/finally is necessary to properly handle thread aborts on platforms
            // which have them.
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _freeBuffers.Enqueue(memory.Index);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        private sealed class OwnedMemory : IMemoryOwner<T>
        {
            public Memory<T> Memory { get; private set; }

            private readonly Bucket _bucket;
            public readonly int Index;

            public OwnedMemory(Bucket bucket, Memory<T> memory, int index)
            {
                _bucket = bucket;
                Index = index;
                Memory = memory;
            }

            public void Dispose()
            {
                // If the memory index is -1, don't do anything as the memory was allocated specifically for this 
                // instance
                if (Index == -1)
                    return;

                Memory = null;

                _bucket.Return(this);
            }

        }

    }
}
