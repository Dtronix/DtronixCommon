﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DtronixCommonCodeGeneration.Collections.Trees
{

    [Generator]
    public class QuadTreeGenerator : ISourceGenerator
    {
        private class Config
        {
            public string ClassName { get; set; }
            public string MainListClass { get; set; }
            public string NumberType { get; set; }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var quadTrees = new Config[]
            {
                new Config()
                {
                    ClassName = "FloatQuadTree",
                    MainListClass = "FloatList",
                    NumberType = "float",
                },
                new Config()
                {
                    ClassName = "LongQuadTree",
                    MainListClass = "LongList",
                    NumberType = "long",
                },
                new Config()
                {
                    ClassName = "IntQuadTree",
                    MainListClass = "IntList",
                    NumberType = "int",
                },
                new Config()
                {
                    ClassName = "DoubleQuadTree",
                    MainListClass = "DoubleList",
                    NumberType = "double",
                }
            };
            foreach (var config in quadTrees)
            {
                var sourceBuilder = new StringBuilder(@"
using System.Runtime.CompilerServices;
using DtronixCommon.Collections.Lists;

namespace DtronixCommon.Collections.Trees;

/// <summary>
/// </summary>
public class " + config.ClassName + @"<T>
    where T : IQuadTreeItem
{
    // ----------------------------------------------------------------------------------------
    // Element node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the next element in the leaf node. A value of -1 
    // indicates the end of the list.
    const int _enodeIdxNext = 0;

    // Stores the element index.
    const int _enodeIdxElt = 1;

    // Stores all the element nodes in the quadtree.
    private IntList _eleNodes = new IntList(2);

    // ----------------------------------------------------------------------------------------
    // Element fields:
    // ----------------------------------------------------------------------------------------
    // Stores the rectangle encompassing the element.
    const int _eltIdxLft = 0;
    const int _eltIdxTop = 1;
    const int _eltIdxRgt = 2;
    const int _eltIdxBtm = 3;

    // Stores the ID of the element.
    const int _eleBoundsItems = 4;

    // Stores all the elements in the quadtree.
    private " + config.MainListClass + @" _eleBounds = new " + config.MainListClass + @"(_eleBoundsItems);

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
    const int _ndIdxMx = 0;
    const int _ndIdxMy = 1;
    const int _ndIdxSx = 2;
    const int _ndIdxSy = 3;

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
    private " + config.NumberType + @" _rootMx;
    private " + config.NumberType + @" _rootMy;
    private " + config.NumberType + @" _rootSx;
    private " + config.NumberType + @" _rootSy;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T?[] items = new T[128];


    /// <summary>
    /// Creates a quadtree with the specified width, height.  Sets the max elements and depth at 8.
    /// </summary>
    /// <param name=""width"">Sets the maximum width of the tree.</param>
    /// <param name=""height"">Sets the maximum height of the tree.</param>
        public " + config.ClassName + @"(" + config.NumberType + @" width, " + config.NumberType + @" height)
        : this(width, height, 8, 8)
    {
    }

    /// <summary>
    /// Creates a quadtree with the specified width, height.
    /// </summary>
    /// <param name=""width"">Sets the maximum width of the tree.</param>
    /// <param name=""height"">Sets the maximum height of the tree.</param>
    /// <param name=""startMaxElements"">Sta</param>
    /// <param name=""startMaxDepth"">Specifies the maximum depth before sub-dividing the tree.</param>
    public " + config.ClassName + @"(" + config.NumberType + @" width, " + config.NumberType + @" height, int startMaxElements, int startMaxDepth)
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

    /// <summary>
    /// Inserts an item into the quadtree.
    /// </summary>
    /// <param name=""x1"">Minim X coordinate.</param>
    /// <param name=""y1"">Minim Y coordinate.</param>
    /// <param name=""x2"">Maximum X coordinate.</param>
    /// <param name=""y2"">Maximum Y coordinate.</param>
    /// <param name=""item"">Item to insert into the quadtree.</param>
    /// <returns>Index of the new item index in the quadtree.</returns>
                public int Insert(" + config.NumberType + @" x1, " + config.NumberType + @" y1, " + config.NumberType + @" x2, " + config.NumberType + @" y2, T item)
    {
        // Insert a new element.
        var newElement = _eleBounds.Insert();

        if (newElement == items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = item;
        // Set the fields of the new element.
        _eleBounds.Set(newElement, _eltIdxLft, x1);
        _eleBounds.Set(newElement, _eltIdxTop, y1);
        _eleBounds.Set(newElement, _eltIdxRgt, x2);
        _eleBounds.Set(newElement, _eltIdxBtm, y2);

        // Insert the element to the appropriate leaf node(s).
        node_insert(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, newElement);
        item.QuadTreeId = newElement;
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name=""element"">Element to remove</param>
    public void Remove(T element)
    {
        // Find the leaves.
        var lft = _eleBounds.Get(element.QuadTreeId, _eltIdxLft);
        var top = _eleBounds.Get(element.QuadTreeId, _eltIdxTop);
        var rgt = _eleBounds.Get(element.QuadTreeId, _eltIdxRgt);
        var btm = _eleBounds.Get(element.QuadTreeId, _eltIdxBtm);
        var leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, lft, top, rgt, btm);

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            var ndIndex = (int)leaves.Get(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _eleNodes.Get(nodeIndex, _enodeIdxElt) != element.QuadTreeId)
            {
                prevIndex = nodeIndex;
                nodeIndex = _eleNodes.Get(nodeIndex, _enodeIdxNext);
            }

            if (nodeIndex != -1)
            {
                // Remove the element node.
                var nextIndex = _eleNodes.Get(nodeIndex, _enodeIdxNext);
                if (prevIndex == -1)
                    _nodes.Set(ndIndex, _nodeIdxFc, nextIndex);
                else
                    _eleNodes.Set(prevIndex, _enodeIdxNext, nextIndex);
                _eleNodes.Erase(nodeIndex);

                // Decrement the leaf element count.
                _nodes.Decrement(ndIndex, _nodeIdxNum);
            }
        }

        // Remove the element.
        _eleBounds.Erase(element.QuadTreeId);
        items[element.QuadTreeId] = default;
        element.QuadTreeId = -1;

    }

    /// <summary>
    /// Queries the quadtree for all the elements intersecting and encompassed in the passed bounds.
    /// </summary>
    /// <param name=""x1"">Minim X coordinate.</param>
    /// <param name=""y1"">Minim Y coordinate.</param>
    /// <param name=""x2"">Maximum X coordinate.</param>
    /// <param name=""y2"">Maximum Y coordinate.</param>
    /// <param name=""action"">Action to execute on each found element.</param>
    /// <param name=""cancellationToken"">Cancellation token to cancel the query.</param>
    /// <returns>Integer list of the ids of the elements contained in the query.</returns>
    public IntList Query(
        " + config.NumberType + @" x1,
        " + config.NumberType + @" y1,
        " + config.NumberType + @" x2, 
        " + config.NumberType + @" y2)
    {
        return Query(x1, y1, x2, y2, null, default);
    }

    /// <summary>
    /// Queries the quadtree for all the elements intersecting and encompassed in the passed bounds.
    /// </summary>
    /// <param name=""x1"">Minim X coordinate.</param>
    /// <param name=""y1"">Minim Y coordinate.</param>
    /// <param name=""x2"">Maximum X coordinate.</param>
    /// <param name=""y2"">Maximum Y coordinate.</param>
    /// <param name=""action"">Action to execute on each found element.</param>
    /// <param name=""cancellationToken"">Cancellation token to cancel the query.</param>
    /// <returns>Integer list of the ids of the elements contained in the query.</returns>
    public IntList Query(
        " + config.NumberType + @" x1,
        " + config.NumberType + @" y1,
        " + config.NumberType + @" x2,
        " + config.NumberType + @" y2,
        Action<T>? action, 
        CancellationToken cancellationToken = default)
    {
        var intListOut = new IntList(1);

        // Find the leaves that intersect the specified query rectangle.
        var leaves = find_leaves(0, 0, _rootMx, _rootMy, _rootSx, _rootSy, x1, y1, x2, y2);

        if (_tempSize < _eleBounds.Size())
        {
            _tempSize = _eleBounds.Size();
            _temp = new bool[_tempSize];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.Size(); ++j)
        {
            int ndIndex = (int)leaves.Get(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                var lft = _eleBounds.Get(element, _eltIdxLft);
                var top = _eleBounds.Get(element, _eltIdxTop);
                var rgt = _eleBounds.Get(element, _eltIdxRgt);
                var btm = _eleBounds.Get(element, _eltIdxBtm);
                if (!_temp[element] && Intersect(x1, y1, x2, y2, lft, top, rgt, btm))
                {
                    if(cancellationToken.IsCancellationRequested)
                        break;

                    intListOut.Set(intListOut.PushBack(), 0, element);
                    action?.Invoke(items[element]!);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
            if(cancellationToken.IsCancellationRequested)
                break;
        }

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.Size(); ++j)
            _temp[intListOut.Get(j, 0)] = false;

        return intListOut;
    }

    /// <summary>
    /// Cleans up the tree, removing empty leaves.
    /// </summary>
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
            int node = (int)toProcess.Get(toProcess.Size() - 1, 0);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(
        in " + config.NumberType + @" l1,
        in " + config.NumberType + @" t1,
        in " + config.NumberType + @" r1,
        in " + config.NumberType + @" b1,
        in " + config.NumberType + @" l2, 
        in " + config.NumberType + @" t2,
        in " + config.NumberType + @" r2,
        in " + config.NumberType + @" b2)
    {
        return l2 <= r1 && r2 >= l1 && t2 <= b1 && b2 >= t1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNode(" + config.MainListClass + @" nodes, int ndIndex, int ndDepth, " + config.NumberType + @" ndMx, " + config.NumberType + @" ndMy, " + config.NumberType + @" ndSx, " + config.NumberType + @" ndSy)
    {
        var backIdx = nodes.PushBack();
        nodes.Set(backIdx, _ndIdxMx, ndMx);
        nodes.Set(backIdx, _ndIdxMy, ndMy);
        nodes.Set(backIdx, _ndIdxSx, ndSx);
        nodes.Set(backIdx, _ndIdxSy, ndSy);
        nodes.Set(backIdx, _ndIdxIndex, ndIndex);
        nodes.Set(backIdx, _ndIdxDepth, ndDepth);
    }

    private " + config.MainListClass + @" find_leaves(
        int node, 
        int depth,
        " + config.NumberType + @" mx,
        " + config.NumberType + @" my,
        " + config.NumberType + @" sx, 
        " + config.NumberType + @" sy,
        " + config.NumberType + @" lft,
        " + config.NumberType + @" top,
        " + config.NumberType + @" rgt, 
        " + config.NumberType + @" btm)
    {
        var leaves = new " + config.MainListClass + @"(_ndNum);
        var toProcess = new " + config.MainListClass + @"(_ndNum);
        PushNode(toProcess, node, depth, mx, my, sx, sy);

        while (toProcess.Size() > 0)
        {
            int backIdx = toProcess.Size() - 1;
            var ndMx = toProcess.Get(backIdx, _ndIdxMx);
            var ndMy = toProcess.Get(backIdx, _ndIdxMy);
            var ndSx = toProcess.Get(backIdx, _ndIdxSx);
            var ndSy = toProcess.Get(backIdx, _ndIdxSy);
            int ndIndex = toProcess.GetInt(backIdx, _ndIdxIndex);
            int ndDepth = toProcess.GetInt(backIdx, _ndIdxDepth);
            toProcess.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                PushNode(leaves, ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Get(ndIndex, _nodeIdxFc);
                " + config.NumberType + @" hx = ndSx / 2, hy = ndSy / 2;
                " + config.NumberType + @" l = ndMx - hx, t = ndMy - hx, r = ndMx + hx, b = ndMy + hy;

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

    private void node_insert(int index, int depth, " + config.NumberType + @" mx, " + config.NumberType + @" my, " + config.NumberType + @" sx, " + config.NumberType + @" sy, int element)
    {
        // Find the leaves and insert the element to all the leaves found.
        var lft = _eleBounds.Get(element, _eltIdxLft);
        var top = _eleBounds.Get(element, _eltIdxTop);
        var rgt = _eleBounds.Get(element, _eltIdxRgt);
        var btm = _eleBounds.Get(element, _eltIdxBtm);
        var leaves = find_leaves(index, depth, mx, my, sx, sy, lft, top, rgt, btm);

        for (int j = 0; j < leaves.Size(); ++j)
        {
            var ndMx = leaves.Get(j, _ndIdxMx);
            var ndMy = leaves.Get(j, _ndIdxMy);
            var ndSx = leaves.Get(j, _ndIdxSx);
            var ndSy = leaves.Get(j, _ndIdxSy);
            int ndIndex = (int)leaves.Get(j, _ndIdxIndex);
            int ndDepth = (int)leaves.Get(j, _ndIdxDepth);
            leaf_insert(ndIndex, ndDepth, ndMx, ndMy, ndSx, ndSy, element);
        }
    }

    private void leaf_insert(int node, int depth, " + config.NumberType + @" mx, " + config.NumberType + @" my, " + config.NumberType + @" sx, " + config.NumberType + @" sy, int element)
    {
        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);
        _nodes.Set(node, _nodeIdxFc, _eleNodes.Insert());
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elts = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _eleNodes.Get(index, _enodeIdxNext);
                int elt = _eleNodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _eleNodes.Erase(index);

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
                node_insert(node, depth, mx, my, sx, sy, (int)elts.Get(j, 0));
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }
}");
                // inject the created source into the users compilation
                context.AddSource(config.ClassName + ".g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            }


        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}