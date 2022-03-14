using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixCommon.Tests.Utilities;
using DtronixCommon.Threading.Dispatcher;
using DtronixCommon.Threading.Dispatcher.Actions;
using NUnit.Framework;

namespace DtronixCommon.Tests.Threading.Dispatcher;

public class QueueTests
{
    private ThreadDispatcher _dispatcher;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new ThreadDispatcher(1);
        _dispatcher.Start();
    }

    [Test]
    public void DispatcherHandlesException()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () =>
        {
            await _dispatcher.Queue(() => throw new InvalidCastException()).TestTimeout();
        });

        Assert.ThrowsAsync<AccessViolationException>(async () =>
        {
            await _dispatcher.Queue(() => throw new AccessViolationException()).TestTimeout();
        });
    }

    [Test]
    public async Task WorkBlocksTaskUntilComplete()
    {
        var started = false;
        var completed = false;
        var task = _dispatcher.Queue(() =>
        {
            started = true;
            Thread.Sleep(100);
            completed = true;
        });

        task.AssertTimesOut(50);
        
        Assert.IsTrue(started);
        Assert.IsFalse(completed);

        await task.TestTimeout();
        Assert.IsTrue(completed);
    }

    [Test]
    public void CancellationTokenPassedToWorker()
    {
        var cts = new CancellationTokenSource(50);
        var task = _dispatcher.Queue(ct =>
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
            }
        }, DispatcherPriority.Normal, cts.Token);

        Assert.ThrowsAsync<OperationCanceledException>(() => task.TestTimeout());
    }

    [Test]
    public async Task MessagePumpActionIsExecuted()
    {
        
        await _dispatcher.Queue(new SimpleMessagePumpActionCancellable(_ =>
        {
            Assert.Pass();
        }, default)).TestTimeout();

    }
}