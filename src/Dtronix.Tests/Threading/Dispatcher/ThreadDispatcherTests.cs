using System;
using System.Threading;
using System.Threading.Tasks;
using Dtronix.Tests.Utilities;
using Dtronix.Threading.Dispatcher;
using NUnit.Framework;

namespace Dtronix.Tests.Threading.Dispatcher;

public class ThreadDispatcherTests
{
    [Test]
    public async Task SingleThread_Blocks()
    {
        var dispatcher = new ThreadDispatcher(1);
        dispatcher.Start();
        bool complete = false;
        var tasks = new Task[2];
        tasks[0] = dispatcher.QueueAsync(async _ =>
        {
            await Task.Delay(100);
            complete = true;
        });

        tasks[1] = dispatcher.Queue(() => { Assert.IsTrue(complete); });
        await Task.WhenAll(tasks).TestTimeout();
    }

    [Test]
    public async Task SingleThread_SequentiallyExecutesActions()
    {
        var dispatcher = new ThreadDispatcher(1);
        dispatcher.Start();
        var counter = 0;
        var tasks = new Task[1000];
        for (int i = 0; i < tasks.Length; i++)
        {
            var i1 = i;
            tasks[i] = dispatcher.Queue(() =>
            {
                Assert.AreEqual(i1, counter++, "Executed tasks out of order.");
            });
        }

        await Task.WhenAll(tasks).TestTimeout();
    }

    [Test]
    public async Task MultipleThread_ConcurrentlyExecutesActions()
    {
        var dispatcher = new ThreadDispatcher(2);
        dispatcher.Start();

        var task1Started = false;
        var task1Completed = false;

        _ = dispatcher.QueueAsync(async _ =>
        {
            task1Started = true;
            await Task.Delay(2000);
            task1Completed = true;
        });
        await Task.Delay(100);
        var task2 = dispatcher.QueueAsync( _ =>
        {
            Assert.IsTrue(task1Started);
            Assert.IsFalse(task1Completed);
            return Task.CompletedTask;
        });

        await task2.TestTimeout();
    }

    [Test]
    public void Stop_KillsAllThreads()
    {
        var count = 5;
        var dispatcher = new ThreadDispatcher(count);
        dispatcher.Start();
        var threads = dispatcher.Threads;

        foreach (var dispatcherThread in threads)
            Assert.IsTrue(dispatcherThread.ThreadState.HasFlag(ThreadState.Background));

        Assert.IsTrue(dispatcher.Stop());

        foreach (var dispatcherThread in threads)
            Assert.IsFalse(dispatcherThread.IsAlive);

    }


    [Test]
    public async Task Start_WillRestart()
    {
        var count = 5;
        var dispatcher = new ThreadDispatcher(count);
        dispatcher.Start();

        dispatcher.Stop();

        dispatcher.Start();
        await dispatcher.Queue(() => { Assert.Pass(); }).TestTimeout();
    }

    [Test]
    public void QueueThrowsWhenStopped()
    {
        var dispatcher = new ThreadDispatcher(1);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await dispatcher.Queue(() => { Assert.Pass(); }).TestTimeout();
        });

    }
}
