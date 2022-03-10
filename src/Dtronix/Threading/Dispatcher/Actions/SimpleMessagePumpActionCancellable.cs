namespace Dtronix.Threading.Dispatcher.Actions;

public class SimpleMessagePumpActionCancellable : MessagePumpAction
{
    private readonly Action<CancellationToken> _action;

    public SimpleMessagePumpActionCancellable(Action<CancellationToken> action, CancellationToken cancellationToken) 
        : base(cancellationToken)
    {
        _action = action;
    }

    protected override void OnExecute(CancellationToken cancellationToken)
    {
        _action.Invoke(cancellationToken);
    }
}