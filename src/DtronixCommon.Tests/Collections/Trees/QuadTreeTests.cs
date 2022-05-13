using System;
using System.Collections.Generic;
using System.Linq;
using DtronixCommon.Collections;
using NUnit.Framework;

namespace DtronixCommon.Tests.Collections;

public class QuadTreeTests
{
    private class Traverse : IQtVisitor
    {
        public void Branch(Quadtree qt, int node, int depth, int mx, int my, int sx, int sy)
        {
            //throw new NotImplementedException();
        }

        public void Leaf(Quadtree qt, int node, int depth, int mx, int my, int sx, int sy)
        {
            
            //throw new NotImplementedException();
        }
    }

    [Test]
    public void HeadIndexResets()
    {
        var qt = new Quadtree(500, 500, 100, 100);

        var val = qt.Insert(1234, 0, 0, 3, 3);
        //var va245l = qt.Insert(12345, 2, 2, 3, 3);
        //var va2l = qt.Insert(123456, 3, 3, 4, 4);
        //var va24l = qt.Insert(1234567, 3, 3, 4, 4);

        qt.Traverse(new Traverse());

        var items = qt.Query(-25, -25, 25, 25);
    }
    
}