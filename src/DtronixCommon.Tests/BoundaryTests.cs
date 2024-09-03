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
        Assert.IsFalse(new BoundaryF(0, -1, 0, 1).IsEmpty);
        Assert.IsFalse(new BoundaryF(-1, 0, 1, 0).IsEmpty);
    }

    [Test]
    public void CanUnionBoundariesWithOneDimension()
    {
        Assert.AreEqual(new BoundaryF(-1, -1, 1, 1), new BoundaryF(0, -1, 0, 1).Union(new BoundaryF(-1, 0, 1, 0)));
    }
}
