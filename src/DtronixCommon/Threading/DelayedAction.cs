using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DtronixCommon.Threading;

/// <summary>
/// Class to aid in the culling of events within a specified amount of time with a maximum delay.
/// </summary>
public class DelayedAction : IDisposable
{
    private readonly int _cullingInterval;
    private readonly int _maxCullingDelay;

    private long _lastInvokedTick = 0;
    private Action _action;
    private readonly Timer _timer;
    private long? _startTick;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// True if an invocation is queued for execution.
    /// </summary>
    public bool InvokeQueued { get; private set; }

    /// <summary>
    /// Creates a DelayedActionArgument class.
    /// </summary>
    /// <param name="cullingInterval">The interval calls can be made in and override the last call passed arguments</param>
    /// <param name="maxCullingDelay">Maximum delay that calls will be culled.</param>
    /// <param name="action">Action to be invoked.</param>
    public DelayedAction(int cullingInterval, int maxCullingDelay, Action action)
    {
        _cullingInterval = cullingInterval;
        _maxCullingDelay = maxCullingDelay;
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Method called when the timer time elapses.
    /// </summary>
    protected virtual void TimerCallback(object? _)
    {
        // If the ticks are equal, the invocation was only called once and is ready to be fired now.
        if (_lastInvokedTick == _startTick)
        {
            ExecuteCallback();
            return;
        }

        var currentTicks = Environment.TickCount64;
        var elapsedTime = currentTicks - _startTick!.Value;

        // See if we have exceeded the max culling delay.
        if (elapsedTime >= _maxCullingDelay)
        {
            ExecuteCallback();
            return;
        }

        // Check to see if we are exceeded the culling interval.
        var lastInvokeTickDelta = currentTicks - _lastInvokedTick;
        if (lastInvokeTickDelta >= _cullingInterval)
        {
            ExecuteCallback();
        }
    }

    private void ExecuteCallback()
    {
        // Stop the timer.
        _timer.Change(-1, -1);
        InvokeQueued = false;
        _startTick = null;
        OnCallback();
    }

    protected virtual void OnCallback()
    {
        _action.Invoke();
    }

    /// <summary>
    /// Called to invoke the passed constructor action with the specified parameters unless this invoke is culled.
    /// </summary>
    /// <returns>Task for synchronization of invokes.</returns>
    public async Task InvokeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            InvokeInternal();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Called to invoke the passed constructor action with the specified parameters unless this invoke is culled.
    /// </summary>
    /// <returns>Task for synchronization of invokes.</returns>
    public void Invoke()
    {
        _semaphore.Wait();
        try
        {
            InvokeInternal();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Called to invoke the passed constructor action with the specified parameters unless this invoke is culled.
    /// </summary>
    /// <returns>Task for synchronization of invokes.</returns>
    private void InvokeInternal()
    {
        InvokeQueued = true;
        _lastInvokedTick = Environment.TickCount64;
        // Check if we have exceeded the maximum delay time.
        if (_startTick == null)
        {
            _startTick = _lastInvokedTick;
            _timer.Change(_cullingInterval, _cullingInterval);
        }
        
       
    }


    public void Dispose()
    {
        _timer.Dispose();
        _semaphore.Dispose();
    }
}