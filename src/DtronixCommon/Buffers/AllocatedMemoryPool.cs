using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace DtronixCommon.Buffers;

/// <summary>
/// A memory pool which is pre-allocated and optionally will use pinned memory. Useful for network operations.
/// </summary>
/// <typeparam name="T">Type for the pool.</typeparam>
#if DTRONIX_COMMON_SET_CLASSES_INTERNAL
internal
#else
public
#endif
sealed partial class AllocatedMemoryPool<T>
{
    private readonly bool _pinned;

    /// <summary>The default maximum length of each array in the pool (2^20).</summary>
    private const int DefaultMaxArrayLength = 1024 * 1024;
    /// <summary>The default maximum number of arrays per bucket that are available for rent.</summary>
    private const int DefaultMaxNumberOfArraysPerBucket = 50;
    public int MaxBufferSize { get; }

    //private readonly ConcurrentBag<OwnedMemory> _ownedMemoryCache = new ConcurrentBag<OwnedMemory>();

    private readonly Bucket[] _buckets;

    internal AllocatedMemoryPool(int maxArrayLength, int arraysPerBucket, bool pinned = false)
    {
        _pinned = pinned;

        // Create the buckets.
        int maxBuckets = SelectBucketIndex(maxArrayLength);
        MaxBufferSize = maxArrayLength;
        var buckets = new Bucket[maxBuckets + 1];

        int arraySize = 0;
        for (int i = 0; i < buckets.Length; i++)
        {
            arraySize += GetMaxSizeForBucket(i) * arraysPerBucket;
        }

        var buffer = GC.AllocateUninitializedArray<T>(arraySize, pinned);

        var memoryBuffer = pinned
            ? MemoryMarshal.CreateFromPinnedArray(buffer, 0, buffer.Length)
            : new Memory<T>(buffer);

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

    public unsafe Memory<T> Rent(int minBufferSize = -1)
    {
#pragma warning disable CS8500
        if (minBufferSize == -1)
            minBufferSize = 1 + (4095 / sizeof(T));
#pragma warning restore CS8500

        if (minBufferSize < 0 || minBufferSize > MaxBufferSize)
            throw new ArgumentOutOfRangeException(nameof(minBufferSize));

        int index = SelectBucketIndex(minBufferSize);
        if (index < _buckets.Length)
        {
            // Search for an array starting at the 'index' bucket. If the bucket is empty, bump up to the
            // next higher bucket and try that one, but only try at most a few buckets.
            const int maxBucketsToTry = 2;
            int i = index;
            do
            {
                // Attempt to rent from the bucket.  If we get a buffer from it, return it.
                if (_buckets[i].TryRent(out var memory))
                    return memory!.Value;
            }
            while (++i < _buckets.Length && i != index + maxBucketsToTry);
        }

        // The request was for a size too large for the pool or the pool was exhausted for this buffer size.
        // Allocate an array of exactly the requested length.
        // When it's returned to the pool, we'll simply throw it away.
        var byteBuffer = GC.AllocateUninitializedArray<T>(minBufferSize, _pinned);
        
        return _pinned
            ? MemoryMarshal.CreateFromPinnedArray(byteBuffer, 0, byteBuffer.Length)
            : new Memory<T>(byteBuffer);
    }

    public void Return(Memory<T>? memory)
    {
        if (memory == null)
            return;

        if (memory.Value.Length == 0)
        {
            // Ignore empty arrays.  When a zero-length array is rented, we return a singleton
            // rather than actually taking a buffer out of the lowest bucket.
            return;
        }

        int bucketIndex = SelectBucketIndex(memory.Value.Length);

        // If we can tell that the buffer was allocated, drop it. Otherwise, check if we have space in the pool
        bool haveBucket = bucketIndex < _buckets.Length;
        if (haveBucket)
        {
            // Return the buffer to its bucket.  In the future, we might consider having Return return false
            // instead of dropping a bucket, in which case we could try to return to a lower-sized bucket,
            // just as how in Rent we allow renting from a higher-sized bucket.
            _buckets[bucketIndex].Return(memory.Value);
        }
    }

    /// <summary>Provides a thread-safe bucket containing buffers that can be Rent'd and Return'd.</summary>
    internal sealed class Bucket
    {
        private readonly int _bufferLength;
        private readonly bool _pinned;
        private readonly Memory<T>?[] _buffers;
        private HashSet<int> _rentedBufferIds = new HashSet<int>();
        private int _index;

        private SpinLock _lock; // do not make this readonly; it's a mutable struct

        /// <summary>
        /// Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
        /// </summary>
        internal Bucket(
            int bufferLength,
            int numberOfBuffers,
            Memory<T> memory,
            bool pinned)
        {
            //_hashBuffers.TryGetValue()
            if (numberOfBuffers > 256)
                throw new ArgumentOutOfRangeException(nameof(numberOfBuffers),
                    "Number of buffers must be equal to or less than 256");

            _lock = new SpinLock();
            _buffers = new Memory<T>?[numberOfBuffers];
            _index = numberOfBuffers - 1;

            var currentPos = 0;
            for (int i = 0; i < numberOfBuffers; i++)
            {
                _buffers[i] = memory.Slice(currentPos, bufferLength);
                currentPos += bufferLength;
            }
            _bufferLength = bufferLength;
            _pinned = pinned;
        }

        /// <summary>Takes an memory from the bucket.  If the bucket is empty, allocates a new array.</summary>
        internal bool TryRent(out Memory<T>? memory)
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                if (_index >= 0)
                {
                    memory = _buffers[_index];
                    _buffers[_index--] = null;

                    if (memory == null)
                        return false;

                    return _rentedBufferIds.Add(memory.GetHashCode());
                }

                // We don't have any more available to rent.
                memory = null;
                return false;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        /// <summary>
        /// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
        /// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
        /// will be returned.
        /// </summary>
        internal void Return(Memory<T> memory)
        {
            // While holding the spin lock, if there's room available in the bucket,
            // put the buffer into the next available slot.  Otherwise, we just drop it.
            // The try/finally is necessary to properly handle thread aborts on platforms
            // which have them.
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                // If we can not remove the ID from the list, that means that this memory was not
                // part of this bucket.
                if(!_rentedBufferIds.Remove(memory.GetHashCode()))
                    return;

                if (_index < _buffers.Length)
                {
                    _buffers[++_index] = memory;
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }
    }
}
