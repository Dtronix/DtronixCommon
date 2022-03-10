namespace Dtronix.Threading.Dispatcher.Actions;

public class SimpleMessagePumpTaskResult<TResult> : MessagePumpActionResult<TResult>
{
    private readonly Func<CancellationToken, Task<TResult>> _func;

    public SimpleMessagePumpTaskResult(
        Func<CancellationToken, Task<TResult>> func,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _func = func;
    }

    protected override TResult OnExecute(CancellationToken cancellationToken)
    {
        var runningTask = _func.Invoke(cancellationToken);
        runningTask.Wait(cancellationToken);
        return runningTask.Result;
    }
}