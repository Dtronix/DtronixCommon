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

        qt.Insert(-50, -50, -30, -30);
        qt.Insert(0, 0, 6, 6);
        qt.Insert(50, 50, 59, 59);

        qt.Insert(53, 53, 54, 54);
        qt.Insert(54, 54, 55, 55);
        qt.Insert(55, 55, 56, 56);
        qt.Insert(56, 56, 57, 57);
        qt.Insert(400, 400, 405, 405);
        qt.Insert(57, 57, 58, 58);


        //var va245l = qt.Insert(12345, 2, 2, 3, 3);
        //var va2l = qt.Insert(123456, 3, 3, 4, 4);
        //var va24l = qt.Insert(1234567, 3, 3, 4, 4);

        qt.Traverse(new Traverse());

        var indexes = qt.Query(-1000, -1000, 1000, 1000);

        qt.QueryVisitor(-1000, -1000, 1000, 1000, c =>
        {

        });
    }
    
}