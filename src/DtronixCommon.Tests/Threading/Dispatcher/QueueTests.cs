using System;
using System.Diagnostics;
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
        }, 0, cts.Token);

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

    [Test]
    public async Task MessagePump_IsInvokePending_FalseOnEmptyQueue()
    {
        Assert.IsFalse(_dispatcher.IsInvokePending);
        await _dispatcher.Queue(new SimpleMessagePumpAction(() =>
        {
            Thread.Sleep(100);
        })).TestTimeout();

        Assert.IsFalse(_dispatcher.IsInvokePending);
    }

    [Test]
    public async Task MessagePump_IsInvokePending_TrueOnItemInQueue()
    {
        Assert.IsFalse(_dispatcher.IsInvokePending);
        _ = _dispatcher.Queue(new SimpleMessagePumpAction(() =>
        {
            Thread.Sleep(1000);
        })).TestTimeout();

        // Delay added to allow the item to queue and start executing.
        await Task.Delay(100);

        Assert.IsFalse(_dispatcher.IsInvokePending);

        _ = _dispatcher.Queue(new SimpleMessagePumpAction(() =>
        {
        })).TestTimeout();

        Assert.IsTrue(_dispatcher.IsInvokePending);
    }

    [Test]
    public void AddingWhenFull_TimesOut()
    {
        var sw = Stopwatch.StartNew();
        _dispatcher = new ThreadDispatcher(new ThreadDispatcherConfiguration
        {
            BoundCapacity = 1,
            QueueTryAddTimeout = 200
        });

        _dispatcher.Start();
        var fire = () => _dispatcher.QueueFireForget(new SimpleMessagePumpAction(() =>
            {
                Thread.Sleep(10000);
            }));

        Assert.IsTrue(fire());
        Assert.IsTrue(fire());
        Assert.IsFalse(fire());
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 190);
    }

    [Test]
    public void AddingWhenFull_TimesOutImmediately()
    {
        var sw = Stopwatch.StartNew();
        _dispatcher = new ThreadDispatcher(new ThreadDispatcherConfiguration
        {
            BoundCapacity = 1,
            QueueTryAddTimeout = 0
        });

        _dispatcher.Start();
        var fire = () => _dispatcher.QueueFireForget(new SimpleMessagePumpAction(() =>
        {
            Thread.Sleep(10000);
        }));

        Assert.IsTrue(fire());
        fire();
        Assert.IsFalse(fire());
        Assert.LessOrEqual(sw.ElapsedMilliseconds, 100);
    }
}
