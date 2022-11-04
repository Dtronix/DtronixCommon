using System.Collections.Concurrent;
using DtronixCommon.Threading.Dispatcher.Actions;

namespace DtronixCommon.Threading.Dispatcher;
/// <summary>
/// Class which handles a specified number of threads with a specified number of queues.
/// </summary>
public class ThreadDispatcher : IDisposable
{
    /// <summary>
    /// Action to stop the message pump loop from running.
    /// </summary>
    private class StopLoop : MessagePumpActionVoid
    {
    }

    private bool _isDisposed;

    /// <summary>
    /// Fired when an exception occurs on an executed queue item.
    /// </summary>
    public EventHandler<ThreadDispatcherExceptionEventArgs>? Exception;

    private BlockingCollection<MessagePumpActionBase>[]? _queues;

    internal Thread[]? Threads;

    private CancellationTokenSource? _cancellationTokenSource;

    private readonly ThreadDispatcherConfiguration _configs;
    private Action<Action>? _dispatcherExecutionWrapper;

    /// <summary>
    /// Optional wrapper which passes the message pump action's ExecuteCore for execution to.
    /// </summary>
    /// <remarks>
    /// Note: Passed action must be called. Otherwise the queueing method will not be returned.
    /// </remarks>
    public Action<Action>? DispatcherExecutionWrapper
    {
        get => _dispatcherExecutionWrapper;
        set => _dispatcherExecutionWrapper = value;
    }

    /// <summary>
    /// True if the thread dispatcher has started.
    /// </summary>
    public bool IsRunning => Threads != null;

    /// <summary>
    /// Returns true if there are any items in the queue to execute.  False if the queue is empty.
    /// </summary>
    public bool IsInvokePending
    {
        get
        {
            if (_queues == null)
                return false;
            
            // Lot path for single queue thread dispatchers.
            if (_queues.Length == 2)
                return _queues[1].Count > 0;

            // Ignore the internal queue.
            for (int i = 1; i < _queues.Length; i++)
            {
                if (_queues[i].Count > 0)
                    return true;
            }

            return false;
        }
    }
    /// <summary>
    /// Creates a new dispatcher with the specified thread count and default configurations 
    /// </summary>
    /// <param name="threadCount">Number of threads to spawn.</param>
    public ThreadDispatcher(int threadCount)
        : this(new ThreadDispatcherConfiguration
        {
            ThreadCount = threadCount
        })

    {

    }

    /// <summary>
    /// Creates a new dispatcher with the specified configurations.
    /// </summary>
    /// <param name="configs">Configurations for the dispatcher.</param>
    /// <exception cref="ArgumentNullException">Configs parameter can't be null.</exception>
    public ThreadDispatcher(ThreadDispatcherConfiguration configs)
    {
        _configs = configs ?? throw new ArgumentNullException(nameof(configs));
    }

