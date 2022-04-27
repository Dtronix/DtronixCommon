using System.Collections.Concurrent;
using DtronixCommon.Threading.Dispatcher.Actions;

namespace DtronixCommon.Threading.Dispatcher;

public class ThreadDispatcher : IDisposable
{
    private readonly int _threadCount;

    private class StopLoop : MessagePumpActionVoid
    {
    }

    private bool _isDisposed;
    public EventHandler<ThreadDispatcherExceptionEventArgs>? Exception;

    internal Thread[]? Threads;
    protected BlockingCollection<MessagePumpActionBase>? InternalPriorityQueue;
    protected BlockingCollection<MessagePumpActionBase>? HighPriorityQueue;
    protected BlockingCollection<MessagePumpActionBase>? NormalPriorityQueue;
    protected BlockingCollection<MessagePumpActionBase>? LowPriorityQueue;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsRunning => Threads != null;

    /// <summary>
    /// Returns true if there are any items in the queue to execute.  False if the queue is empty.
    /// </summary>
    public bool IsInvokePending => NormalPriorityQueue?.Count > 0 
                                 || HighPriorityQueue?.Count > 0 
                                 || LowPriorityQueue?.Count > 0;
    public ThreadDispatcher(int threadCount)
    {
        _threadCount = threadCount;
    }

    private void Pump()
    {
        var queueList = new[]
        {
            InternalPriorityQueue,
            HighPriorityQueue,
            NormalPriorityQueue,
            LowPriorityQueue
        };
        try
        {
            while (true)
            {
                var queueId = BlockingCollection<MessagePumpActionBase>.TakeFromAny(
                    queueList!,
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
        if (Threads == null)
            throw new InvalidOperationException("Message pump is not running.");

        // Send enough StopLoop commands to end all the threads.
        for (int i = 0; i < _threadCount; i++)
            InternalPriorityQueue?.TryAdd(new StopLoop());
            

        _cancellationTokenSource?.Cancel();
        InternalPriorityQueue?.CompleteAdding();
        HighPriorityQueue?.CompleteAdding();
        NormalPriorityQueue?.CompleteAdding();
        LowPriorityQueue?.CompleteAdding();

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

        InternalPriorityQueue = new BlockingCollection<MessagePumpActionBase>();
        HighPriorityQueue = new BlockingCollection<MessagePumpActionBase>();
        NormalPriorityQueue = new BlockingCollection<MessagePumpActionBase>();
        LowPriorityQueue = new BlockingCollection<MessagePumpActionBase>();

        _cancellationTokenSource = new CancellationTokenSource();

        Threads = new Thread[_threadCount];
        for (int i = 0; i < _threadCount; i++)
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
        DispatcherPriority priority = DispatcherPriority.Normal)
    {
        QueueFireForget(new MessagePumpActionFireForget(action));
    }

    /// <summary>
    /// Queues the MessagePumpAction and executes when the next execution slot is available.
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <param name="priority">Priority to execute action with.</param>
    public void QueueFireForget(
        MessagePumpActionBase action,
        DispatcherPriority priority = DispatcherPriority.Normal)
    {
        GetQueue(priority).Add(action);
    }

    /// <summary>
    /// Queues an action for execution and returns a task which will complete upon the action's execution completion.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <param name="priority">Priority to execute action with.</param>
    /// <returns>Task which will complete upon the action's execution completion.</returns>
    public Task Queue(
        Action action,
        DispatcherPriority priority = DispatcherPriority.Normal)
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
        DispatcherPriority priority = DispatcherPriority.Normal,
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
        DispatcherPriority priority = DispatcherPriority.Normal)
    {
        if (!IsRunning)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        GetQueue(priority).Add(action, action.CancellationToken);
        return action.Result;
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
        DispatcherPriority priority = DispatcherPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        var messageTask = new SimpleMessagePumpTask(action, cancellationToken);
        GetQueue(priority).Add(messageTask, cancellationToken);
        return messageTask.Result;
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
        DispatcherPriority priority = DispatcherPriority.Normal,
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
        DispatcherPriority priority = DispatcherPriority.Normal,
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
        DispatcherPriority priority = DispatcherPriority.Normal)
    {
        if (!IsRunning)
            throw new InvalidOperationException("ThreadDispatcher is not running");

        GetQueue(priority).Add(action, action.CancellationToken);
        return action.Result;
    }

    private BlockingCollection<MessagePumpActionBase> GetQueue(DispatcherPriority priority)
    {
        return priority switch
        {
            DispatcherPriority.Low => LowPriorityQueue,
            DispatcherPriority.Normal => NormalPriorityQueue,
            DispatcherPriority.High => HighPriorityQueue,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        } ?? throw new InvalidOperationException();
    }

    public virtual void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Stop();
        InternalPriorityQueue?.Dispose();
        HighPriorityQueue?.Dispose();
        NormalPriorityQueue?.Dispose();
        LowPriorityQueue?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}