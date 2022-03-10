using System.Threading.Tasks;
using Dtronix.Threading.Dispatcher;
using NUnit.Framework;

namespace Dtronix.Tests.Threading.Dispatcher;

public class QueueForCompletionTests
{
    private ThreadDispatcher _dispatcher;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new ThreadDispatcher(1);
        _dispatcher.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _dispatcher.Stop();
    }

}