    private void Pump()
    {
        try
        {
            while (_queues != null)
            {
                var queueId = BlockingCollection<MessagePumpActionBase>.TakeFromAny(
                    _queues!,
                    out var action,
                    _cancellationTokenSource!.Token);

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
                    // Hot path for early canceled actions.
                    if (action.CancellationToken.IsCancellationRequested)
                    {
                        action.SetCanceled();
                        continue;
                    }

                    if (_dispatcherExecutionWrapper != null)
                    {
                        _dispatcherExecutionWrapper(action.ExecuteCore);
                    }
                    else
                    {
                        action.ExecuteCore();
                    }
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

        // Setup the internal queue to be twice the size of the thread pool.
        _queues[0] = new BlockingCollection<MessagePumpActionBase>(_configs.ThreadCount * 2);
        for (int i = 1; i < _queues.Length; i++)
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

    /// <summary>
    /// Queues the action and executes when the next execution slot is available.
    /// </summary>
    /// <param name="action">Cancellable Action to execute</param>
    /// <param name="priority">Priority to execute action with.</param>
    public void QueueFireForget(
        Action<CancellationToken> action,
        int priority = 0)
    {
        QueueFireForget(new MessagePumpActionFireForget(action));
    }

    /// <summary>
    /// Queues the MessagePumpAction and executes when the next execution slot is available.
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <param name="priority">Priority to execute action with.</param>
    public bool QueueFireForget(
        MessagePumpActionBase action,
        int priority = 0)
    {
        if (_queues == null)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        return _queues[priority + 1].TryAdd(action, _configs.QueueTryAddTimeout);
    }

    /// <summary>
    /// Queues an action for execution and returns a task which will complete upon the action's execution completion.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <returns>Task which will complete upon the action's execution completion.</returns>
    public Task Queue(
        Action action,
        int priority = 0)
    {
        return Queue(new SimpleMessagePumpAction(action), priority);
    }

    /// <summary>
    /// Queues a cancellable action for execution and returns a task which will complete upon the action's execution completion.
    /// </summary>
    /// <param name="action">Cancellable Action to execute.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the action with.</param>
    /// <returns>Task which will complete upon the action's execution completion.</returns>
    public Task Queue(
        Action<CancellationToken> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return Queue(
            new SimpleMessagePumpActionCancellable(action, cancellationToken), priority);
    }

    /// <summary>
    /// Queues a MessagePumpAction for execution and returns a task which will complete upon the action's execution completion.
    /// </summary>
    /// <param name="action">MessagePumpAction to execute.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <returns>Task which will complete upon the action's execution completion.</returns>
    /// <exception cref="InvalidOperationException">Throws if the ThreadDispatcher is not running.</exception>
    public Task Queue(
        MessagePumpAction action,
        int priority = 0)
    {
        if (_queues == null)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        return _queues[priority + 1].TryAdd(action, _configs.QueueTryAddTimeout, action.CancellationToken)
            ? action.Result
            : Task.CompletedTask;
    }

    /// <summary>
    /// Queues an cancellable async function for execution and returns a task which will complete
    /// upon the action's execution completion.
    /// </summary>
    /// <param name="action">Cancellable async function to execute.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the action with.</param>
    /// <returns>Task which will complete upon the action's execution completion.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task QueueAsync(
        Func<CancellationToken, Task> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        if (_queues == null)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        var messageTask = new SimpleMessagePumpTask(action, cancellationToken);

        return _queues[priority + 1].TryAdd(messageTask, _configs.QueueTryAddTimeout, cancellationToken)
            ? messageTask.Result
            : Task.CompletedTask;
    }

    /// <summary>
    /// Queues an cancellable async function with a return value for execution and
    /// returns a task which will complete upon the action's execution completion.
    /// </summary>
    /// <typeparam name="TResult">Return type of the task.</typeparam>
    /// <param name="action">Cancellable async function with a return value.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the action with.</param>
    /// <returns>Returns value of the async function.</returns>
    public Task<TResult> QueueResultAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueResult(
            new SimpleMessagePumpTaskResult<TResult>(action, cancellationToken), priority);
    }

    /// <summary>
    /// Queues an cancellable function with a return value for execution and
    /// returns the task's result upon the action's execution completion.
    /// </summary>
    /// <typeparam name="TResult">Return type of the function.</typeparam>
    /// <param name="action">Cancellable function with a return value.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the action with.</param>
    /// <returns>Returns value of the function.</returns>
    public Task<TResult> QueueResult<TResult>(
        Func<CancellationToken, TResult> action,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueResult(
            new SimpleMessagePumpBlockingResult<TResult>(action, cancellationToken), priority);
    }

    /// <summary>
    /// Queues an cancellable MessagePumpActionResult with a return value for execution and
    /// returns the result upon the action's execution completion.
    /// </summary>
    /// <typeparam name="TResult">Return type of the function.</typeparam>
    /// <param name="action">Cancellable MessagePumpActionResult with a return value.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <returns>Returns value of the function.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task<TResult> QueueResult<TResult>(
        MessagePumpActionResult<TResult> action,
        int priority = 0)
    {
        if (_queues == null)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        return _queues[priority + 1].TryAdd(action, _configs.QueueTryAddTimeout, action.CancellationToken)
            ? action.Result
            : Task.FromResult(default(TResult))!;
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
