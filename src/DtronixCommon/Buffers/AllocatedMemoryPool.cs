using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using DtronixCommon.Collections;

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
sealed partial class AllocatedMemoryPool<T> : MemoryPool<T>
{
    private readonly bool _pinned;

    /// <summary>The default maximum length of each array in the pool (2^20).</summary>
    private const int DefaultMaxArrayLength = 1024 * 1024;
    /// <summary>The default maximum number of arrays per bucket that are available for rent.</summary>
    private const int DefaultMaxNumberOfArraysPerBucket = 50;
    public override int MaxBufferSize { get; }

    private readonly ConcurrentBag<OwnedMemory> _ownedMemoryCache = new ConcurrentBag<OwnedMemory>();

    private readonly Bucket[] _buckets;

    internal AllocatedMemoryPool(int maxArrayLength, int arraysPerBucket, bool pinned = false)
    {
        if (arraysPerBucket > 256)
            throw new ArgumentOutOfRangeException(nameof(arraysPerBucket), "Must be maximum of 256 arrays.");
        _pinned = pinned;

        // Create the buckets.
        int maxBuckets = SelectBucketIndex(maxArrayLength);
        MaxBufferSize = maxArrayLength;
        var buckets = new Bucket[maxBuckets + 1];

        int arraySize = 0;
        for (int i = 0; i < buckets.Length; i++)
            arraySize += GetMaxSizeForBucket(i) * arraysPerBucket;

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
                _ownedMemoryCache,
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

    /// <inheritdoc />
    public override unsafe IMemoryOwner<T> Rent(int minBufferSize = -1)
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
                    return memory!;
            }
            while (++i < _buckets.Length && i != index + maxBucketsToTry);
        }

        // The request was for a size too large for the pool or the pool was exhausted for this buffer size.
        // Allocate an array of exactly the requested length.
        // When it's returned to the pool, we'll simply throw it away.
        var byteBuffer = GC.AllocateUninitializedArray<T>(minBufferSize, _pinned);

        var array = _pinned
            ? MemoryMarshal.CreateFromPinnedArray(byteBuffer, 0, byteBuffer.Length)
            : new Memory<T>(byteBuffer);

        // See if we can use a cached ownedMemory.
        if (!_ownedMemoryCache.TryTake(out var ownedMemory))
            return new OwnedMemory(null, array, 0);

        ownedMemory.Set(null, array, 0);
        return ownedMemory;
    }


    protected override void Dispose(bool disposing)
    {
        _ownedMemoryCache.Clear();
    }
    
    /// <summary>Provides a thread-safe bucket containing buffers that can be Rent'd and Return'd.</summary>
    internal sealed class Bucket
    {
        private readonly int _bufferLength;
        private readonly ConcurrentBag<OwnedMemory> _ownedMemoryCache;
        private readonly bool _pinned;
        private readonly Memory<T>[] _buffers;
        private readonly ByteStack _freeStack;

        private SpinLock _lock; // do not make this readonly; it's a mutable struct

        /// <summary>
        /// Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
        /// </summary>
        internal Bucket(
            int bufferLength,
            int numberOfBuffers,
            Memory<T> memory,
            ConcurrentBag<OwnedMemory> ownedMemoryCache,
            bool pinned)
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
            _ownedMemoryCache = ownedMemoryCache;
            _pinned = pinned;
        }

        /// <summary>Takes an memory from the bucket.  If the bucket is empty, allocates a new array.</summary>
        internal bool TryRent(out IMemoryOwner<T>? memory)
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                if (_freeStack.TryPop(out var freeIndex))
                {
                    if (_ownedMemoryCache?.TryTake(out var ownedMemory) == true)
                    {
                        ownedMemory.Set(this, _buffers[freeIndex], freeIndex);
                        memory = ownedMemory;
                    }
                    else
                    {
                        memory = new OwnedMemory(this, _buffers[freeIndex], freeIndex);
                    }

                    return true;
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            memory = null;
            return false;
        }

        /// <summary>
        /// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
        /// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
        /// will be returned.
        /// </summary>
        internal void Return(OwnedMemory memory)
        {
            // While holding the spin lock, if there's room available in the bucket,
            // put the buffer into the next available slot.  Otherwise, we just drop it.
            // The try/finally is necessary to properly handle thread aborts on platforms
            // which have them.
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _freeStack.Push((byte)memory.Index);
                _ownedMemoryCache.Add(memory);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }
    }
    internal sealed class OwnedMemory : IMemoryOwner<T>
    {
        private Bucket? _bucket;
        private byte _index;
        private Memory<T> _memory;

        public Memory<T> Memory => _memory;

        public byte Index => _index;

        public OwnedMemory(Bucket? bucket, Memory<T> memory, byte index)
        {
            _bucket = bucket;
            _index = index;
            _memory = memory;
        }

        public void Set(Bucket? bucket, Memory<T> memory, byte index)
        {
            _bucket = bucket;
            _index = index;
            _memory = memory;
        }

        public void Dispose()
        {
            _memory = null;
            var bucket = Interlocked.Exchange(ref _bucket, null);

            // If the bucket is null, don't do anything as the memory was allocated specifically for this 
            // instance or has already been disposed.
            if (bucket == null)
                return;

            bucket.Return(this);
        }
    }
}
