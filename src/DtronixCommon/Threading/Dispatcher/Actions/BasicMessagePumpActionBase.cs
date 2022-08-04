namespace DtronixCommon.Threading.Dispatcher.Actions;

/// <summary>
/// Basic implementation of a <see cref="MessagePumpActionBase"/> for use as a base for other external actions.
/// </summary>
public abstract class BasicMessagePumpActionBase : MessagePumpActionBase
{ 

    protected BasicMessagePumpActionBase(CancellationToken cancellationToken)
        : base(cancellationToken)
    {
    }

    protected abstract void OnSetFailed(Exception e);

    protected abstract void OnSetCanceled();

    protected abstract void OnExecute(CancellationToken cancellationToken);

    internal override void SetFailed(Exception e)
    {
        OnSetFailed(e);

    }

    internal override void SetCanceled()
    {
        OnSetCanceled();
    }

    protected override void Execute(CancellationToken cancellationToken)
    {
        OnExecute(cancellationToken);
    }


}
