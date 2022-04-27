using System.Diagnostics;
using System.Threading.Tasks;
using DtronixCommon.Threading;
using NUnit.Framework;

namespace DtronixCommon.Tests.Threading;

[Parallelizable(scope: ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class DelayedActionTests
{
    private TaskCompletionSource _tcs;

    private struct ArgsValue
    {
        public ArgsValue(int i)
        {
            I = i;
        }
        public int I;
    }

    [SetUp]
    public void Setup()
    {
        _tcs = new TaskCompletionSource();
    }

    private async Task<bool> WaitForCompletion(int timeout)
    {
        var task = _tcs.Task;
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
        {
            return task.IsCompletedSuccessfully;
        }

        return false;
    }

    [Test]
    public async Task ActionInvokedAtCullingInterval()
    {
        var delayedAction = new DelayedAction(50, 200, () =>
        {
            _tcs.TrySetResult();
        });

        await delayedAction.InvokeAsync();
        Assert.IsTrue(await WaitForCompletion(100));
    }


    [Test]
    public async Task ActionInvokedAtCullingInterval_Multiple()
    {
        var delayedAction = new DelayedAction(50, 200, () =>
        {
            _tcs.TrySetResult();
        });

        await delayedAction.InvokeAsync();
        Assert.IsTrue(await WaitForCompletion(100));

        _tcs = new TaskCompletionSource();
        await delayedAction.InvokeAsync();
        Assert.IsTrue(await WaitForCompletion(100));

    }

    [Test]
    public async Task QueueInvokeCallsAreCulledUntilMaxDelay()
    {

        var delayedAction = new DelayedAction(20, 100, () =>
        {
            _tcs.TrySetResult();
        });
        var sw = Stopwatch.StartNew();

        _ = Task.Run(async () =>
        {
            while(true)
            {
                await delayedAction.InvokeAsync();
                await Task.Delay(1);
            }
        });


        Assert.IsTrue(await WaitForCompletion(1000));
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 80);
    }

    [Test]
    public async Task QueueInvokeCallsAreCulled()
    {
        var delayedAction = new DelayedAction<ArgsValue>(50, 200, value =>
        {
            if(value.I == 1)
                _tcs.TrySetResult();
        });
        _ = Task.Run(async () =>
        {
            await delayedAction.InvokeAsync(new ArgsValue(0));
            await Task.Delay(25);
            await delayedAction.InvokeAsync(new ArgsValue(1));
        });

        Assert.IsTrue(await WaitForCompletion(150));
    }
}