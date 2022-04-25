namespace DtronixCommon.Threading;

/// <summary>
/// Class to aid in the culling of events within a specified amount of time with a maximum delay.
/// </summary>
internal class DelayedAction : IDisposable
{
    private readonly int _cullingInterval;
    private readonly int _maxCullingDelay;

    protected Action Action;
    private readonly Timer _timer;
    private DateTime? _startTime;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

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
        Action = action;
        _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    protected virtual void TimerCallback(object _)
    {
        InvokeQueued = false;
        _startTime = null;
        Action?.Invoke();
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
        // Check if we have exceeded the maximum delay time.
        if (_maxCullingDelay == 0
            || (DateTime.UtcNow - (_startTime ??= DateTime.UtcNow)).TotalMilliseconds >= _maxCullingDelay)
        {
            // Stop the timer.
            _timer.Change(0, Timeout.Infinite);
            return;
        }

        _timer.Change(_cullingInterval, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _semaphore?.Dispose();
    }
}