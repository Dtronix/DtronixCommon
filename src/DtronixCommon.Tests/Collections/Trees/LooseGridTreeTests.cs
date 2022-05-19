using DtronixCommon.Collections;
using DtronixCommon.Collections.Trees;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections.Trees;

public class LooseGridTreeTests
{

    [Test]
    public void Test()
    {

        var grid = LooseGridTree.lgrid_create(50, 50, 50, 50, 0, 0, 100, 100);

        //LooseGridTree.lgrid_insert(grid, 321, 20, 20, 21, 21);
        //LooseGridTree.lgrid_insert(grid, 32111, 22, 22, 23, 23);
        LooseGridTree.lgrid_insert(grid, 99999, 780, 780, 800, 800);
        //LooseGridTree.lgrid_insert(grid, 999, 22, 22, 23, 23);

        var query = LooseGridTree.lgrid_query(grid, -50, -50, 50, 50, -500);
        /*
        var qt = new Quadtree(500, 500, 100, 100);

        var val = qt.Insert(1234, 0, 0, 3, 3);
        //var va245l = qt.Insert(12345, 2, 2, 3, 3);
        //var va2l = qt.Insert(123456, 3, 3, 4, 4);
        //var va24l = qt.Insert(1234567, 3, 3, 4, 4);

        qt.Traverse(new Traverse());

        var items = qt.Query(-25, -25, 25, 25);*/
    }
    
}