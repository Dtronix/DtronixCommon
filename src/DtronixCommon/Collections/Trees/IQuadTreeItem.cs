using DtronixCommon.Structures;

namespace DtronixCommon.Collections.Trees;

public interface IQuadTreeItem
{
    public int QuadTreeId { get; set; }
    public Boundary Bounds{ get; set; }
}
