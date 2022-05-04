using System.Collections.Concurrent;
using Dtronix.Threading.Dispatcher.Actions;

namespace Dtronix.Threading.Dispatcher;

public class ThreadDispatcherConfigurations
{
    public int ThreadCount { get; set; } = 1;

    /// <summary>
    /// Number 
    /// </summary>
    public int BoundCapacity { get; set; } = -1;

    public int QueueCount { get; set; } = 1;
}

public class ThreadDispatcher : IDisposable
{
    private class StopLoop : MessagePumpActionVoid
    {
    }

    private bool _isDisposed;
    public EventHandler<ThreadDispatcherExceptionEventArgs>? Exception;

    private BlockingCollection<MessagePumpActionBase>[]? _queues;

    internal Thread[]? Threads;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ThreadDispatcherConfigurations _configs;

    public bool IsRunning => Threads != null;

    public ThreadDispatcher(int threadCount)
        : this(new ThreadDispatcherConfigurations
        {
            BoundCapacity = -1,
            ThreadCount = threadCount
        })

    {

    }

    public ThreadDispatcher(ThreadDispatcherConfigurations configs)
    {
        _configs = configs ?? throw new ArgumentNullException(nameof(configs));
    }

    private void Pump()
    {
        try
        {
            while (true)
            {
                var queueId = BlockingCollection<MessagePumpActionBase>.TakeFromAny(_queues, out var action);

                // Check if this is a command.
                if (queueId == 0)
                {
                    if (action is StopLoop)
                        return;

                    continue;
                }

                if (action == null)
                    continue;

                try
                {
                    if (action.CancellationToken.IsCancellationRequested)
                    {
                        action.SetCanceled();
                        continue;
                    }

                    action.ExecuteCore();
                }
                catch (TaskCanceledException)
                {
                    action.SetCanceled();
                }
                catch (Exception e)
                {
                    Exception?.Invoke(this, new ThreadDispatcherExceptionEventArgs(e));
                    action.SetFailed(e);
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
    /// Stops the thread dispatcher and joins all the threads.
    /// </summary>
    /// <param name="timeout">Timeout for waiting on each thread.</param>
    /// <returns>True on successful stopping of the dispatcher threads, otherwise false.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Stop(int timeout = 1000)
    {
        if (Threads == null || _queues == null)
            throw new InvalidOperationException("Message pump is not running.");

        // Send enough StopLoop commands to end all the threads.
        for (int i = 0; i < _configs.ThreadCount; i++)
            _queues[0].TryAdd(new StopLoop());


        _cancellationTokenSource?.Cancel();

        foreach (var queue in _queues)
        {
            queue.CompleteAdding();
            queue.Dispose();
        }

        _queues = null;

        var stopSuccessful = true;
        // Join all the threads back to ensure they are complete.
        foreach (var thread in Threads)
        {
            // If the thread join times out, return a failure result to the caller.
            if (!thread.Join(timeout))
                stopSuccessful = false;
        }

        Threads = null;

        return stopSuccessful;
    }

    /// <summary>
    /// Starts the dispatcher and waits for the startup of each thread.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        if (Threads != null)
            throw new InvalidOperationException("Message pump already running.");

        _queues = new BlockingCollection<MessagePumpActionBase>[_configs.QueueCount + 1];
        for (int i = 0; i < _queues.Length; i++)
        {
            // If the bound capacity is less than zero, it has no upper bound.
            _queues[i] = _configs.BoundCapacity < 0
                ? new BlockingCollection<MessagePumpActionBase>()
                : new BlockingCollection<MessagePumpActionBase>(_configs.BoundCapacity);
        }

        _cancellationTokenSource = new CancellationTokenSource();

        Threads = new Thread[_configs.ThreadCount];
        for (int i = 0; i < Threads.Length; i++)
        {
            Threads[i] = new Thread(Pump)
            {
                IsBackground = true
            };
            Threads[i].Start();
        }

    }

    public Task Queue(
        Action action,
        int priority = 0)
    {
        return Queue(new SimpleMessagePumpAction(action), priority);
    }

    public Task Queue(
        Action<CancellationToken> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return Queue(
            new SimpleMessagePumpActionCancellable(action, cancellationToken), priority);
    }

    public Task Queue(
        MessagePumpAction action,
        int priority = 0)
    {
        if (!IsRunning)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        _queues[priority + 1].Add(action, action.CancellationToken);
        return action.Result;
    }

    public Task QueueAsync(
        Func<CancellationToken, Task> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        var messageTask = new SimpleMessagePumpTask(action, cancellationToken);
        _queues[priority + 1].Add(messageTask, cancellationToken);
        return messageTask.Result;
    }

    public Task<TResult> QueueResultAsync<TResult>(
        Func<CancellationToken, Task<TResult>> task,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueResult(
            new SimpleMessagePumpTaskResult<TResult>(task, cancellationToken), priority);
    }

    public Task<TResult> QueueResult<TResult>(
        Func<CancellationToken, TResult> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueResult(
            new SimpleMessagePumpBlockingResult<TResult>(action, cancellationToken), priority);
    }

    public Task<TResult> QueueResult<TResult>(
        MessagePumpActionResult<TResult> action,
        int priority = 0)
    {
        if (!IsRunning)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        _queues[priority + 1].Add(action, action.CancellationToken);
        return action.Result;
    }

    public virtual void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
        _cancellationTokenSource?.Dispose();
    }
}