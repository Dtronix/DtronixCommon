namespace DtronixCommon.Threading;

/// <summary>
/// Class to aid in the culling of events within a specified amount of time with a maximum delay.
/// </summary>
/// <typeparam name="TArgs"></typeparam>
internal class DelayedAction<TArgs> : DelayedAction
    where TArgs : struct
{
    private TArgs _arguments;

    /// <summary>
    /// Creates a DelayedActionArgument class.
    /// </summary>
    /// <param name="cullingInterval">The interval calls can be made in and override the last call passed arguments</param>
    /// <param name="maxCullingDelay">Maximum delay that calls will be culled.</param>
    /// <param name="action">Action to be invoked with the arguments passed.</param>
    public DelayedAction(int cullingInterval, int maxCullingDelay, Action<TArgs> action)
        : base(cullingInterval, maxCullingDelay, null)
    {
        Action = () => action(_arguments);
    }

    /// <summary>
    /// Called to invoke the passed constructor action with the specified parameters unless this invoke is culled.
    /// </summary>
    /// <param name="arguments">Arguments to pass to teh constructor action</param>
    /// <returns>Task for synchronization of invokes.</returns>
    public Task InvokeAsync(TArgs arguments)
    {
        _arguments = arguments;
        return InvokeAsync();
    }

    /// <summary>
    /// Called to invoke the passed constructor action with the specified parameters unless this invoke is culled.
    /// </summary>
    /// <param name="arguments">Arguments to pass to teh constructor action</param>
    /// <returns>Task for synchronization of invokes.</returns>
    public void Invoke(TArgs arguments)
    {
        _arguments = arguments;
        Invoke();
    }
}