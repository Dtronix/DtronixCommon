﻿using System.Threading;
using System.Threading.Tasks;
using DtronixCommon.Tests.Utilities;
using DtronixCommon.Threading.Dispatcher;
using NUnit.Framework;

namespace DtronixCommon.Tests.Threading.Dispatcher;

public class QueueResultTests
{

    private ThreadDispatcher? _dispatcher;

    [TearDown]
    public void TearDown()
    {
        _dispatcher?.Dispose();
    }

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

        Assert.That(await task.TestTimeout(), Is.True);
    }

    [Test]
    public async Task ReturnsLongRunningResult()
    {
        var task = _dispatcher.QueueResult(_ =>
        {
            Thread.Sleep(100);
            return true;
        });

        Assert.That(await task.TestTimeout(), Is.True);
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
        }, 0, cts.Token);

        Assert.That(await task.TestTimeout(), Is.True);
    }

  
}
