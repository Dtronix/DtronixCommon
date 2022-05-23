using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using DtronixCommon.Collections.Trees;

namespace DtronixCommon.Collections;
/// <summary>
/// https://stackoverflow.com/a/48354356
/// Review https://github.com/Appleguysnake/DragonSpace-Demo
/// </summary>
public class LongQuadtree<T>
{
    // Creates a quadtree with the requested extents, maximum elements per leaf, and maximum tree depth.
    public LongQuadtree(long width, long height, int startMaxElements, int startMaxDepth)
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
    public long Insert(long x1, long y1, long x2, long y2)
    {
        // Insert a new element.
        var newElement = _elts.Insert();

        // Set the fields of the new element.
        _elts.Set(newElement, _eltIdxLft, x1);
        _elts.Set(newElement, _eltIdxTop, y1);
        _elts.Set(newElement, _eltIdxRgt, x2);
        _elts.Set(newElement, _eltIdxBtm, y2);
        //_elts.Set(newElement, _eltIdxId, id);

        // Insert the element to the appropriate leaf node(s).
        node_insert(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, newElement);
        return newElement;
    }

    // Removes the specified element from the tree.
    public void Remove(long element)
    {
        // Find the leaves.
        long lft = _elts.Get(element, _eltIdxLft);
        long top = _elts.Get(element, _eltIdxTop);
        long rgt = _elts.Get(element, _eltIdxRgt);
        long btm = _elts.Get(element, _eltIdxBtm);
        LongList leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, lft, top, rgt, btm);

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            var ndIndex = leaves.Get(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            long prevIndex = -1;
            while (nodeIndex != -1 && _enodes.Get(nodeIndex, _enodeIdxElt) != element)
            {
                prevIndex = nodeIndex;
                nodeIndex = _enodes.Get(nodeIndex, _enodeIdxNext);
            }

            if (nodeIndex != -1)
            {
                // Remove the element node.
                var nextIndex = _enodes.Get(nodeIndex, _enodeIdxNext);
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
        LongList toProcess = new LongList(1);

        // Only process the root if it's not a leaf.
        if (_nodes.Get(0, _nodeIdxNum) == -1)
        {
            // Push the root index to the stack.
            toProcess.Set(toProcess.PushBack(), 0, 0);
        }

        while (toProcess.Size() > 0)
        {
            // Pop a node from the stack.
            long node = toProcess.Get(toProcess.Size() - 1, 0);
            long fc = _nodes.Get(node, _nodeIdxFc);
            long numEmptyLeaves = 0;
            toProcess.PopBack();

            // Loop through the children.
            for (int j = 0; j < 4; ++j)
            {
                long child = fc + j;

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

    // Returns a list of elements found in the specified rectangle excluding the
    // specified element to omit.
    public LongList Query(long x1, long y1, long x2, long y2, long omitElement)
    {
        LongList intListOut = new LongList(1);

        // Find the leaves that intersect the specified query rectangle.
        LongList leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, x1, y1, x2, y2);

        if (_temp.Length < _elts.Size())
        {
            _temp = new bool[_elts.Size()];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            long ndIndex = leaves.Get(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            long eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                long element = _enodes.Get(eltNodeIndex, _enodeIdxElt);
                long lft = _elts.Get(element, _eltIdxLft);
                long top = _elts.Get(element, _eltIdxTop);
                long rgt = _elts.Get(element, _eltIdxRgt);
                long btm = _elts.Get(element, _eltIdxBtm);
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

    public void QueryTraverse(long x1, long y1, long x2, long y2, Action<long> action)
    {
        var intListOut = new LongList(1);

        // Find the leaves that intersect the specified query rectangle.
        LongList leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, x1, y1, x2, y2);

        if (_temp.Length < _elts.Size())
        {
            _temp = new bool[_elts.Size()];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            long ndIndex = leaves.Get(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            long eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                long element = _enodes.Get(eltNodeIndex, _enodeIdxElt);
                long lft = _elts.Get(element, _eltIdxLft);
                long top = _elts.Get(element, _eltIdxTop);
                long rgt = _elts.Get(element, _eltIdxRgt);
                long btm = _elts.Get(element, _eltIdxBtm);
                if (!_temp[element] && Intersect(x1, y1, x2, y2, lft, top, rgt, btm))
                {
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    action(element);
                    _temp[element] = true;
                }
                eltNodeIndex = _enodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.Size(); ++j)
            _temp[intListOut.Get(j, 0)] = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(in long l1, in long t1, in long r1, in long b1,
        in long l2, in long t2, in long r2, in long b2)
    {
        return l2 <= r1 && r2 >= l1 && t2 <= b1 && b2 >= t1;
    }

    private static void PushNode(LongList nodes, long ndIndex, long ndDepth, long ndMx, long ndMy, long ndSx, long ndSy)
    {
        var backIdx = nodes.PushBack();
        nodes.Set(backIdx, _ndIdxMx, ndMx);
        nodes.Set(backIdx, _ndIdxMy, ndMy);
        nodes.Set(backIdx, _ndIdxSx, ndSx);
        nodes.Set(backIdx, _ndIdxSy, ndSy);
        nodes.Set(backIdx, _ndIdxIndex, ndIndex);
        nodes.Set(backIdx, _ndIdxDepth, ndDepth);
    }

    private LongList find_leaves(long node, long depth,
                                long mx, long my, long sx, long sy,
                                long lft, long top, long rgt, long btm)
    {
        LongList leaves = new LongList(_ndNum);
        LongList toProcess = new LongList(_ndNum);
        PushNode(toProcess, node, depth, mx, my, sx, sy);

        while (toProcess.Size() > 0)
        {
            long backIdx = toProcess.Size() - 1;
            long ndMx = toProcess.Get(backIdx, _ndIdxMx);
            long ndMy = toProcess.Get(backIdx, _ndIdxMy);
            long ndSx = toProcess.Get(backIdx, _ndIdxSx);
            long ndSy = toProcess.Get(backIdx, _ndIdxSy);
            long ndIndex = toProcess.Get(backIdx, _ndIdxIndex);
            long ndDepth = toProcess.Get(backIdx, _ndIdxDepth);
            toProcess.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                PushNode(leaves, ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                long fc = _nodes.Get(ndIndex, _nodeIdxFc);
                long hx = ndSx / 2, hy = ndSy / 2;
                long l = ndMx - hx, t = ndMy - hx, r = ndMx + hx, b = ndMy + hy;

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

    private void node_insert(long index, long depth, long mx, long my, long sx, long sy, long element)
    {
        // Find the leaves and insert the element to all the leaves found.
        long lft = _elts.Get(element, _eltIdxLft);
        long top = _elts.Get(element, _eltIdxTop);
        long rgt = _elts.Get(element, _eltIdxRgt);
        long btm = _elts.Get(element, _eltIdxBtm);
        LongList leaves = find_leaves(index, depth, mx, my, sx, sy, lft, top, rgt, btm);

        for (int j = 0; j < leaves.Size(); ++j)
        {
            long ndMx = leaves.Get(j, _ndIdxMx);
            long ndMy = leaves.Get(j, _ndIdxMy);
            long ndSx = leaves.Get(j, _ndIdxSx);
            long ndSy = leaves.Get(j, _ndIdxSy);
            long ndIndex = leaves.Get(j, _ndIdxIndex);
            long ndDepth = leaves.Get(j, _ndIdxDepth);
            leaf_insert(ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy, element);
        }
    }

    private void leaf_insert(long node, long depth, long mx, long my, long sx, long sy, long element)
    {
        // Insert the element node to the leaf.
        long ndFc = _nodes.Get(node, _nodeIdxFc);
        _nodes.Set(node, _nodeIdxFc, _enodes.Insert());
        _enodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _enodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            LongList elts = new LongList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                long index = _nodes.Get(node, _nodeIdxFc);
                long nextIndex = _enodes.Get(index, _enodeIdxNext);
                long elt = _enodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _enodes.Erase(index);

                // Insert element to the list.
                elts.Set(elts.PushBack(), 0, elt);
            }

            // Start by allocating 4 child nodes.
            long fc = _nodes.Insert();
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
    private LongList _enodes = new LongList(2);

    // ----------------------------------------------------------------------------------------
    // Element fields:
    // ----------------------------------------------------------------------------------------
    // Stores the rectangle encompassing the element.
    const int _eltIdxLft = 0, _eltIdxTop = 1, _eltIdxRgt = 2, _eltIdxBtm = 3;

    // Stores the ID of the element.
    //const int _eltIdxId = 4;

    // Stores all the elements in the quadtree.
    private LongList _elts = new LongList(4);

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
    private LongList _nodes = new LongList(2);

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

    // Stores the quadtree extents.
    private long _rootMx, _rootMy, _rootSx, _rootSy;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;
}