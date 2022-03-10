using System.Runtime.CompilerServices;

namespace Dtronix.Threading.Tasks;

public interface ITaskCompletionAwaiter : INotifyCompletion
{
    /// <summary>
    /// Gets whether this TaskCompletion has completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Returns the result value for the current awaiter.
    /// </summary>
    /// <returns>Value.</returns>
    void GetResult();

    /// <summary>
    /// Gets the awaiter for the current class.
    /// </summary>
    /// <returns></returns>
    ITaskCompletionAwaiter GetAwaiter();
}
