using System;
using System.Threading;
using System.Threading.Tasks;
using Dtronix.Tests.Utilities;
using Dtronix.Threading.Dispatcher;
using NUnit.Framework;

namespace Dtronix.Tests.Threading.Dispatcher;

public class QueueResultTests
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
        var task = _dispatcher.QueueResult(_ =>
        {
            Thread.Sleep(100);
            return true;
        });

        Assert.IsTrue(await task.TestTimeout());
    }

    [Test]
    public async Task ReturnsLongRunningResult()
    {
        var task = _dispatcher.QueueResult(_ =>
        {
            Thread.Sleep(100);
            return true;
        });

        Assert.IsTrue(await task.TestTimeout());
    }

    [Test]
    public async Task CancellationTokenPassedToWorker()
    {
        var cts = new CancellationTokenSource(100);
        var task = _dispatcher.QueueResult(ct =>
        {
            while (!ct.IsCancellationRequested)
            {
            }

            return true;
        }, DispatcherPriority.Normal, cts.Token);

        Assert.IsTrue(await task.TestTimeout());
    }

  
}