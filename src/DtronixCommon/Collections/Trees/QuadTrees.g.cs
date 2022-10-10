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
public class FloatQuadTree<T>
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
    static int _nodeIdxNum = 1;

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
    private bool[] _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T?[] items;

    private readonly float[] _rootNode;

    private readonly FloatList.Cache _listCache = new FloatList.Cache(_ndNum);

    private static readonly Func<T, int> _quadTreeIdGetter;
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
    /// <param name="initialCapacity">Initial item capacity for the tree.</param>
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
                $"Type {typeof(T).FullName} does not contain a interger property named QuadTreeId as required.");

        var backingField = property.GetBackingField();

        _quadTreeIdGetter = backingField.CreateGetter<T, int>();
        _quadTreeIdSetter = backingField.CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an item into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="item">Item to insert into the quad tree.</param>
    /// <returns>Index of the new item. -1 if the item exists in the quad tree.</returns>
    public int Insert(float x1, float y1, float x2, float y2, T item)
    {
        if (_quadTreeIdGetter(item) != -1)
            return -1;

        ReadOnlySpan<float> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = item;

        // Insert the element to the appropriate leaf node(s).
        node_insert(new ReadOnlySpan<float>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(item, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
        public void Remove(T element)
    {
        var id = _quadTreeIdGetter(element);
        // Find the leaves.
        var leaves = find_leaves(
            new ReadOnlySpan<float>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        int nodeIndex;
        int ndIndex;

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
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
        items[id] = default;
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
        var leaves = find_leaves(new ReadOnlySpan<float>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        int ndIndex;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp[element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items[element]!);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp[_quadTreeIdGetter(listOut[j])] = false;

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
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<float>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp[intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found item.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<float>(_rootNode), bounds);

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
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
        Array.Clear(items, 0, items.Length);

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
    private FloatList.Cache.Item find_leaves(
        ReadOnlySpan<float> data,
        ReadOnlySpan<float> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        toProcess.List.PushBack(data);

        while (toProcess.List.InternalCount > 0)
        {
            int backIdx = toProcess.List.InternalCount - 1;
            var ndData = toProcess.List.Get(backIdx, 0, 6);

            var ndIndex = (int)ndData[_ndIdxIndex];
            var ndDepth = (int)ndData[_ndIdxDepth];
            toProcess.List.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                leaves.List.PushBack(ndData);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Get(ndIndex, _nodeIdxFc);
                var hx = ndData[_ndIdxSx] / 2;
                var hy = ndData[_ndIdxSy] / 2;
                var l = ndData[_ndIdxMx] - hx;
                var t = ndData[_ndIdxMy] - hx;
                var r = ndData[_ndIdxMx] + hx;
                var b = ndData[_ndIdxMy] + hy;

                if (bounds[_eltIdxTop] <= ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 0, ndDepth + 1, l, t, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 1, ndDepth + 1, r, t, hx, hy);
                }

                if (bounds[_eltIdxBtm] > ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 2, ndDepth + 1, l, b, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 3, ndDepth + 1, r, b, hx, hy);
                }
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void node_insert(ReadOnlySpan<float> data, ReadOnlySpan<float> elementBounds, int elementId)
    {
        var leaves = find_leaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            leaf_insert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void leaf_insert(int element, ReadOnlySpan<float> data)
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
            for (int j = 0; j < elts.InternalCount; ++j)
            {
                var id = elts.GetInt(j, 0);
                node_insert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }
}
/// <summary>
/// Quadtree with 
/// </summary>
public class LongQuadTree<T>
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
    static int _nodeIdxNum = 1;

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
    private bool[] _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T?[] items;

    private readonly long[] _rootNode;

    private readonly LongList.Cache _listCache = new LongList.Cache(_ndNum);

    private static readonly Func<T, int> _quadTreeIdGetter;
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
    /// <param name="initialCapacity">Initial item capacity for the tree.</param>
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
                $"Type {typeof(T).FullName} does not contain a interger property named QuadTreeId as required.");

        var backingField = property.GetBackingField();

        _quadTreeIdGetter = backingField.CreateGetter<T, int>();
        _quadTreeIdSetter = backingField.CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an item into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="item">Item to insert into the quad tree.</param>
    /// <returns>Index of the new item. -1 if the item exists in the quad tree.</returns>
    public int Insert(long x1, long y1, long x2, long y2, T item)
    {
        if (_quadTreeIdGetter(item) != -1)
            return -1;

        ReadOnlySpan<long> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = item;

        // Insert the element to the appropriate leaf node(s).
        node_insert(new ReadOnlySpan<long>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(item, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
        public void Remove(T element)
    {
        var id = _quadTreeIdGetter(element);
        // Find the leaves.
        var leaves = find_leaves(
            new ReadOnlySpan<long>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        int nodeIndex;
        int ndIndex;

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
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
        items[id] = default;
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
        var leaves = find_leaves(new ReadOnlySpan<long>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        int ndIndex;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp[element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items[element]!);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp[_quadTreeIdGetter(listOut[j])] = false;

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
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<long>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp[intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found item.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<long>(_rootNode), bounds);

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
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
        Array.Clear(items, 0, items.Length);

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
    private LongList.Cache.Item find_leaves(
        ReadOnlySpan<long> data,
        ReadOnlySpan<long> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        toProcess.List.PushBack(data);

        while (toProcess.List.InternalCount > 0)
        {
            int backIdx = toProcess.List.InternalCount - 1;
            var ndData = toProcess.List.Get(backIdx, 0, 6);

            var ndIndex = (int)ndData[_ndIdxIndex];
            var ndDepth = (int)ndData[_ndIdxDepth];
            toProcess.List.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                leaves.List.PushBack(ndData);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Get(ndIndex, _nodeIdxFc);
                var hx = ndData[_ndIdxSx] / 2;
                var hy = ndData[_ndIdxSy] / 2;
                var l = ndData[_ndIdxMx] - hx;
                var t = ndData[_ndIdxMy] - hx;
                var r = ndData[_ndIdxMx] + hx;
                var b = ndData[_ndIdxMy] + hy;

                if (bounds[_eltIdxTop] <= ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 0, ndDepth + 1, l, t, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 1, ndDepth + 1, r, t, hx, hy);
                }

                if (bounds[_eltIdxBtm] > ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 2, ndDepth + 1, l, b, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 3, ndDepth + 1, r, b, hx, hy);
                }
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void node_insert(ReadOnlySpan<long> data, ReadOnlySpan<long> elementBounds, int elementId)
    {
        var leaves = find_leaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            leaf_insert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void leaf_insert(int element, ReadOnlySpan<long> data)
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
            for (int j = 0; j < elts.InternalCount; ++j)
            {
                var id = elts.GetInt(j, 0);
                node_insert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }
}
/// <summary>
/// Quadtree with 
/// </summary>
public class IntQuadTree<T>
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
    static int _nodeIdxNum = 1;

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
    private bool[] _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T?[] items;

    private readonly int[] _rootNode;

    private readonly IntList.Cache _listCache = new IntList.Cache(_ndNum);

    private static readonly Func<T, int> _quadTreeIdGetter;
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
    /// <param name="initialCapacity">Initial item capacity for the tree.</param>
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
                $"Type {typeof(T).FullName} does not contain a interger property named QuadTreeId as required.");

        var backingField = property.GetBackingField();

        _quadTreeIdGetter = backingField.CreateGetter<T, int>();
        _quadTreeIdSetter = backingField.CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an item into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="item">Item to insert into the quad tree.</param>
    /// <returns>Index of the new item. -1 if the item exists in the quad tree.</returns>
    public int Insert(int x1, int y1, int x2, int y2, T item)
    {
        if (_quadTreeIdGetter(item) != -1)
            return -1;

        ReadOnlySpan<int> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = item;

        // Insert the element to the appropriate leaf node(s).
        node_insert(new ReadOnlySpan<int>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(item, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
        public void Remove(T element)
    {
        var id = _quadTreeIdGetter(element);
        // Find the leaves.
        var leaves = find_leaves(
            new ReadOnlySpan<int>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        int nodeIndex;
        int ndIndex;

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
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
        items[id] = default;
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
        var leaves = find_leaves(new ReadOnlySpan<int>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        int ndIndex;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp[element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items[element]!);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp[_quadTreeIdGetter(listOut[j])] = false;

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
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<int>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp[intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found item.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<int>(_rootNode), bounds);

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
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
        Array.Clear(items, 0, items.Length);

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
    private IntList.Cache.Item find_leaves(
        ReadOnlySpan<int> data,
        ReadOnlySpan<int> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        toProcess.List.PushBack(data);

        while (toProcess.List.InternalCount > 0)
        {
            int backIdx = toProcess.List.InternalCount - 1;
            var ndData = toProcess.List.Get(backIdx, 0, 6);

            var ndIndex = (int)ndData[_ndIdxIndex];
            var ndDepth = (int)ndData[_ndIdxDepth];
            toProcess.List.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                leaves.List.PushBack(ndData);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Get(ndIndex, _nodeIdxFc);
                var hx = ndData[_ndIdxSx] / 2;
                var hy = ndData[_ndIdxSy] / 2;
                var l = ndData[_ndIdxMx] - hx;
                var t = ndData[_ndIdxMy] - hx;
                var r = ndData[_ndIdxMx] + hx;
                var b = ndData[_ndIdxMy] + hy;

                if (bounds[_eltIdxTop] <= ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 0, ndDepth + 1, l, t, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 1, ndDepth + 1, r, t, hx, hy);
                }

                if (bounds[_eltIdxBtm] > ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 2, ndDepth + 1, l, b, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 3, ndDepth + 1, r, b, hx, hy);
                }
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void node_insert(ReadOnlySpan<int> data, ReadOnlySpan<int> elementBounds, int elementId)
    {
        var leaves = find_leaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            leaf_insert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void leaf_insert(int element, ReadOnlySpan<int> data)
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
            for (int j = 0; j < elts.InternalCount; ++j)
            {
                var id = elts.GetInt(j, 0);
                node_insert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }
}
/// <summary>
/// Quadtree with 
/// </summary>
public class DoubleQuadTree<T>
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
    static int _nodeIdxNum = 1;

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
    private bool[] _temp;

    // Stores the size of the temporary buffer.
    private int _tempSize = 0;

    // Maximum allowed elements in a leaf before the leaf is subdivided/split unless
    // the leaf is at the maximum allowed tree depth.
    private int _maxElements;

    // Stores the maximum depth allowed for the quadtree.
    private int _maxDepth;

    private T?[] items;

    private readonly double[] _rootNode;

    private readonly DoubleList.Cache _listCache = new DoubleList.Cache(_ndNum);

    private static readonly Func<T, int> _quadTreeIdGetter;
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
    /// <param name="initialCapacity">Initial item capacity for the tree.</param>
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
                $"Type {typeof(T).FullName} does not contain a interger property named QuadTreeId as required.");

        var backingField = property.GetBackingField();

        _quadTreeIdGetter = backingField.CreateGetter<T, int>();
        _quadTreeIdSetter = backingField.CreateSetter<T, int>();
    }

    /// <summary>
    /// Inserts an item into the quad tree at with the specified bounds.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="item">Item to insert into the quad tree.</param>
    /// <returns>Index of the new item. -1 if the item exists in the quad tree.</returns>
    public int Insert(double x1, double y1, double x2, double y2, T item)
    {
        if (_quadTreeIdGetter(item) != -1)
            return -1;

        ReadOnlySpan<double> bounds = stackalloc[] { x1, y1, x2, y2 };
        // Insert a new element.                   
        var newElement = _eleBounds.Insert(bounds);  
                                                   
        if (newElement == items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[newElement] = item;

        // Insert the element to the appropriate leaf node(s).
        node_insert(new ReadOnlySpan<double>(_rootNode), bounds, newElement);
         _quadTreeIdSetter(item, newElement);
        return newElement;
    }

    /// <summary>
    /// Removes the specified element from the tree.
    /// </summary>
    /// <param name="element">Element to remove.</param>
        public void Remove(T element)
    {
        var id = _quadTreeIdGetter(element);
        // Find the leaves.
        var leaves = find_leaves(
            new ReadOnlySpan<double>(_rootNode),
            _eleBounds.Get(id, 0, 4));

        int nodeIndex;
        int ndIndex;

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list until we find the element node.
            nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
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
        items[id] = default;
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
        var leaves = find_leaves(new ReadOnlySpan<double>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        int ndIndex;

        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (!_temp[element] && Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    listOut.Add(items[element]!);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }
        }


        leaves.Return();
        // Unmark the elements that were inserted.
        for (int j = 0; j < listOut.Count; j++)
            _temp[_quadTreeIdGetter(listOut[j])] = false;

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
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<double>(_rootNode), bounds);

        if (_tempSize < _eleBounds.InternalCount)
        {
            _tempSize = _eleBounds.InternalCount;
            _temp = new bool[_tempSize];
        }

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
                break;
        }

        leaves.Return();

        // Unmark the elements that were inserted.
        for (int j = 0; j < intListOut.InternalCount; ++j)
            _temp[intListOut.Get(j, 0)] = false;

        return intListOut;
    }

         /// <summary>
    /// Walks the specified bounds of the QuadTree and invokes the callback on each found item.
    /// </summary>
    /// <param name="x1">Min X</param>
    /// <param name="y1">Min Y</param>
    /// <param name="x2">Max X</param>
    /// <param name="y2">Max Y</param>
    /// <param name="callback">
    /// Callback which is invoked on each found item.
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
        var leaves = find_leaves(new ReadOnlySpan<double>(_rootNode), bounds);

        bool cancel = false;
        int ndIndex;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            ndIndex = leaves.List.GetInt(j, _ndIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _enodeIdxElt);
                if (Intersect(bounds, _eleBounds.Get(element, 0, 4)))
                {
                    cancel = !callback.Invoke(items[element]!);
                    if(cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _enodeIdxNext);
            }

            if(cancel)
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
        Array.Clear(items, 0, items.Length);

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
    private DoubleList.Cache.Item find_leaves(
        ReadOnlySpan<double> data,
        ReadOnlySpan<double> bounds)
    {
        var leaves = _listCache.Get();
        var toProcess = _listCache.Get();
        toProcess.List.PushBack(data);

        while (toProcess.List.InternalCount > 0)
        {
            int backIdx = toProcess.List.InternalCount - 1;
            var ndData = toProcess.List.Get(backIdx, 0, 6);

            var ndIndex = (int)ndData[_ndIdxIndex];
            var ndDepth = (int)ndData[_ndIdxDepth];
            toProcess.List.PopBack();

            // If this node is a leaf, insert it to the list.
            if (_nodes.Get(ndIndex, _nodeIdxNum) != -1)
                leaves.List.PushBack(ndData);
            else
            {
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Get(ndIndex, _nodeIdxFc);
                var hx = ndData[_ndIdxSx] / 2;
                var hy = ndData[_ndIdxSy] / 2;
                var l = ndData[_ndIdxMx] - hx;
                var t = ndData[_ndIdxMy] - hx;
                var r = ndData[_ndIdxMx] + hx;
                var b = ndData[_ndIdxMy] + hy;

                if (bounds[_eltIdxTop] <= ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 0, ndDepth + 1, l, t, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 1, ndDepth + 1, r, t, hx, hy);
                }

                if (bounds[_eltIdxBtm] > ndData[_ndIdxMy])
                {
                    if (bounds[_eltIdxLft] <= ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 2, ndDepth + 1, l, b, hx, hy);
                    if (bounds[_eltIdxRgt] > ndData[_ndIdxMx])
                        PushNode(toProcess.List, fc + 3, ndDepth + 1, r, b, hx, hy);
                }
            }
        }

        toProcess.Return();

        return leaves;
    }

    private void node_insert(ReadOnlySpan<double> data, ReadOnlySpan<double> elementBounds, int elementId)
    {
        var leaves = find_leaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            leaf_insert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void leaf_insert(int element, ReadOnlySpan<double> data)
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
            for (int j = 0; j < elts.InternalCount; ++j)
            {
                var id = elts.GetInt(j, 0);
                node_insert(data, _eleBounds.Get(id, 0, 4), id);
            }
        }
        else
        {
            // Increment the leaf element count.
            _nodes.Increment(node, _nodeIdxNum);
        }
    }
}
