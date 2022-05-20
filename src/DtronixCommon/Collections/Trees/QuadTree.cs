using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections;
/// <summary>
/// https://stackoverflow.com/a/48354356
/// Review https://github.com/Appleguysnake/DragonSpace-Demo
/// </summary>
public class Quadtree
{
    // Creates a quadtree with the requested extents, maximum elements per leaf, and maximum tree depth.
    public Quadtree(int width, int height, int startMaxElements, int startMaxDepth)
    {
        _maxElements = startMaxElements;
        _maxDepth = startMaxDepth;

        // Insert the root node to the qt.
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);

        // Set the extents of the root node.
        _rootMx = width / 2;
        _rootMy = height / 2;
        _rootSx = _rootMx;
        _rootSy = _rootMy;
    }

    // Outputs a list of elements found in the specified rectangle.
    public int Insert(float x1, float y1, float x2, float y2)
    {
        // Insert a new element.
        int newElement = _elts.Insert();

        // Set the fields of the new element.
        _elts.Set(newElement, _eltIdxLft, (int)x1);
        _elts.Set(newElement, _eltIdxTop, (int)y1);
        _elts.Set(newElement, _eltIdxRgt, (int)x2);
        _elts.Set(newElement, _eltIdxBtm, (int)y2);
        //_elts.Set(newElement, _eltIdxId, id);

        // Insert the element to the appropriate leaf node(s).
        node_insert(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, newElement);
        return newElement;
    }

    // Removes the specified element from the tree.
    public void Remove(int element)
    {
        // Find the leaves.
        int lft = _elts.Get(element, _eltIdxLft);
        int top = _elts.Get(element, _eltIdxTop);
        int rgt = _elts.Get(element, _eltIdxRgt);
        int btm = _elts.Get(element, _eltIdxBtm);
        IntList leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, lft, top, rgt, btm);

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            int ndIndex = leaves.Get(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            int nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _enodes.Get(nodeIndex, _enodeIdxElt) != element)
            {
                prevIndex = nodeIndex;
                nodeIndex = _enodes.Get(nodeIndex, _enodeIdxNext);
            }

            if (nodeIndex != -1)
            {
                // Remove the element node.
                int nextIndex = _enodes.Get(nodeIndex, _enodeIdxNext);
                if (prevIndex == -1)
                    _nodes.Set(ndIndex, _nodeIdxFc, nextIndex);
                else
                    _enodes.Set(prevIndex, _enodeIdxNext, nextIndex);
                _enodes.Erase(nodeIndex);

                // Decrement the leaf element count.
                _nodes.Set(ndIndex, _nodeIdxNum, _nodes.Get(ndIndex, _nodeIdxNum) - 1);
            }
        }

        // Remove the element.
        _elts.Erase(element);
    }

    // Cleans up the tree, removing empty leaves.
    public void Cleanup()
    {
        IntList toProcess = new IntList(1);

        // Only process the root if it's not a leaf.
        if (_nodes.Get(0, _nodeIdxNum) == -1)
        {
            // Push the root index to the stack.
            toProcess.Set(toProcess.PushBack(), 0, 0);
        }

        while (toProcess.Size() > 0)
        {
            // Pop a node from the stack.
            int node = toProcess.Get(toProcess.Size() - 1, 0);
            int fc = _nodes.Get(node, _nodeIdxFc);
            int numEmptyLeaves = 0;
            toProcess.PopBack();

            // Loop through the children.
            for (int j = 0; j < 4; ++j)
            {
                int child = fc + j;

                // Increment empty leaf count if the child is an empty 
                // leaf. Otherwise if the child is a branch, add it to
                // the stack to be processed in the next iteration.
                if (_nodes.Get(child, _nodeIdxNum) == 0)
                    ++numEmptyLeaves;
                else if (_nodes.Get(child, _nodeIdxNum) == -1)
                {
                    // Push the child index to the stack.
                    toProcess.Set(toProcess.PushBack(), 0, child);
                }
            }

            // If all the children were empty leaves, remove them and 
            // make this node the new empty leaf.
            if (numEmptyLeaves == 4)
            {
                // Remove all 4 children in reverse order so that they 
                // can be reclaimed on subsequent insertions in proper
                // order.
                _nodes.Erase(fc + 3);
                _nodes.Erase(fc + 2);
                _nodes.Erase(fc + 1);
                _nodes.Erase(fc + 0);

                // Make this node the new empty leaf.
                _nodes.Set(node, _nodeIdxFc, -1);
                _nodes.Set(node, _nodeIdxNum, 0);
            }
        }
    }

    // Returns a list of elements found in the specified rectangle.
    public IntList Query(int x1, int y1, int x2, int y2)
    {
        return Query(x1, y1, x2, y2, -1);
    }

    // Returns a list of elements found in the specified rectangle excluding the
    // specified element to omit.
    public IntList Query(int x1, int y1, int x2, int y2, int omitElement)
    {
        IntList intListOut = new IntList(1);

        // Find the leaves that intersect the specified query rectangle.
        IntList leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, x1, y1, x2, y2);

        if (_tempSize < _elts.Size())
        {
            _tempSize = _elts.Size();
            _temp = new bool[_tempSize];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            int ndIndex = leaves.Get(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _enodes.Get(eltNodeIndex, _enodeIdxElt);
                int lft = _elts.Get(element, _eltIdxLft);
                int top = _elts.Get(element, _eltIdxTop);
                int rgt = _elts.Get(element, _eltIdxRgt);
                int btm = _elts.Get(element, _eltIdxBtm);
                if (!_temp[element] && element != omitElement && Intersect(x1, y1, x2, y2, lft, top, rgt, btm))
                {
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp[element] = true;
                }
                eltNodeIndex = _enodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.Size(); ++j)
            _temp[intListOut.Get(j, 0)] = false;

        return intListOut;
    }

    // Returns a list of elements found in the specified rectangle excluding the
    // specified element to omit.
    public void QueryVisitor(float x1, float y1, float x2, float y2, Action<int> visit)
    {
        // Find the leaves that intersect the specified query rectangle.
        int qlft = (int)x1;
        int qtop = (int)y1;
        int qrgt = (int)x2;
        int qbtm = (int)y2;
        IntList leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, qlft, qtop, qrgt, qbtm);

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            int ndIndex = leaves.Get(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _enodes.Get(eltNodeIndex, _enodeIdxElt);
                int lft = _elts.Get(element, _eltIdxLft);
                int top = _elts.Get(element, _eltIdxTop);
                int rgt = _elts.Get(element, _eltIdxRgt);
                int btm = _elts.Get(element, _eltIdxBtm);
                //int id = _elts.Get(element, _eltIdxId);
                if (Intersect(qlft, qtop, qrgt, qbtm, lft, top, rgt, btm))
                {
                    visit(element);
                }
                eltNodeIndex = _enodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }
    }

    // Traverses all the nodes in the tree, calling 'branch' for branch nodes and 'leaf' 
    // for leaf nodes.
    public void Traverse(IQtVisitor visitor)
    {
        IntList toProcess = new IntList(_ndNum);
        PushNode(toProcess, 0, 0, _rootMx, _rootMy, _rootSx, _rootSy);

        while (toProcess.Size() > 0)
        {
            int backIdx = toProcess.Size() - 1;
            int ndMx = toProcess.Get(backIdx, _ndIdxMx);
            int ndMy = toProcess.Get(backIdx, _ndIdxMy);
            int ndSx = toProcess.Get(backIdx, _ndIdxSx);
            int ndSy = toProcess.Get(backIdx, _ndIdxSy);
            int ndIndex = toProcess.Get(backIdx, _ndIdxIndex);
            int ndDepth = toProcess.Get(backIdx, _ndIdxDepth);
            int fc = _nodes.Get(ndIndex, _nodeIdxFc);
            toProcess.PopBack();

            if (_nodes.Get(ndIndex, _nodeIdxNum) == -1)
            {
                // Push the children of the branch to the stack.
                int hx = ndSx >> 1, hy = ndSy >> 1;
                int l = ndMx - hx, t = ndMy - hx, r = ndMx + hx, b = ndMy + hy;
                PushNode(toProcess, fc + 0, ndDepth + 1, l, t, hx, hy);
                PushNode(toProcess, fc + 1, ndDepth + 1, r, t, hx, hy);
                PushNode(toProcess, fc + 2, ndDepth + 1, l, b, hx, hy);
                PushNode(toProcess, fc + 3, ndDepth + 1, r, b, hx, hy);
                visitor.Branch(this, ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy);
            }
            else
            {
                visitor.Leaf(this, ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy);
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(in int l1, in int t1, in int r1, in int b1,
        in int l2, in int t2, in int r2, in int b2)
    {
        return l2 <= r1 && r2 >= l1 && t2 <= b1 && b2 >= t1;
    }

    private static void PushNode(IntList nodes, int ndIndex, int ndDepth, int ndMx, int ndMy, int ndSx, int ndSy)
    {
        int backIdx = nodes.PushBack();
        nodes.Set(backIdx, _ndIdxMx, ndMx);
        nodes.Set(backIdx, _ndIdxMy, ndMy);
        nodes.Set(backIdx, _ndIdxSx, ndSx);
        nodes.Set(backIdx, _ndIdxSy, ndSy);
        nodes.Set(backIdx, _ndIdxIndex, ndIndex);
        nodes.Set(backIdx, _ndIdxDepth, ndDepth);
    }

    private IntList find_leaves(int node, int depth,
                                int mx, int my, int sx, int sy,
                                int lft, int top, int rgt, int btm)
    {
        IntList leaves = new IntList(_ndNum);
        IntList toProcess = new IntList(_ndNum);
        PushNode(toProcess, node, depth, mx, my, sx, sy);

        while (toProcess.Size() > 0)
        {
            int backIdx = toProcess.Size() - 1;
            int ndMx = toProcess.Get(backIdx, _ndIdxMx);
            int ndMy = toProcess.Get(backIdx, _ndIdxMy);
            int ndSx = toProcess.Get(backIdx, _ndIdxSx);
            int ndSy = toProcess.Get(backIdx, _ndIdxSy);
            int ndIndex = toProcess.Get(backIdx, _ndIdxIndex);
            int ndDepth = toProcess.Get(backIdx, _ndIdxDepth);
            toProcess.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                PushNode(leaves, ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Get(ndIndex, _nodeIdxFc);
                int hx = ndSx / 2, hy = ndSy / 2;
                int l = ndMx - hx, t = ndMy - hx, r = ndMx + hx, b = ndMy + hy;

                if (top <= ndMy)
                {
                    if (lft <= ndMx)
                        PushNode(toProcess, fc + 0, ndDepth + 1, l, t, hx, hy);
                    if (rgt > ndMx)
                        PushNode(toProcess, fc + 1, ndDepth + 1, r, t, hx, hy);
                }
                if (btm > ndMy)
                {
                    if (lft <= ndMx)
                        PushNode(toProcess, fc + 2, ndDepth + 1, l, b, hx, hy);
                    if (rgt > ndMx)
                        PushNode(toProcess, fc + 3, ndDepth + 1, r, b, hx, hy);
                }
            }
        }
        return leaves;
    }

    private void node_insert(int index, int depth, int mx, int my, int sx, int sy, int element)
    {
        // Find the leaves and insert the element to all the leaves found.
        int lft = _elts.Get(element, _eltIdxLft);
        int top = _elts.Get(element, _eltIdxTop);
        int rgt = _elts.Get(element, _eltIdxRgt);
        int btm = _elts.Get(element, _eltIdxBtm);
        IntList leaves = find_leaves(index, depth, mx, my, sx, sy, lft, top, rgt, btm);

        for (int j = 0; j < leaves.Size(); ++j)
        {
            int ndMx = leaves.Get(j, _ndIdxMx);
            int ndMy = leaves.Get(j, _ndIdxMy);
            int ndSx = leaves.Get(j, _ndIdxSx);
            int ndSy = leaves.Get(j, _ndIdxSy);
            int ndIndex = leaves.Get(j, _ndIdxIndex);
            int ndDepth = leaves.Get(j, _ndIdxDepth);
            leaf_insert(ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy, element);
        }
    }

    private void leaf_insert(int node, int depth, int mx, int my, int sx, int sy, int element)
    {
        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);
        _nodes.Set(node, _nodeIdxFc, _enodes.Insert());
        _enodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _enodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elts = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _enodes.Get(index, _enodeIdxNext);
                int elt = _enodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _enodes.Erase(index);

                // Insert element to the list.
                elts.Set(elts.PushBack(), 0, elt);
            }

            // Start by allocating 4 child nodes.
            int fc = _nodes.Insert();
            _nodes.Insert();
            _nodes.Insert();
            _nodes.Insert();
            _nodes.Set(node, _nodeIdxFc, fc);

            // Initialize the new child nodes.
            for (int j = 0; j < 4; ++j)
            {
                _nodes.Set(fc + j, _nodeIdxFc, -1);
                _nodes.Set(fc + j, _nodeIdxNum, 0);
            }

            // Transfer the elements in the former leaf node to its new children.
            _nodes.Set(node, _nodeIdxNum, -1);
            for (int j = 0; j < elts.Size(); ++j)
                node_insert(node, depth, mx, my, sx, sy, elts.Get(j, 0));
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Set(node, _nodeIdxNum, _nodes.Get(node, _nodeIdxNum) + 1);
        }
    }


    // ----------------------------------------------------------------------------------------
    // Element node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the next element in the leaf node. A value of -1 
    // indicates the end of the list.
    const int _enodeIdxNext = 0;

    // Stores the element index.
    const int _enodeIdxElt = 1;

    // Stores all the element nodes in the quadtree.
    private IntList _enodes = new IntList(2);

    // ----------------------------------------------------------------------------------------
    // Element fields:
    // ----------------------------------------------------------------------------------------
    // Stores the rectangle encompassing the element.
    const int _eltIdxLft = 0, _eltIdxTop = 1, _eltIdxRgt = 2, _eltIdxBtm = 3;

    // Stores the ID of the element.
    //const int _eltIdxId = 4;

    // Stores all the elements in the quadtree.
    private IntList _elts = new IntList(4);

    // ----------------------------------------------------------------------------------------
    // Node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the first child if this node is a branch or the first element
    // if this node is a leaf.
    const int _nodeIdxFc = 0;

    // Stores the number of elements in the node or -1 if it is not a leaf.
    static int _nodeIdxNum = 1;

    // Stores all the nodes in the quadtree. The first node in this
    // sequence is always the root.
    private IntList _nodes = new IntList(2);

    // ----------------------------------------------------------------------------------------
    // Node data fields:
    // ----------------------------------------------------------------------------------------
    const int _ndNum = 6;

    // Stores the extents of the node using a centered rectangle and half-size.
    const int _ndIdxMx = 0, _ndIdxMy = 1, _ndIdxSx = 2, _ndIdxSy = 3;

    // Stores the index of the node.
    const int _ndIdxIndex = 4;

    // Stores the depth of the node.
    const int _ndIdxDepth = 5;

    // ----------------------------------------------------------------------------------------
    // Data Members
    // ----------------------------------------------------------------------------------------
    // Temporary buffer used for queries.
    private bool[] _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Stores the quadtree extents.
    private int _rootMx, _rootMy, _rootSx, _rootSy;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;
}