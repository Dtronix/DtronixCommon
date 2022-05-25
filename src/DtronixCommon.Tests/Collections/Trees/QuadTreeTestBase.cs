using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixCommon.Collections.Trees;

namespace DtronixCommon.Tests.Collections.Trees;

public class QuadTreeTestBase
{
    protected class TestQuadTreeItem : IQuadTreeItem
    {
        public int QuadTreeId { get; set; } = -1;
    }

}
