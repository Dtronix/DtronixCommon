using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixCommon.Tests.Utilities;
using DtronixCommon.Threading.Dispatcher;
using DtronixCommon.Threading.Dispatcher.Actions;
using NUnit.Framework;

namespace DtronixCommon.Tests.Threading.Dispatcher;

public class QueueFireForgetTests
{
    private ThreadDispatcher _dispatcher;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new ThreadDispatcher(1);
        _dispatcher.Start();
    }

    [Test]
    public async Task Executes()
    {
        var tcs = new TaskCompletionSource();
        _dispatcher.QueueFireForget(_ =>
        {
            tcs.TrySetResult();
        });

        await tcs.Task.TestTimeout();
    }

    [Test]
    public async Task CanReuse()
    {
        var tcs = new TaskCompletionSource();
        var count = 0;
        var mpa = new SimpleMessagePumpAction(() =>
        {
            if (++count == 10)
                tcs.TrySetResult();
        });

        for (int i = 0; i < 10; i++)
        {
            _dispatcher.QueueFireForget(mpa);
        }
        
        await tcs.Task.TestTimeout();
    }

}
