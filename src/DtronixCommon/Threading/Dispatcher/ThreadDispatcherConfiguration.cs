namespace DtronixCommon.Threading.Dispatcher;

/// <summary>
/// Configurations for the <see cref="ThreadDispatcher"/> class.
/// </summary>
public class ThreadDispatcherConfiguration
{
    private int _queueCount = 1;

    /// <summary>
    /// Number of threads to run.
    /// </summary>
    public int ThreadCount { get; set; } = 1;

    /// <summary>
    /// Number of items each queue can hold.  Set to -1 to have the limit unbound.
    /// </summary>
    public int BoundCapacity { get; set; } = -1;

    /// <summary>
    /// Number of queues to have running available.  Useful when you want to have prioritized queues.
    /// Number must be larger than 0.
    /// </summary>
    public int QueueCount
    {
        get => _queueCount;
        set
        {
            if(value < 1)
                throw new ArgumentOutOfRangeException(nameof(QueueCount), "Must be 1 or more.");

            _queueCount = value;
        }
    }

    /// <summary>
    /// Sets the time the items are attempting to wait to be added to the queue in milliseconds.
    /// Set to -1 to infinitely wait.
    /// </summary>
    public int QueueTryAddTimeout { get; set; } = 1500;
}