using System.Diagnostics;
using System.Threading.Tasks;
using DtronixCommon.Threading;
using NUnit.Framework;

namespace DtronixCommon.Tests;

public class BoundaryTests
{
    [Test]
    public void OneDimensionalBoundaryIsNotEmpty()
    {
        Assert.That(new BoundaryF(0, -1, 0, 1).IsEmpty, Is.False);
        Assert.That(new BoundaryF(-1, 0, 1, 0).IsEmpty, Is.False);
    }

    [Test]
    public void CanUnionBoundariesWithOneDimension()
    {
        Assert.That(new BoundaryF(0, -1, 0, 1).Union(new BoundaryF(-1, 0, 1, 0)), Is.EqualTo(new BoundaryF(-1, -1, 1, 1)));
    }
}
