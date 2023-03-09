﻿using System;
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
sealed partial class FixedSizeAllocatedMemoryPool<T> : MemoryPool<T>
{
    private readonly int _arrayLength;
    private readonly bool _pinned;
    private SpinLock _lock = new SpinLock(); // do not make this readonly; it's a mutable struct
    private readonly Stack<short> _freeStack;
    private Memory<T> _memory;
    private bool _dispoed = false;
    public override int MaxBufferSize { get; }

    private readonly ConcurrentBag<OwnedMemory> _ownedMemoryCache = new ConcurrentBag<OwnedMemory>();

    internal FixedSizeAllocatedMemoryPool(int arrayLength, int count, bool pinned = false)
    {
        _arrayLength = arrayLength;
        _pinned = pinned;

        var freeStackContents = new short[count];
        for (int i = 0; i < freeStackContents.Length; i++)
            freeStackContents[i] = (short)i;

        _freeStack = new Stack<short>(freeStackContents);

        var buffer = GC.AllocateUninitializedArray<T>(count * arrayLength, pinned);

        _memory = pinned
            ? MemoryMarshal.CreateFromPinnedArray(buffer, 0, buffer.Length)
            : new Memory<T>(buffer);
    }

    /// <inheritdoc />
    public override IMemoryOwner<T> Rent(int minBufferSize = -1)
    {
        Memory<T> memory;
        bool lockTaken = false;
        short index = -1;
        try
        {
            _lock.Enter(ref lockTaken);

            if (!_freeStack.TryPop(out var freeIndex))
            {
                // The request was for a size too large for the pool or the pool was exhausted for this buffer size.
                // Allocate an array of exactly the requested length.
                // When it's returned to the pool, we'll simply throw it away.
                var byteBuffer = GC.AllocateUninitializedArray<T>(minBufferSize, _pinned);

                memory = _pinned
                    ? MemoryMarshal.CreateFromPinnedArray(byteBuffer, 0, byteBuffer.Length)
                    : new Memory<T>(byteBuffer);
            }
            else
            {
                memory = _memory.Slice(freeIndex * _arrayLength, _arrayLength);
                index = freeIndex;
            }
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }

        // See if we can use a cached ownedMemory.
        if (!_ownedMemoryCache.TryTake(out var ownedMemory))
            return new OwnedMemory(this, memory, index);

        ownedMemory.Set(memory, 0);
        
        return ownedMemory;
    }

    /// <summary>
    /// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
    /// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
    /// will be returned.
    /// </summary>
    internal void Return(OwnedMemory memory, short index)
    {
        if (_dispoed)
            return;

        // While holding the spin lock, if there's room available in the bucket,
        // put the buffer into the next available slot.  Otherwise, we just drop it.
        // The try/finally is necessary to properly handle thread aborts on platforms
        // which have them.
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            _freeStack.Push(index);
        }
        finally
        {
            if (lockTaken) 
                _lock.Exit(false);
        }

        _ownedMemoryCache.Add(memory);
    }


    protected override void Dispose(bool disposing)
    {
        if (_dispoed)
            return;

        _dispoed = true;
        _memory = null;
        _ownedMemoryCache.Clear();
    }

    internal sealed class OwnedMemory : IMemoryOwner<T>
    {
        private readonly FixedSizeAllocatedMemoryPool<T> _pool;
        private int _index;
        private Memory<T> _memory;

        public Memory<T> Memory => _memory;

        public int Index => _index;

        public OwnedMemory(FixedSizeAllocatedMemoryPool<T> pool, Memory<T> memory, int index)
        {
            _pool = pool;
            _index = index;
            _memory = memory;
        }

        public void Set(Memory<T> memory, byte index)
        {
            _index = index;
            _memory = memory;
        }

        public void Dispose()
        {
            _memory = null;
            var index = Interlocked.Exchange(ref _index, -1);

            // If the bucket is null, don't do anything as the memory was allocated specifically for this 
            // instance or has already been disposed.
            if (index == -1)
                return;

            _pool.Return(this, (short)index);
        }
    }
}
