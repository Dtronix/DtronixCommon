namespace DtronixCommon.Collections;

/// <summary>
/// https://stackoverflow.com/a/48354356
/// </summary>
public interface IQtVisitor
{
    // Called when traversing a branch node.
    // (mx, my) indicate the center of the node's AABB.
    // (sx, sy) indicate the half-size of the node's AABB.
    void Branch(Quadtree qt, int node, int depth, int mx, int my, int sx, int sy);

    // Called when traversing a leaf node.
    // (mx, my) indicate the center of the node's AABB.
    // (sx, sy) indicate the half-size of the node's AABB.
    void Leaf(Quadtree qt, int node, int depth, int mx, int my, int sx, int sy);
}