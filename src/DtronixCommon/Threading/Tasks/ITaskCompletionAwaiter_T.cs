using System.Runtime.CompilerServices;

namespace DtronixCommon.Threading.Tasks;

public interface ITaskCompletionAwaiter<T> : INotifyCompletion
{
    /// <summary>
    /// Gets whether this TaskCompletion has completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Returns the result value for the current awaiter.
    /// </summary>
    /// <returns>Value.</returns>
    T GetResult();

    /// <summary>
    /// Gets the awaiter for the current class.
    /// </summary>
    /// <returns></returns>
    ITaskCompletionAwaiter<T> GetAwaiter();
}
