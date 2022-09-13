using DtronixCommon.Threading.Dispatcher;

namespace DtronixCommon.Threading;
/// <summary>
/// Class which handles a single thread action dispatcher..
/// </summary>
public class SingleActionThreadExecutor : IDisposable
{
    private bool _isDisposed;

    /// <summary>
    /// Fired when an exception occurs on an executed queue item.
    /// </summary>
    public EventHandler<ThreadDispatcherExceptionEventArgs>? Exception;
    
    internal Thread? Thread;

    private readonly Action _action;

    /// <summary>
    /// True if the thread dispatcher has started.
    /// </summary>
    public bool IsRunning => Thread != null;

    /// <summary>
    /// Source used to cancel the waiting message pump.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Internal reset event for execution of the action.
    /// </summary>
    private readonly ManualResetEventSlim _resetEvent;

    private static readonly TimeSpan TimeoutSpan = TimeSpan.FromMilliseconds(5_000);

    /// <summary>
    /// Returns true if there are any items in the queue to execute.  False if the queue is empty.
    /// </summary>
    public bool IsInvokePending => _resetEvent.IsSet;

    /// <summary>
    /// Creates a new single action dispatcher.
    /// </summary>
    public SingleActionThreadExecutor(Action action)
    {
        _action = action;
        _resetEvent = new ManualResetEventSlim(false);
    }

    private void Pump()
    {
        try
        {
            while (_cancellationTokenSource!.IsCancellationRequested == false)
            {
                if (!_resetEvent.Wait(TimeoutSpan, _cancellationTokenSource.Token))
                {
                    _resetEvent.Reset();
                    continue;
                }
                _resetEvent.Reset();

                try
                {
                    _action?.Invoke();
                }
                catch (Exception e)
                {
                    Exception?.Invoke(this, new ThreadDispatcherExceptionEventArgs(e));
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception e)
        {
            Exception?.Invoke(this, new ThreadDispatcherExceptionEventArgs(e));
        }
    }

    /// <summary>
    /// Stops the thread dispatcher and joins the thread.
    /// </summary>
    /// <param name="timeout">Timeout for waiting on each thread.</param>
    /// <returns>True on successful stopping of the dispatcher threads, otherwise false.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Stop(int timeout = 1000)
    {
        if (Thread == null)
            throw new InvalidOperationException("Message pump is not running.");
        
        _cancellationTokenSource?.Cancel();

        // Join all the thread back to ensure they are complete.
        var stopSuccessful = Thread.Join(timeout);

        Thread = null;

        return stopSuccessful;
    }

    /// <summary>
    /// Starts the dispatcher and waits for the startup of each thread.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        if (Thread != null)
            throw new InvalidOperationException("Message pump already running.");
        
        _cancellationTokenSource = new CancellationTokenSource();

        Thread = new Thread(Pump)
        {
            IsBackground = true
        };

        Thread.Start();
    }

    /// <summary>
    /// Queues the action and executes when the next execution slot is available.
    /// </summary>
    public void Call()
    {
        if (Thread == null)
            throw new InvalidOperationException("Message pump is not running.");

        _resetEvent.Set();
    }

    /// <summary>
    /// Releases resources held by <see cref="ThreadDispatcher"/>.
    /// </summary>
    public virtual void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Stop();
        _cancellationTokenSource?.Dispose();
    }
}
