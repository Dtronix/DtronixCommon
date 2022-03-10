using System;
using System.Threading;
using System.Threading.Tasks;
using Dtronix.Tests.Utilities;
using Dtronix.Threading.Dispatcher;
using NUnit.Framework;

namespace Dtronix.Tests.Threading.Dispatcher;

public class QueueResultAsyncTests
{
    private ThreadDispatcher _dispatcher;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new ThreadDispatcher(1);
        _dispatcher.Start();
    }


    [Test]
    public async Task ReturnsResult()
    {
        var task = _dispatcher.QueueResultAsync(_ => Task.FromResult(true));

        Assert.IsTrue(await task.TestTimeout());
    }

    [Test]
    public async Task ReturnsLongRunningResult()
    {
        var task = _dispatcher.QueueResultAsync(async _ =>
        {
            await Task.Delay(100);
            return true;
        });

        Assert.IsTrue(await task.TestTimeout());
    }

    [Test]
    public void CancellationTokenPassedToWorker()
    {
        var cts = new CancellationTokenSource(100);
        var task = _dispatcher.QueueResultAsync(async ct =>
        {
            await Task.Delay(500, ct);
            return true;

        }, DispatcherPriority.Normal, cts.Token);

        Assert.ThrowsAsync<OperationCanceledException>(() => task.TestTimeout());

    }

}