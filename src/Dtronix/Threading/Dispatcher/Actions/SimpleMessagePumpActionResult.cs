namespace Dtronix.Threading.Dispatcher.Actions;

public class SimpleMessagePumpBlockingResult<TResult> : MessagePumpActionResult<TResult>
{
    private readonly Func<CancellationToken, TResult> _func;

    public SimpleMessagePumpBlockingResult(
        Func<CancellationToken, TResult> func,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _func = func;
    }

    protected override TResult OnExecute(CancellationToken cancellationToken)
    {
        return _func.Invoke(cancellationToken);
    }
}