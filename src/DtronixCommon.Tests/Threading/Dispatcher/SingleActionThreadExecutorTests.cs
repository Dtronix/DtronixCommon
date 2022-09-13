using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixCommon.Tests.Utilities;
using DtronixCommon.Threading;
using DtronixCommon.Threading.Dispatcher;
using DtronixCommon.Threading.Dispatcher.Actions;
using NUnit.Framework;

namespace DtronixCommon.Tests.Threading.Dispatcher;

public class SingleActionThreadExecutorTests
{
    [Test]
    public async Task ExecutesAction()
    {
        var tcs = new TaskCompletionSource();

        void TestAction()
        {
            tcs.TrySetResult();
        }
        var dispatcher = new SingleActionThreadExecutor(TestAction);
        dispatcher.Start();

        dispatcher.Call();

        await tcs.Task.TestTimeout();
    }

    [Test]
    public async Task ExecutesActionMultipleTimes()
    {
        var tcs = new TaskCompletionSource();
        int timesCalled = 0;
        SingleActionThreadExecutor dispatcher = null;

        void TestAction()
        {
            dispatcher!.Call();
            if (++timesCalled == 2)
                tcs.TrySetResult();
        }

        dispatcher = new SingleActionThreadExecutor(TestAction);
        dispatcher.Start();

        dispatcher.Call();

        await tcs.Task.TestTimeout();
    }

    [Test]
    public async Task QueuesReExecutionOnInvocation()
    {
        var tcs = new TaskCompletionSource();
        var tcsRun = new TaskCompletionSource();
        int timesCalled = 0;

        void TestAction()
        {
            tcsRun.TrySetResult();

            if (++timesCalled == 2)
                tcs.TrySetResult();
        }

        var dispatcher = new SingleActionThreadExecutor(TestAction);
        dispatcher.Start();

        dispatcher.Call();
        await tcsRun.Task;
        dispatcher.Call();

        await tcs.Task.TestTimeout();
    }

    [Test]
    public void CallThrowsOnStoppedState()
    {
        var dispatcher = new SingleActionThreadExecutor(() =>{ });
        Assert.Throws<InvalidOperationException>(dispatcher.Call);
    }

    [Test]
    public void StartThrowsOnStartedState()
    {
        var dispatcher = new SingleActionThreadExecutor(() => { });
        dispatcher.Start();
        Assert.Throws<InvalidOperationException>(dispatcher.Start);
    }

    [Test]
    public void StopThrowsOnStoppedState()
    {
        var dispatcher = new SingleActionThreadExecutor(() => { });
        Assert.Throws<InvalidOperationException>(() => dispatcher.Stop(1000));
    }

    [Test]
    public async Task ExecutesAction_Restarts()
    {
        var tcs = new TaskCompletionSource();

        void TestAction()
        {
            tcs.TrySetResult();
        }
        var dispatcher = new SingleActionThreadExecutor(TestAction);
        dispatcher.Start();
        dispatcher.Stop();
        dispatcher.Start();

        dispatcher.Call();

        await tcs.Task.TestTimeout();
    }
}
