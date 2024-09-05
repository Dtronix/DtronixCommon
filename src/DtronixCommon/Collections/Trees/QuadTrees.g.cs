#nullable enable
// ----------------------------
// This file is auto generated.
// Any modifications to this file will be overridden
// ----------------------------
using System.Runtime.CompilerServices;
using DtronixCommon.Collections.Lists;
using DtronixCommon.Reflection;
using System.Reflection;

namespace DtronixCommon.Collections.Trees;

/// <summary>
/// Quadtree with 
/// </summary>
public class FloatQuadTree<T> : IDisposable
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
    private readonly IntList _eleNodes;

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
    private readonly FloatList _eleBounds;

    // ----------------------------------------------------------------------------------------
    // Node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the first child if this node is a branch or the first element
    // if this node is a leaf.
    const int _nodeIdxFc = 0;

    // Stores the number of elements in the node or -1 if it is not a leaf.
    const int _nodeIdxNum = 1;

    /// <summary>
    /// A static array of integers used as the default values for new child nodes in the quadtree. 
    /// These values are used when a leaf node in the quadtree is full and needs to be split into four child nodes.
    /// Each pair of -1 and 0 in the array represents the initial state of a child node, where -1 indicates that the node is empty and 0 indicates that the node has no elements.
    /// </summary>
    private static readonly int[] _defaultNode4Values = new[] { -1, 0, -1, 0, -1, 0, -1, 0, };

    // Stores all the nodes in the quadtree. The first node in this
    // sequence is always the root.
    private readonly IntList _nodes;

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
    private bool[]? _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T[]? items;

    private readonly float[] _rootNode;

    private readonly FloatList.Cache _listCache = new FloatList.Cache(_ndNum);

    private static readonly Action<T, int> _quadTreeIdSetter;
    /// <summary>
    /// Items contained in the quad tree.  The index of the items matches their QuadTreeId.
    /// </summary>
    public ReadOnlySpan<T> Items => new ReadOnlySpan<T>(items);

    /// <summary>
    /// Creates a quadtree with the requested extents, maximum elements per leaf,
    /// and maximum tree depth and maximum capacity.
    /// </summary>
    /// <param name="width">Width extents of the root node.</param>
    /// <param name="height">Height extents of the root node.</param>
    /// <param name="startMaxElements">
    /// Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    /// the leaf is at the maximum allowed tree depth.
    /// </param>
    /// <param name="startMaxDepth">Maximum depth allowed for the quadtree.</param>
    /// <param name="initialCapacity">Initial element capacity for the tree.</param>
    public FloatQuadTree(float width, float height, int startMaxElements, int startMaxDepth, int initialCapacity = 128)
    {
        _maxElements = startMaxElements;
        _maxDepth = startMaxDepth;
        
        _eleNodes = new IntList(2, 2 * initialCapacity);
        _nodes = new IntList(2, 2 * initialCapacity);
        _eleBounds = new FloatList(_eleBoundsItems, _eleBoundsItems * initialCapacity);
        items = new T[initialCapacity];

        // Insert the root node to the qt.
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);

        // Set the extents of the root node.
        _rootNode = new[]
        {
            width / 2, // _ndIdxMx
            height / 2, // _ndIdxMy
            width / 2, // _ndIdxSx
            height / 2, // _ndIdxSy
            0, // _ndIdxIndex
            0 // _ndIdxDepth
        };
    }

    static FloatQuadTree()
    {
        // Implemented interface.
        var property = typeof(T).GetProperty("QuadTreeId", 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (property == null)
        {
            // Explicit interface implementation
            property = typeof(T).GetProperty("DtronixCommon.Collections.Trees.IQuadTreeItem.QuadTreeId",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        if (property == null)
            throw new Exception(
                $"Type {typeof(T).FullName} does not contain a integer property named QuadTreeId as required.");

        _quadTreeIdSetter = property.GetBackingField().CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an element into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="element">Item to insert into the quad tree.</param>
    /// <returns>Index of the new element. -1 if the element exists in the quad tree.</returns>
    public int Insert(float x1, float y1, float x2, float y2, T element)
    {
        if (element.QuadTreeId != -1)
            return -1;

        ReadOnlySpan<float> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items!.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = element;

        // Insert the element to the appropriate leaf node(s).
        NodeInsert(new ReadOnlySpan<float>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(element, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
    public void Remove(T element)
    {
        var id = element.QuadTreeId;
        // Find the leaves.
        var leaves = FindLeaves(
            new ReadOnlySpan<float>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _eleNodes.Get(nodeIndex, _enodeIdxElt) != id)
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
        leaves.Return();
        // Remove the element.
        _eleBounds.Erase(id);
        items![id] = default!;
        _quadTreeIdSetter(element, -1);
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

        while (toProcess.InternalCount > 0)
        {
            // Pop a node from the stack.
            int node = (int)toProcess.Get(toProcess.InternalCount - 1, 0);
            int fc = _nodes.Get(node, _nodeIdxFc);
            int numEmptyLeaves = 0;
            toProcess.InternalCount--;

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

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <returns>List of items which intersect the bounds.</returns>
    public List<T> Query(
        float x1,
        float y1,
        float x2, 
        float y2)
    {
        var listOut = new List<T>();
        ReadOnlySpan<float> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<float>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp![element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items![element]);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp![listOut[j].QuadTreeId] = false;

        return listOut;
    }

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    /// <returns>List of items which intersect the bounds.</returns>
      public IntList Query(
        float x1,
        float y1,
        float x2,
        float y2,
        Func<T, bool> callback)
    {
        var intListOut = new IntList(1);
        ReadOnlySpan<float> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<float>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp![element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp![intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found element.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    public unsafe void Walk(
        float x1,
        float y1,
        float x2,
        float y2,
        Func<T, bool> callback)
    {
        ReadOnlySpan<float> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<float>(_rootNode), bounds);

        bool cancel = false;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();
    }

    /// <summary>
    /// Clears the quad tree for use.
    /// </summary>
    public void Clear()
    {
        _eleNodes.Clear();
        _nodes.Clear();
        _eleBounds.Clear();

#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(
        ReadOnlySpan<float> b1,
        ReadOnlySpan<float> b2)
    {
        return b2[_eltIdxLft] <= b1[_eltIdxRgt] 
               && b2[_eltIdxRgt] >= b1[_eltIdxLft]
               && b2[_eltIdxTop] <= b1[_eltIdxBtm]
               && b2[_eltIdxBtm] >= b1[_eltIdxTop];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNode(FloatList nodes, int ndIndex, int ndDepth, float ndMx, float ndMy, float ndSx, float ndSy)
    {
        nodes.PushBack(stackalloc[] { ndMx, ndMy, ndSx, ndSy, ndIndex, ndDepth });
    }
    private FloatList.Cache.Item FindLeaves(
        ReadOnlySpan<float> data,
        ReadOnlySpan<float> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        var toProcessList = toProcess.List;
        var toProcessListData = toProcessList.Data!;

        toProcessList.PushBack(data);

        while (toProcessList.InternalCount > 0)
        {
            int backIdx = toProcessList.InternalCount - 1;
            int backOffset = backIdx * 6;
            //var ndData = toProcessList.Get(backIdx, 0, 6);
            //var ndIndex = (int)ndData[_ndIdxIndex];
            //var ndDepth = (int)ndData[_ndIdxDepth];

            var ndIndexOffset = (int)toProcessListData[backOffset + _ndIdxIndex] * 2;
            var ndDepth = (int)toProcessListData[backOffset + _ndIdxDepth];
            toProcessList.InternalCount--;

            // If this node is a leaf, insert it to the list.
            
            if (_nodes.Data![ndIndexOffset + _nodeIdxNum] != -1)
            {
                leaves.List.PushBack(toProcessList.Get(backIdx, 0, 6));
            }
            else
            {
                var mx = toProcessListData[backOffset + _ndIdxMx];
                var my = toProcessListData[backOffset + _ndIdxMy];
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Data[ndIndexOffset + _nodeIdxFc]; //_nodes.Get(ndIndex, _nodeIdxFc);
                var hx = toProcessListData[backOffset + _ndIdxSx] / 2;
                var hy = toProcessListData[backOffset + _ndIdxSy] / 2;
                var l = mx - hx;
                var r = mx + hx;

                var offset = toProcessList.InternalCount;
                toProcessList.EnsureSpaceAvailable(4);

                if (bounds[_eltIdxTop] <= my)
                {
                    var t = my - hx;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 0; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                        //toProcessList.PushBack(processItem);
                        //toProcessList.PushBack(stackalloc[] { l, t, hx, hy, fc + 0, ndDepth + 1 });
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 1; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                if (bounds[_eltIdxBtm] > my)
                {
                    var b = my + hy;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 2; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 3; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                toProcessList.InternalCount = offset;
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void NodeInsert(ReadOnlySpan<float> data, ReadOnlySpan<float> elementBounds, int elementId)
    {
        var leaves = FindLeaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            LeafInsert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void LeafInsert(int element, ReadOnlySpan<float> data)
    {
        var node = (int)data[_ndIdxIndex];
        var depth = (int)data[_ndIdxDepth];

        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);

        _nodes.Set(node, _nodeIdxFc, _eleNodes.Insert());
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elements = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _eleNodes.Get(index, _enodeIdxNext);
                int elt = _eleNodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _eleNodes.Erase(index);

                // Insert element to the list.
                elements.Set(elements.PushBack(), 0, elt);
            }

            // Start by allocating 4 child nodes.
            int fc = _nodes.PushBackCount(_defaultNode4Values, 4);
            _nodes.Set(node, _nodeIdxFc, fc);

            // Transfer the elements in the former leaf node to its new children.
            _nodes.Set(node, _nodeIdxNum, -1);
            for (int j = 0; j < elements.InternalCount; ++j)
            {
                var id = elements.GetInt(j, 0);
                NodeInsert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }

    /// <summary>
    /// Disposes the quad tree.
    /// </summary>
    public void Dispose()
    {
        if(items == null)
            return;

        _eleNodes?.Dispose();
        _eleBounds?.Dispose();
        _nodes?.Dispose();
#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        items = null!;
    }
}
/// <summary>
/// Quadtree with 
/// </summary>
public class LongQuadTree<T> : IDisposable
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
    private readonly IntList _eleNodes;

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
    private readonly LongList _eleBounds;

    // ----------------------------------------------------------------------------------------
    // Node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the first child if this node is a branch or the first element
    // if this node is a leaf.
    const int _nodeIdxFc = 0;

    // Stores the number of elements in the node or -1 if it is not a leaf.
    const int _nodeIdxNum = 1;

    /// <summary>
    /// A static array of integers used as the default values for new child nodes in the quadtree. 
    /// These values are used when a leaf node in the quadtree is full and needs to be split into four child nodes.
    /// Each pair of -1 and 0 in the array represents the initial state of a child node, where -1 indicates that the node is empty and 0 indicates that the node has no elements.
    /// </summary>
    private static readonly int[] _defaultNode4Values = new[] { -1, 0, -1, 0, -1, 0, -1, 0, };

    // Stores all the nodes in the quadtree. The first node in this
    // sequence is always the root.
    private readonly IntList _nodes;

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
    private bool[]? _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T[]? items;

    private readonly long[] _rootNode;

    private readonly LongList.Cache _listCache = new LongList.Cache(_ndNum);

    private static readonly Action<T, int> _quadTreeIdSetter;
    /// <summary>
    /// Items contained in the quad tree.  The index of the items matches their QuadTreeId.
    /// </summary>
    public ReadOnlySpan<T> Items => new ReadOnlySpan<T>(items);

    /// <summary>
    /// Creates a quadtree with the requested extents, maximum elements per leaf,
    /// and maximum tree depth and maximum capacity.
    /// </summary>
    /// <param name="width">Width extents of the root node.</param>
    /// <param name="height">Height extents of the root node.</param>
    /// <param name="startMaxElements">
    /// Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    /// the leaf is at the maximum allowed tree depth.
    /// </param>
    /// <param name="startMaxDepth">Maximum depth allowed for the quadtree.</param>
    /// <param name="initialCapacity">Initial element capacity for the tree.</param>
    public LongQuadTree(long width, long height, int startMaxElements, int startMaxDepth, int initialCapacity = 128)
    {
        _maxElements = startMaxElements;
        _maxDepth = startMaxDepth;
        
        _eleNodes = new IntList(2, 2 * initialCapacity);
        _nodes = new IntList(2, 2 * initialCapacity);
        _eleBounds = new LongList(_eleBoundsItems, _eleBoundsItems * initialCapacity);
        items = new T[initialCapacity];

        // Insert the root node to the qt.
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);

        // Set the extents of the root node.
        _rootNode = new[]
        {
            width / 2, // _ndIdxMx
            height / 2, // _ndIdxMy
            width / 2, // _ndIdxSx
            height / 2, // _ndIdxSy
            0, // _ndIdxIndex
            0 // _ndIdxDepth
        };
    }

    static LongQuadTree()
    {
        // Implemented interface.
        var property = typeof(T).GetProperty("QuadTreeId", 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (property == null)
        {
            // Explicit interface implementation
            property = typeof(T).GetProperty("DtronixCommon.Collections.Trees.IQuadTreeItem.QuadTreeId",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        if (property == null)
            throw new Exception(
                $"Type {typeof(T).FullName} does not contain a integer property named QuadTreeId as required.");

        _quadTreeIdSetter = property.GetBackingField().CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an element into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="element">Item to insert into the quad tree.</param>
    /// <returns>Index of the new element. -1 if the element exists in the quad tree.</returns>
    public int Insert(long x1, long y1, long x2, long y2, T element)
    {
        if (element.QuadTreeId != -1)
            return -1;

        ReadOnlySpan<long> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items!.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = element;

        // Insert the element to the appropriate leaf node(s).
        NodeInsert(new ReadOnlySpan<long>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(element, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
    public void Remove(T element)
    {
        var id = element.QuadTreeId;
        // Find the leaves.
        var leaves = FindLeaves(
            new ReadOnlySpan<long>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _eleNodes.Get(nodeIndex, _enodeIdxElt) != id)
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
        leaves.Return();
        // Remove the element.
        _eleBounds.Erase(id);
        items![id] = default!;
        _quadTreeIdSetter(element, -1);
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

        while (toProcess.InternalCount > 0)
        {
            // Pop a node from the stack.
            int node = (int)toProcess.Get(toProcess.InternalCount - 1, 0);
            int fc = _nodes.Get(node, _nodeIdxFc);
            int numEmptyLeaves = 0;
            toProcess.InternalCount--;

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

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <returns>List of items which intersect the bounds.</returns>
    public List<T> Query(
        long x1,
        long y1,
        long x2, 
        long y2)
    {
        var listOut = new List<T>();
        ReadOnlySpan<long> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<long>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp![element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items![element]);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp![listOut[j].QuadTreeId] = false;

        return listOut;
    }

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    /// <returns>List of items which intersect the bounds.</returns>
      public IntList Query(
        long x1,
        long y1,
        long x2,
        long y2,
        Func<T, bool> callback)
    {
        var intListOut = new IntList(1);
        ReadOnlySpan<long> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<long>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp![element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp![intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found element.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    public unsafe void Walk(
        long x1,
        long y1,
        long x2,
        long y2,
        Func<T, bool> callback)
    {
        ReadOnlySpan<long> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<long>(_rootNode), bounds);

        bool cancel = false;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();
    }

    /// <summary>
    /// Clears the quad tree for use.
    /// </summary>
    public void Clear()
    {
        _eleNodes.Clear();
        _nodes.Clear();
        _eleBounds.Clear();

#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(
        ReadOnlySpan<long> b1,
        ReadOnlySpan<long> b2)
    {
        return b2[_eltIdxLft] <= b1[_eltIdxRgt] 
               && b2[_eltIdxRgt] >= b1[_eltIdxLft]
               && b2[_eltIdxTop] <= b1[_eltIdxBtm]
               && b2[_eltIdxBtm] >= b1[_eltIdxTop];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNode(LongList nodes, int ndIndex, int ndDepth, long ndMx, long ndMy, long ndSx, long ndSy)
    {
        nodes.PushBack(stackalloc[] { ndMx, ndMy, ndSx, ndSy, ndIndex, ndDepth });
    }
    private LongList.Cache.Item FindLeaves(
        ReadOnlySpan<long> data,
        ReadOnlySpan<long> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        var toProcessList = toProcess.List;
        var toProcessListData = toProcessList.Data!;

        toProcessList.PushBack(data);

        while (toProcessList.InternalCount > 0)
        {
            int backIdx = toProcessList.InternalCount - 1;
            int backOffset = backIdx * 6;
            //var ndData = toProcessList.Get(backIdx, 0, 6);
            //var ndIndex = (int)ndData[_ndIdxIndex];
            //var ndDepth = (int)ndData[_ndIdxDepth];

            var ndIndexOffset = (int)toProcessListData[backOffset + _ndIdxIndex] * 2;
            var ndDepth = (int)toProcessListData[backOffset + _ndIdxDepth];
            toProcessList.InternalCount--;

            // If this node is a leaf, insert it to the list.
            
            if (_nodes.Data![ndIndexOffset + _nodeIdxNum] != -1)
            {
                leaves.List.PushBack(toProcessList.Get(backIdx, 0, 6));
            }
            else
            {
                var mx = toProcessListData[backOffset + _ndIdxMx];
                var my = toProcessListData[backOffset + _ndIdxMy];
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Data[ndIndexOffset + _nodeIdxFc]; //_nodes.Get(ndIndex, _nodeIdxFc);
                var hx = toProcessListData[backOffset + _ndIdxSx] / 2;
                var hy = toProcessListData[backOffset + _ndIdxSy] / 2;
                var l = mx - hx;
                var r = mx + hx;

                var offset = toProcessList.InternalCount;
                toProcessList.EnsureSpaceAvailable(4);

                if (bounds[_eltIdxTop] <= my)
                {
                    var t = my - hx;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 0; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                        //toProcessList.PushBack(processItem);
                        //toProcessList.PushBack(stackalloc[] { l, t, hx, hy, fc + 0, ndDepth + 1 });
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 1; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                if (bounds[_eltIdxBtm] > my)
                {
                    var b = my + hy;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 2; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 3; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                toProcessList.InternalCount = offset;
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void NodeInsert(ReadOnlySpan<long> data, ReadOnlySpan<long> elementBounds, int elementId)
    {
        var leaves = FindLeaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            LeafInsert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void LeafInsert(int element, ReadOnlySpan<long> data)
    {
        var node = (int)data[_ndIdxIndex];
        var depth = (int)data[_ndIdxDepth];

        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);

        _nodes.Set(node, _nodeIdxFc, _eleNodes.Insert());
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elements = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _eleNodes.Get(index, _enodeIdxNext);
                int elt = _eleNodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _eleNodes.Erase(index);

                // Insert element to the list.
                elements.Set(elements.PushBack(), 0, elt);
            }

            // Start by allocating 4 child nodes.
            int fc = _nodes.PushBackCount(_defaultNode4Values, 4);
            _nodes.Set(node, _nodeIdxFc, fc);

            // Transfer the elements in the former leaf node to its new children.
            _nodes.Set(node, _nodeIdxNum, -1);
            for (int j = 0; j < elements.InternalCount; ++j)
            {
                var id = elements.GetInt(j, 0);
                NodeInsert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }

    /// <summary>
    /// Disposes the quad tree.
    /// </summary>
    public void Dispose()
    {
        if(items == null)
            return;

        _eleNodes?.Dispose();
        _eleBounds?.Dispose();
        _nodes?.Dispose();
#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        items = null!;
    }
}
/// <summary>
/// Quadtree with 
/// </summary>
public class IntQuadTree<T> : IDisposable
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
    private readonly IntList _eleNodes;

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
    private readonly IntList _eleBounds;

    // ----------------------------------------------------------------------------------------
    // Node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the first child if this node is a branch or the first element
    // if this node is a leaf.
    const int _nodeIdxFc = 0;

    // Stores the number of elements in the node or -1 if it is not a leaf.
    const int _nodeIdxNum = 1;

    /// <summary>
    /// A static array of integers used as the default values for new child nodes in the quadtree. 
    /// These values are used when a leaf node in the quadtree is full and needs to be split into four child nodes.
    /// Each pair of -1 and 0 in the array represents the initial state of a child node, where -1 indicates that the node is empty and 0 indicates that the node has no elements.
    /// </summary>
    private static readonly int[] _defaultNode4Values = new[] { -1, 0, -1, 0, -1, 0, -1, 0, };

    // Stores all the nodes in the quadtree. The first node in this
    // sequence is always the root.
    private readonly IntList _nodes;

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
    private bool[]? _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T[]? items;

    private readonly int[] _rootNode;

    private readonly IntList.Cache _listCache = new IntList.Cache(_ndNum);

    private static readonly Action<T, int> _quadTreeIdSetter;
    /// <summary>
    /// Items contained in the quad tree.  The index of the items matches their QuadTreeId.
    /// </summary>
    public ReadOnlySpan<T> Items => new ReadOnlySpan<T>(items);

    /// <summary>
    /// Creates a quadtree with the requested extents, maximum elements per leaf,
    /// and maximum tree depth and maximum capacity.
    /// </summary>
    /// <param name="width">Width extents of the root node.</param>
    /// <param name="height">Height extents of the root node.</param>
    /// <param name="startMaxElements">
    /// Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    /// the leaf is at the maximum allowed tree depth.
    /// </param>
    /// <param name="startMaxDepth">Maximum depth allowed for the quadtree.</param>
    /// <param name="initialCapacity">Initial element capacity for the tree.</param>
    public IntQuadTree(int width, int height, int startMaxElements, int startMaxDepth, int initialCapacity = 128)
    {
        _maxElements = startMaxElements;
        _maxDepth = startMaxDepth;
        
        _eleNodes = new IntList(2, 2 * initialCapacity);
        _nodes = new IntList(2, 2 * initialCapacity);
        _eleBounds = new IntList(_eleBoundsItems, _eleBoundsItems * initialCapacity);
        items = new T[initialCapacity];

        // Insert the root node to the qt.
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);

        // Set the extents of the root node.
        _rootNode = new[]
        {
            width / 2, // _ndIdxMx
            height / 2, // _ndIdxMy
            width / 2, // _ndIdxSx
            height / 2, // _ndIdxSy
            0, // _ndIdxIndex
            0 // _ndIdxDepth
        };
    }

    static IntQuadTree()
    {
        // Implemented interface.
        var property = typeof(T).GetProperty("QuadTreeId", 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (property == null)
        {
            // Explicit interface implementation
            property = typeof(T).GetProperty("DtronixCommon.Collections.Trees.IQuadTreeItem.QuadTreeId",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        if (property == null)
            throw new Exception(
                $"Type {typeof(T).FullName} does not contain a integer property named QuadTreeId as required.");

        _quadTreeIdSetter = property.GetBackingField().CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an element into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="element">Item to insert into the quad tree.</param>
    /// <returns>Index of the new element. -1 if the element exists in the quad tree.</returns>
    public int Insert(int x1, int y1, int x2, int y2, T element)
    {
        if (element.QuadTreeId != -1)
            return -1;

        ReadOnlySpan<int> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items!.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = element;

        // Insert the element to the appropriate leaf node(s).
        NodeInsert(new ReadOnlySpan<int>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(element, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
    public void Remove(T element)
    {
        var id = element.QuadTreeId;
        // Find the leaves.
        var leaves = FindLeaves(
            new ReadOnlySpan<int>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _eleNodes.Get(nodeIndex, _enodeIdxElt) != id)
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
        leaves.Return();
        // Remove the element.
        _eleBounds.Erase(id);
        items![id] = default!;
        _quadTreeIdSetter(element, -1);
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

        while (toProcess.InternalCount > 0)
        {
            // Pop a node from the stack.
            int node = (int)toProcess.Get(toProcess.InternalCount - 1, 0);
            int fc = _nodes.Get(node, _nodeIdxFc);
            int numEmptyLeaves = 0;
            toProcess.InternalCount--;

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

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <returns>List of items which intersect the bounds.</returns>
    public List<T> Query(
        int x1,
        int y1,
        int x2, 
        int y2)
    {
        var listOut = new List<T>();
        ReadOnlySpan<int> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<int>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp![element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items![element]);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp![listOut[j].QuadTreeId] = false;

        return listOut;
    }

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    /// <returns>List of items which intersect the bounds.</returns>
      public IntList Query(
        int x1,
        int y1,
        int x2,
        int y2,
        Func<T, bool> callback)
    {
        var intListOut = new IntList(1);
        ReadOnlySpan<int> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<int>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp![element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp![intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found element.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    public unsafe void Walk(
        int x1,
        int y1,
        int x2,
        int y2,
        Func<T, bool> callback)
    {
        ReadOnlySpan<int> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<int>(_rootNode), bounds);

        bool cancel = false;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();
    }

    /// <summary>
    /// Clears the quad tree for use.
    /// </summary>
    public void Clear()
    {
        _eleNodes.Clear();
        _nodes.Clear();
        _eleBounds.Clear();

#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(
        ReadOnlySpan<int> b1,
        ReadOnlySpan<int> b2)
    {
        return b2[_eltIdxLft] <= b1[_eltIdxRgt] 
               && b2[_eltIdxRgt] >= b1[_eltIdxLft]
               && b2[_eltIdxTop] <= b1[_eltIdxBtm]
               && b2[_eltIdxBtm] >= b1[_eltIdxTop];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNode(IntList nodes, int ndIndex, int ndDepth, int ndMx, int ndMy, int ndSx, int ndSy)
    {
        nodes.PushBack(stackalloc[] { ndMx, ndMy, ndSx, ndSy, ndIndex, ndDepth });
    }
    private IntList.Cache.Item FindLeaves(
        ReadOnlySpan<int> data,
        ReadOnlySpan<int> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        var toProcessList = toProcess.List;
        var toProcessListData = toProcessList.Data!;

        toProcessList.PushBack(data);

        while (toProcessList.InternalCount > 0)
        {
            int backIdx = toProcessList.InternalCount - 1;
            int backOffset = backIdx * 6;
            //var ndData = toProcessList.Get(backIdx, 0, 6);
            //var ndIndex = (int)ndData[_ndIdxIndex];
            //var ndDepth = (int)ndData[_ndIdxDepth];

            var ndIndexOffset = (int)toProcessListData[backOffset + _ndIdxIndex] * 2;
            var ndDepth = (int)toProcessListData[backOffset + _ndIdxDepth];
            toProcessList.InternalCount--;

            // If this node is a leaf, insert it to the list.
            
            if (_nodes.Data![ndIndexOffset + _nodeIdxNum] != -1)
            {
                leaves.List.PushBack(toProcessList.Get(backIdx, 0, 6));
            }
            else
            {
                var mx = toProcessListData[backOffset + _ndIdxMx];
                var my = toProcessListData[backOffset + _ndIdxMy];
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Data[ndIndexOffset + _nodeIdxFc]; //_nodes.Get(ndIndex, _nodeIdxFc);
                var hx = toProcessListData[backOffset + _ndIdxSx] / 2;
                var hy = toProcessListData[backOffset + _ndIdxSy] / 2;
                var l = mx - hx;
                var r = mx + hx;

                var offset = toProcessList.InternalCount;
                toProcessList.EnsureSpaceAvailable(4);

                if (bounds[_eltIdxTop] <= my)
                {
                    var t = my - hx;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 0; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                        //toProcessList.PushBack(processItem);
                        //toProcessList.PushBack(stackalloc[] { l, t, hx, hy, fc + 0, ndDepth + 1 });
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 1; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                if (bounds[_eltIdxBtm] > my)
                {
                    var b = my + hy;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 2; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 3; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                toProcessList.InternalCount = offset;
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void NodeInsert(ReadOnlySpan<int> data, ReadOnlySpan<int> elementBounds, int elementId)
    {
        var leaves = FindLeaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            LeafInsert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void LeafInsert(int element, ReadOnlySpan<int> data)
    {
        var node = (int)data[_ndIdxIndex];
        var depth = (int)data[_ndIdxDepth];

        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);

        _nodes.Set(node, _nodeIdxFc, _eleNodes.Insert());
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elements = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _eleNodes.Get(index, _enodeIdxNext);
                int elt = _eleNodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _eleNodes.Erase(index);

                // Insert element to the list.
                elements.Set(elements.PushBack(), 0, elt);
            }

            // Start by allocating 4 child nodes.
            int fc = _nodes.PushBackCount(_defaultNode4Values, 4);
            _nodes.Set(node, _nodeIdxFc, fc);

            // Transfer the elements in the former leaf node to its new children.
            _nodes.Set(node, _nodeIdxNum, -1);
            for (int j = 0; j < elements.InternalCount; ++j)
            {
                var id = elements.GetInt(j, 0);
                NodeInsert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }

    /// <summary>
    /// Disposes the quad tree.
    /// </summary>
    public void Dispose()
    {
        if(items == null)
            return;

        _eleNodes?.Dispose();
        _eleBounds?.Dispose();
        _nodes?.Dispose();
#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        items = null!;
    }
}
/// <summary>
/// Quadtree with 
/// </summary>
public class DoubleQuadTree<T> : IDisposable
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
    private readonly IntList _eleNodes;

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
    private readonly DoubleList _eleBounds;

    // ----------------------------------------------------------------------------------------
    // Node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the first child if this node is a branch or the first element
    // if this node is a leaf.
    const int _nodeIdxFc = 0;

    // Stores the number of elements in the node or -1 if it is not a leaf.
    const int _nodeIdxNum = 1;

    /// <summary>
    /// A static array of integers used as the default values for new child nodes in the quadtree. 
    /// These values are used when a leaf node in the quadtree is full and needs to be split into four child nodes.
    /// Each pair of -1 and 0 in the array represents the initial state of a child node, where -1 indicates that the node is empty and 0 indicates that the node has no elements.
    /// </summary>
    private static readonly int[] _defaultNode4Values = new[] { -1, 0, -1, 0, -1, 0, -1, 0, };

    // Stores all the nodes in the quadtree. The first node in this
    // sequence is always the root.
    private readonly IntList _nodes;

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
    private bool[]? _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T[]? items;

    private readonly double[] _rootNode;

    private readonly DoubleList.Cache _listCache = new DoubleList.Cache(_ndNum);

    private static readonly Action<T, int> _quadTreeIdSetter;
    /// <summary>
    /// Items contained in the quad tree.  The index of the items matches their QuadTreeId.
    /// </summary>
    public ReadOnlySpan<T> Items => new ReadOnlySpan<T>(items);

    /// <summary>
    /// Creates a quadtree with the requested extents, maximum elements per leaf,
    /// and maximum tree depth and maximum capacity.
    /// </summary>
    /// <param name="width">Width extents of the root node.</param>
    /// <param name="height">Height extents of the root node.</param>
    /// <param name="startMaxElements">
    /// Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    /// the leaf is at the maximum allowed tree depth.
    /// </param>
    /// <param name="startMaxDepth">Maximum depth allowed for the quadtree.</param>
    /// <param name="initialCapacity">Initial element capacity for the tree.</param>
    public DoubleQuadTree(double width, double height, int startMaxElements, int startMaxDepth, int initialCapacity = 128)
    {
        _maxElements = startMaxElements;
        _maxDepth = startMaxDepth;
        
        _eleNodes = new IntList(2, 2 * initialCapacity);
        _nodes = new IntList(2, 2 * initialCapacity);
        _eleBounds = new DoubleList(_eleBoundsItems, _eleBoundsItems * initialCapacity);
        items = new T[initialCapacity];

        // Insert the root node to the qt.
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);

        // Set the extents of the root node.
        _rootNode = new[]
        {
            width / 2, // _ndIdxMx
            height / 2, // _ndIdxMy
            width / 2, // _ndIdxSx
            height / 2, // _ndIdxSy
            0, // _ndIdxIndex
            0 // _ndIdxDepth
        };
    }

    static DoubleQuadTree()
    {
        // Implemented interface.
        var property = typeof(T).GetProperty("QuadTreeId", 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (property == null)
        {
            // Explicit interface implementation
            property = typeof(T).GetProperty("DtronixCommon.Collections.Trees.IQuadTreeItem.QuadTreeId",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        if (property == null)
            throw new Exception(
                $"Type {typeof(T).FullName} does not contain a integer property named QuadTreeId as required.");

        _quadTreeIdSetter = property.GetBackingField().CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an element into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="element">Item to insert into the quad tree.</param>
    /// <returns>Index of the new element. -1 if the element exists in the quad tree.</returns>
    public int Insert(double x1, double y1, double x2, double y2, T element)
    {
        if (element.QuadTreeId != -1)
            return -1;

        ReadOnlySpan<double> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items!.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = element;

        // Insert the element to the appropriate leaf node(s).
        NodeInsert(new ReadOnlySpan<double>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(element, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
    public void Remove(T element)
    {
        var id = element.QuadTreeId;
        // Find the leaves.
        var leaves = FindLeaves(
            new ReadOnlySpan<double>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _eleNodes.Get(nodeIndex, _enodeIdxElt) != id)
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
        leaves.Return();
        // Remove the element.
        _eleBounds.Erase(id);
        items![id] = default!;
        _quadTreeIdSetter(element, -1);
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

        while (toProcess.InternalCount > 0)
        {
            // Pop a node from the stack.
            int node = (int)toProcess.Get(toProcess.InternalCount - 1, 0);
            int fc = _nodes.Get(node, _nodeIdxFc);
            int numEmptyLeaves = 0;
            toProcess.InternalCount--;

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

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <returns>List of items which intersect the bounds.</returns>
    public List<T> Query(
        double x1,
        double y1,
        double x2, 
        double y2)
    {
        var listOut = new List<T>();
        ReadOnlySpan<double> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<double>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp![element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items![element]);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp![listOut[j].QuadTreeId] = false;

        return listOut;
    }

    /// <summary>
    /// Queries the QuadTree and returns a list of the items which intersect the passed bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    /// <returns>List of items which intersect the bounds.</returns>
      public IntList Query(
        double x1,
        double y1,
        double x2,
        double y2,
        Func<T, bool> callback)
    {
        var intListOut = new IntList(1);
        ReadOnlySpan<double> bounds = stackalloc[] { x1, y1, x2, y2 };

        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<double>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp![element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp![intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found element.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    public unsafe void Walk(
        double x1,
        double y1,
        double x2,
        double y2,
        Func<T, bool> callback)
    {
        ReadOnlySpan<double> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<double>(_rootNode), bounds);

        bool cancel = false;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if (cancel)
                break;
        }

        leaves.Return();
    }

    /// <summary>
    /// Clears the quad tree for use.
    /// </summary>
    public void Clear()
    {
        _eleNodes.Clear();
        _nodes.Clear();
        _eleBounds.Clear();

#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(
        ReadOnlySpan<double> b1,
        ReadOnlySpan<double> b2)
    {
        return b2[_eltIdxLft] <= b1[_eltIdxRgt] 
               && b2[_eltIdxRgt] >= b1[_eltIdxLft]
               && b2[_eltIdxTop] <= b1[_eltIdxBtm]
               && b2[_eltIdxBtm] >= b1[_eltIdxTop];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNode(DoubleList nodes, int ndIndex, int ndDepth, double ndMx, double ndMy, double ndSx, double ndSy)
    {
        nodes.PushBack(stackalloc[] { ndMx, ndMy, ndSx, ndSy, ndIndex, ndDepth });
    }
    private DoubleList.Cache.Item FindLeaves(
        ReadOnlySpan<double> data,
        ReadOnlySpan<double> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        var toProcessList = toProcess.List;
        var toProcessListData = toProcessList.Data!;

        toProcessList.PushBack(data);

        while (toProcessList.InternalCount > 0)
        {
            int backIdx = toProcessList.InternalCount - 1;
            int backOffset = backIdx * 6;
            //var ndData = toProcessList.Get(backIdx, 0, 6);
            //var ndIndex = (int)ndData[_ndIdxIndex];
            //var ndDepth = (int)ndData[_ndIdxDepth];

            var ndIndexOffset = (int)toProcessListData[backOffset + _ndIdxIndex] * 2;
            var ndDepth = (int)toProcessListData[backOffset + _ndIdxDepth];
            toProcessList.InternalCount--;

            // If this node is a leaf, insert it to the list.
            
            if (_nodes.Data![ndIndexOffset + _nodeIdxNum] != -1)
            {
                leaves.List.PushBack(toProcessList.Get(backIdx, 0, 6));
            }
            else
            {
                var mx = toProcessListData[backOffset + _ndIdxMx];
                var my = toProcessListData[backOffset + _ndIdxMy];
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Data[ndIndexOffset + _nodeIdxFc]; //_nodes.Get(ndIndex, _nodeIdxFc);
                var hx = toProcessListData[backOffset + _ndIdxSx] / 2;
                var hy = toProcessListData[backOffset + _ndIdxSy] / 2;
                var l = mx - hx;
                var r = mx + hx;

                var offset = toProcessList.InternalCount;
                toProcessList.EnsureSpaceAvailable(4);

                if (bounds[_eltIdxTop] <= my)
                {
                    var t = my - hx;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 0; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                        //toProcessList.PushBack(processItem);
                        //toProcessList.PushBack(stackalloc[] { l, t, hx, hy, fc + 0, ndDepth + 1 });
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 1; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                if (bounds[_eltIdxBtm] > my)
                {
                    var b = my + hy;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 2; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _ndIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _ndIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _ndIdxSx] = hx; // ndSx
                        toProcessListData[thisOffset + _ndIdxSy] = hy; // ndSy
                        toProcessListData[thisOffset + _ndIdxIndex] = fc + 3; // ndIndex
                        toProcessListData[thisOffset + _ndIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                toProcessList.InternalCount = offset;
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void NodeInsert(ReadOnlySpan<double> data, ReadOnlySpan<double> elementBounds, int elementId)
    {
        var leaves = FindLeaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            LeafInsert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void LeafInsert(int element, ReadOnlySpan<double> data)
    {
        var node = (int)data[_ndIdxIndex];
        var depth = (int)data[_ndIdxDepth];

        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);

        _nodes.Set(node, _nodeIdxFc, _eleNodes.Insert());
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxNext, ndFc);
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _enodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elements = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _eleNodes.Get(index, _enodeIdxNext);
                int elt = _eleNodes.Get(index, _enodeIdxElt);

                // Pop off the element node from the leaf and remove it from the qt.
                _nodes.Set(node, _nodeIdxFc, nextIndex);
                _eleNodes.Erase(index);

                // Insert element to the list.
                elements.Set(elements.PushBack(), 0, elt);
            }

            // Start by allocating 4 child nodes.
            int fc = _nodes.PushBackCount(_defaultNode4Values, 4);
            _nodes.Set(node, _nodeIdxFc, fc);

            // Transfer the elements in the former leaf node to its new children.
            _nodes.Set(node, _nodeIdxNum, -1);
            for (int j = 0; j < elements.InternalCount; ++j)
            {
                var id = elements.GetInt(j, 0);
                NodeInsert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }

    /// <summary>
    /// Disposes the quad tree.
    /// </summary>
    public void Dispose()
    {
        if(items == null)
            return;

        _eleNodes?.Dispose();
        _eleBounds?.Dispose();
        _nodes?.Dispose();
#if NET6_0_OR_GREATER
        Array.Clear(items!);
#else
        Array.Clear(items!, 0, items.Length);
#endif
        items = null!;
    }
}
