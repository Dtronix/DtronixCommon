namespace Dtronix.Threading.Dispatcher.Actions;

public class SimpleMessagePumpAction : MessagePumpAction
{
    private readonly Action _action;

    public SimpleMessagePumpAction(Action action) 
        : base(default)
    {
        _action = action;
    }

    protected override void OnExecute(CancellationToken cancellationToken)
    {
        _action.Invoke();
    }
}