using System;
using System.Threading.Tasks;
using Dtronix.Tests.Utilities;
using Dtronix.Threading.Tasks;
using NUnit.Framework;

namespace Dtronix.Tests.Threading.Tasks;

public class ReusableTaskCompletionTests
{
    [Test]
    public async Task ReusableTaskCompletionGeneric()
    {
        new TaskCompletionSource().SetCanceled();
        var manualReset = new ManualResetAwaiterSource<bool>();
        _ = Task.Run(async () =>
        {
            await Task.Delay(25);
            manualReset.TrySetResult(true);
            manualReset.Reset();

            await Task.Delay(25);
            manualReset.TrySetResult(false);
            manualReset.Reset();

            await Task.Delay(25);
            manualReset.TrySetException(new ApplicationException());
            manualReset.Reset();

            await Task.Delay(25);
            manualReset.TrySetCanceled();
            manualReset.Reset();
        });
        Assert.IsTrue(await manualReset.Awaiter);

        // Spin while the task resets.
        await Task.Delay(1);
        Assert.IsFalse(await manualReset.Awaiter);

        // Spin while the task resets.
        await Task.Delay(1);
        Assert.ThrowsAsync<ApplicationException>(async () => await manualReset.Awaiter);

        // Spin while the task resets.
        await Task.Delay(1);
        Assert.ThrowsAsync<OperationCanceledException>(async () => await manualReset.Awaiter);
        
        manualReset.ToTask().AssertTimesOut(100);
    }

    [Test]
    public async Task ReusableTaskCompletion()
    {
        new TaskCompletionSource().SetCanceled();
        var manualReset = new ManualResetAwaiterSource();

        var setResult1 = false;
        var setResult2 = false;
        _ = Task.Run(async () =>
        {
            await Task.Delay(25);
            setResult1 = true;
            manualReset.TrySetResult();

            manualReset.Reset();

            await Task.Delay(25);
            setResult2 = true;
            manualReset.TrySetResult();
            manualReset.Reset();

            await Task.Delay(25);
            manualReset.TrySetException(new ApplicationException());
            manualReset.Reset();

            await Task.Delay(25);
            manualReset.TrySetCanceled();
            manualReset.Reset();
        });
        await manualReset.Awaiter;
        Assert.IsTrue(setResult1);

        // Spin while the task resets.
        await Task.Delay(1);
        await manualReset.Awaiter;
        Assert.IsTrue(setResult2);

        // Spin while the task resets.
        await Task.Delay(1);
        Assert.ThrowsAsync<ApplicationException>(async () => await manualReset.Awaiter);

        // Spin while the task resets.
        await Task.Delay(1);
        Assert.ThrowsAsync<OperationCanceledException>(async () => await manualReset.Awaiter);

        manualReset.ToTask().AssertTimesOut(100);
    }


}

