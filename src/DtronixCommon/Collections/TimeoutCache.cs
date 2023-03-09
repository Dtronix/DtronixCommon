using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections
{
    public class TimeoutCache<T> : IDisposable
        where T : IDisposable, new()
    {

        private readonly ConcurrentBag<T> _items = new();
        private ReaderWriterLockSlim _monitorLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly Timer _timer;

        public TimeoutCache()
        {
            _timer = new Timer(UsageReviewTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void UsageReviewTimer(object? state)
        {
            throw new NotImplementedException();
        }

        public void Get(out T item)
        {
            _monitorLock.EnterReadLock();
            if (!_items.TryTake(out item!))
            {
                item = new T();
            }
            _monitorLock.ExitReadLock();
        }

        public void Return(T item)
        {
            ThreadPool.RegisterWaitForSingleObject()
            _items.Add(item);
            _timer.Change()
        }

        public void Dispose()
        {
            _monitorLock.Dispose();
            _timer.Dispose();
            _items.Clear();
        }
    }
}
