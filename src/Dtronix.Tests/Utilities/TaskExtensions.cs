using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Dtronix.Tests.Utilities
{
    internal static class TaskExtensions
    {
        public static void AssertTimesOut(this Task task, int milliseconds)
        {
            Assert.ThrowsAsync<TimeoutException>(() =>
            {
                var modifiedTask = task.WaitAsync(TimeSpan.FromMilliseconds(milliseconds));
                return modifiedTask;
            });
        }

        public static Task TestTimeout(this Task task, int milliseconds = 1000)
        {
            try
            {
                return task.WaitAsync(TimeSpan.FromMilliseconds(milliseconds));
            }
            catch (TimeoutException e)
            {
                Assert.Fail("Test exceeded maximum execution time.");
            }
            return Task.CompletedTask;
        }

        public static Task<TResult> TestTimeout<TResult>(this Task<TResult> task, int milliseconds = 1000)
        {
            try
            {
                return task.WaitAsync(TimeSpan.FromMilliseconds(milliseconds));
            }
            catch (TimeoutException e)
            {
                Assert.Fail("Test exceeded maximum execution time.");
            }
            return Task.FromResult(default(TResult));
        }
    }
}
