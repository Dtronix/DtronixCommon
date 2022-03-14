using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixCommon.Tests.Utilities;
using DtronixCommon.Threading.Dispatcher;
using NUnit.Framework;

namespace DtronixCommon.Tests.Threading.Dispatcher;
public class QueueAsyncTests
{
    private ThreadDispatcher _dispatcher;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new ThreadDispatcher(1);
        _dispatcher.Start();
    }

    [Test]
    public async Task Returns()
    {
        await _dispatcher.QueueAsync(_ => Task.CompletedTask).TestTimeout();
    }

    [Test]
    public async Task ReturnsLongRunningResult()
    {
        var task = _dispatcher.QueueResultAsync(async _ =>
        {
            await Task.Delay(100);
            return true;
        });

        await task.TestTimeout();
    }


    [Test]
    public void CancellationTokenPassedToWorker()
    {
        var cts = new CancellationTokenSource(100);
        var task = _dispatcher.QueueAsync(async ct =>
        {
            await Task.Delay(500, ct);
        }, DispatcherPriority.Normal, cts.Token);

        Assert.ThrowsAsync<OperationCanceledException>(() => task.TestTimeout());

    }

}