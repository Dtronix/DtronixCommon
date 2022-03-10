namespace Dtronix.Threading.Dispatcher.Actions;

public abstract class MessagePumpActionBase
{
    internal readonly CancellationToken CancellationToken;
    internal abstract void SetFailed(Exception e);
    internal abstract void SetCanceled();

    protected MessagePumpActionBase(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }
    internal void ExecuteCore()
    {
        Execute(CancellationToken);
    }
    protected abstract void Execute(CancellationToken cancellationToken);
}