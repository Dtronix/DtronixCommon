namespace Dtronix.Threading.Dispatcher.Actions;

public class SimpleMessagePumpTask : MessagePumpAction
{
    private readonly Func<CancellationToken, Task> _func;

    public SimpleMessagePumpTask(
        Func<CancellationToken, Task> func,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _func = func;
    }

    protected override void OnExecute(CancellationToken cancellationToken)
    {
        _func.Invoke(cancellationToken).Wait(cancellationToken);
    }
}