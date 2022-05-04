namespace Dtronix.Threading.Dispatcher;

/// <summary>
/// Configurations for the <see cref="ThreadDispatcher"/> class.
/// </summary>
public class ThreadDispatcherConfiguration
{
    /// <summary>
    /// Number of threads to run
    /// </summary>
    public int ThreadCount { get; set; } = 1;

    /// <summary>
    /// Number 
    /// </summary>
    public int BoundCapacity { get; set; } = -1;

    /// <summary>
    /// Number of queues to have running available.  Useful when you want to have prioritized queues.
    /// </summary>
    public int QueueCount { get; set; } = 1;
}