namespace Dtronix.Threading.Tasks;

/// <summary>
/// Manually resettable awaiter for reuse in high performance situations.
/// </summary>
/// <typeparam name="T">Completion return value.</typeparam>
public sealed class ManualResetAwaiterSource<T> : IManualResetAwaiterSource
{
    private readonly ManualResetAwaiter _awaiter = new();

    public ITaskCompletionAwaiter<T> Awaiter => _awaiter;
    /// <summary>
    /// Attempts to transition the completion state.
    /// </summary>
    /// <param name="result">Result to have the awaited task return.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TrySetResult(T result)
    {
        return _awaiter.TrySetResult(result);
    }

    /// <inheritdoc />
    public bool TrySetException(Exception exception)
    {
        return _awaiter.TrySetException(exception);
    }

    /// <inheritdoc />
    public bool TrySetCanceled()
    {
        return _awaiter.TrySetCanceled();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _awaiter.Reset();
    }

    /// <summary>
    /// Returns a task for the current awaiter state.
    /// </summary>
    /// <returns>Task for the state.</returns>
    public Task<T> ToTask()
    {
        return Task.Run(async () => await _awaiter);
    }

    private sealed class ManualResetAwaiter : ITaskCompletionAwaiter<T>
    {
        private Action? _continuation;
        private Exception? _exception;
        private T? _result;

        public void OnCompleted(Action continuation)
        {
            if (_continuation != null)
                throw new InvalidOperationException("This ReusableTaskCompletionSource instance has already been listened");
            _continuation = continuation;
        }

        public bool IsCompleted { get; private set; }

        public T GetResult()
        {
            if (_exception != null)
                throw _exception;
            return _result;
        }

        /// <summary>
        /// Attempts to transition the completion state.
        /// </summary>
        /// <param name="result">Result to have the awaited task return.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool TrySetResult(T result)
        {
            if (IsCompleted) 
                return false;

            IsCompleted = true;
            _result = result;

            _continuation?.Invoke();
            return true;
        }

        /// <summary>
        /// Attempts to transition the exception state.
        /// </summary>
        /// <returns></returns>
        public bool TrySetException(Exception exception)
        {
            if (IsCompleted) 
                return false;

            IsCompleted = true;
            _exception = exception;

            _continuation?.Invoke();
            return true;
        }

        /// <summary>
        /// Attempts to transition to the canceled state.
        /// </summary>
        /// <returns></returns>
        public bool TrySetCanceled()
        {
            if (IsCompleted)
                return false;

            IsCompleted = true;
            _exception = new OperationCanceledException();

            _continuation?.Invoke();
            return true;
        }

        /// <summary>
        /// Reset the awaiter to initial status
        /// </summary>
        /// <returns></returns>
        public void Reset()
        {
            _result = default;
            _continuation = null;
            _exception = null;
            IsCompleted = false;
        }
        public ITaskCompletionAwaiter<T> GetAwaiter()
        {
            return this;
        }
    }
}
