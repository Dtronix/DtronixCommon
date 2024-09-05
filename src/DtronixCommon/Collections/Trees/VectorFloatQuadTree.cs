using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using DtronixCommon.Collections.Lists;
using DtronixCommon.Reflection;

namespace DtronixCommon.Collections.Trees;
/// <summary>
/// Quadtree with 
/// </summary>
public class VectorFloatQuadTree<T> : IDisposable
    where T : IQuadTreeItem
{
    // ----------------------------------------------------------------------------------------
    // Element node fields:
    // ----------------------------------------------------------------------------------------
    // Points to the next element in the leaf node. A value of -1 
    // indicates the end of the list.
    const int _eleNodeIdxNext = 0;

    // Stores the element index.
    const int _leNodeIdxElt = 1;

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

    // Stores all the elements in the quadtree.
    private readonly VectorFloatList _eleBounds;

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
    const int _leafNodeIdxNum = 6;

    // Stores the extents of the node using a centered rectangle and half-size.
    const int _leafNodeIdxMx = 0;
    const int _leafNodeIdxMy = 1;
    const int _leafNodeIdxHalfWidth = 2;
    const int _leafNodeIdxHalfHeight = 3;

    // Stores the index of the node.
    const int _leafNodeIdxIndex = 4;

    // Stores the depth of the node.
    const int _leafNodeIdxDepth = 5;

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

    private readonly FloatList.Cache _leafNodeListCache = new FloatList.Cache(_leafNodeIdxNum);

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
    public VectorFloatQuadTree(float width, float height, int startMaxElements, int startMaxDepth, int initialCapacity = 128)
    {
        _maxElements = startMaxElements;
        _maxDepth = startMaxDepth;

        _eleNodes = new IntList(2, 2 * initialCapacity);
        _nodes = new IntList(2, 2 * initialCapacity);
        _eleBounds = new VectorFloatList(initialCapacity);
        items = new T[initialCapacity];

        // Insert the root node to the qt.
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);

        // Set the extents of the root node.
        _rootNode = new[]
        {
            width / 2, // _leafNodeIdxMx
            height / 2, // _leafNodeIdxMy
            width / 2, // _leafNodeIdxHalfWidth
            height / 2, // _leafNodeIdxHalfHeight
            0, // _leafNodeIdxIndex
            0 // _leafNodeIdxDepth
        };
    }

    static VectorFloatQuadTree()
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
    /// <param name="element">Item to insert into the quad tree.</param>
    /// <returns>Index of the new element. -1 if the element exists in the quad tree.</returns>
    public int Insert(in Vector128<float> bounds, T element)
    {
        if (element.QuadTreeId != -1)
            return -1;

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
        var leaves = FindLeaves(new ReadOnlySpan<float>(_rootNode), _eleBounds.Data[id]);

        // For each leaf node, remove the element node.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _leafNodeIdxIndex);

            // Walk the list until we find the element node.
            var nodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int prevIndex = -1;
            while (nodeIndex != -1 && _eleNodes.Get(nodeIndex, _leNodeIdxElt) != id)
            {
                prevIndex = nodeIndex;
                nodeIndex = _eleNodes.Get(nodeIndex, _eleNodeIdxNext);
            }

            if (nodeIndex != -1)
            {
                // Remove the element node.
                var nextIndex = _eleNodes.Get(nodeIndex, _eleNodeIdxNext);
                if (prevIndex == -1)
                    _nodes.Set(ndIndex, _nodeIdxFc, nextIndex);
                else
                    _eleNodes.Set(prevIndex, _eleNodeIdxNext, nextIndex);
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
    /// <returns>List of items which intersect the bounds.</returns>
    public List<T> Query(in Vector128<float> bounds)
    {
        var listOut = new List<T>();

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
            var ndIndex = leaves.List.GetInt(j, _leafNodeIdxIndex);
            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _leNodeIdxElt);
                if (!_temp![element] && IntersectVector(bounds, _eleBounds.Data[element]))
                {
                    listOut.Add(items![element]);
                    _temp[element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _eleNodeIdxNext);
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
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    /// <returns>List of items which intersect the bounds.</returns>
    public IntList Query(in Vector128<float> bounds, Func<T, bool> callback)
    {
        var intListOut = new IntList(1);

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
            var ndIndex = leaves.List.GetInt(j, _leafNodeIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            while (eltNodeIndex != -1)
            {
                int element = _eleNodes.Get(eltNodeIndex, _leNodeIdxElt);
                if (IntersectVector(bounds, _eleBounds.Data[element]))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                    intListOut.Set(intListOut.PushBack(), 0, element);
                    _temp![element] = true;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _eleNodeIdxNext);
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
    /// <param name="callback">
    /// Callback which is invoked on each found element.
    /// Return true to continue searching, false to stop.
    /// </param>
    public void Walk(in Vector128<float> bounds, Func<T, bool> callback)
    {
        // Find the leaves that intersect the specified query rectangle.
        var leaves = FindLeaves(new ReadOnlySpan<float>(_rootNode), bounds);

        bool cancel = false;
        // For each leaf node, look for elements that intersect.
        for (int j = 0; j < leaves.List.InternalCount; ++j)
        {
            var ndIndex = leaves.List.GetInt(j, _leafNodeIdxIndex);

            // Walk the list and add elements that intersect.
            int eltNodeIndex = _nodes.Get(ndIndex, _nodeIdxFc);
            int element;
            while (eltNodeIndex != -1)
            {
                element = _eleNodes.Get(eltNodeIndex, _leNodeIdxElt);
                if (IntersectVector(bounds, _eleBounds.Data[element]))
                {
                    cancel = !callback.Invoke(items![element]);
                    if (cancel)
                        break;
                }
                eltNodeIndex = _eleNodes.Get(eltNodeIndex, _eleNodeIdxNext);
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
        Array.Clear(items!);
        _nodes.Insert();
        _nodes.Set(0, _nodeIdxFc, -1);
        _nodes.Set(0, _nodeIdxNum, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IntersectVector(
        Vector128<float> b1,
        Vector128<float> b2)
    {
        var shuffled =
            Vector256.Shuffle(Vector256.Create(b1, b2), Vector256.Create(6, 2, 7, 3, 0, 4, 1, 5));
        return Vector128.GreaterThanOrEqualAll(shuffled.GetLower(), shuffled.GetUpper());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNode(FloatList nodes, int ndIndex, int ndDepth, float ndMx, float ndMy, float ndSx, float ndSy)
    {
        nodes.PushBack(stackalloc[] { ndMx, ndMy, ndSx, ndSy, ndIndex, ndDepth });
    }

    public (float value, int nodeId) FindFirstLeafBounds(in Vector128<float> bounds, int direction, HashSet<int> skipList)
    {
        var data = new ReadOnlySpan<float>(_rootNode);
        var toProcess = _leafNodeListCache.Get();
        var toProcessList = toProcess.List;
        var toProcessListData = toProcessList.Data!;

        toProcessList.PushBack(data);

        while (toProcessList.InternalCount > 0)
        {
            int backIdx = toProcessList.InternalCount - 1;
            int backOffset = backIdx * 6;
            //var ndData = toProcessList.Get(backIdx, 0, 6);
            //var ndIndex = (int)ndData[_leafNodeIdxIndex];
            //var ndDepth = (int)ndData[_leafNodeIdxDepth];

            var ndIndexOffset = (int)toProcessListData[backOffset + _leafNodeIdxIndex] * 2;
            toProcessList.InternalCount--;

            var nodeId = _nodes.Data![ndIndexOffset + _nodeIdxNum];

            if(skipList.Contains(nodeId))
                continue;

            // If this node is a leaf, walk it to see if there is an element on it.
            if (nodeId != -1)
            {
                // Walk the list and add elements that intersect.
                int eltNodeIndex = _nodes.Get((int)toProcessListData[_leafNodeIdxIndex], _nodeIdxFc);
                float value = direction < 2 ? float.MinValue : float.MaxValue;
                
                if (eltNodeIndex != -1)
                {
                    while (eltNodeIndex != -1)
                    {
                        var element = _eleNodes.Get(eltNodeIndex, _leNodeIdxElt);

                        var eleBoundDirection = _eleBounds.Data[element][direction];
                        value = direction < 2 
                            ? MathF.Max(eleBoundDirection, value)
                            : MathF.Min(eleBoundDirection, value);
                        eltNodeIndex = _eleNodes.Get(eltNodeIndex, _eleNodeIdxNext);
                    }

                    toProcess.Return();
                    return (value, nodeId);
                }
            }
            else
            {

                var ndDepth = (int)toProcessListData[backOffset + _leafNodeIdxDepth];
                var mx = toProcessListData[backOffset + _leafNodeIdxMx];
                var my = toProcessListData[backOffset + _leafNodeIdxMy];
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Data[ndIndexOffset + _nodeIdxFc]; //_nodes.Get(ndIndex, _nodeIdxFc);
                var hx = toProcessListData[backOffset + _leafNodeIdxHalfWidth] / 2;
                var hy = toProcessListData[backOffset + _leafNodeIdxHalfHeight] / 2;
                var l = mx - hx;
                var r = mx + hx;

                var offset = toProcessList.InternalCount;
                if (toProcessList.EnsureSpaceAvailable(4))
                    toProcessListData = toProcessList.Data!;

                if (bounds[_eltIdxTop] <= my)
                {
                    var t = my - hx;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 0; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 1; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                if (bounds[_eltIdxBtm] > my)
                {
                    var b = my + hy;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 2; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 3; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                toProcessList.InternalCount = offset;
            }
        }

        return (5000000, -1);

    }

    private FloatList.Cache.Item FindLeaves(
        ReadOnlySpan<float> data,
        in Vector128<float> bounds)
    {
        var leaves = _leafNodeListCache.Get();
        var toProcess = _leafNodeListCache.Get();
        var toProcessList = toProcess.List;
        var toProcessListData = toProcessList.Data!;

        toProcessList.PushBack(data);

        while (toProcessList.InternalCount > 0)
        {
            int backIdx = toProcessList.InternalCount - 1;
            int backOffset = backIdx * 6;
            //var ndData = toProcessList.Get(backIdx, 0, 6);
            //var ndIndex = (int)ndData[_leafNodeIdxIndex];
            //var ndDepth = (int)ndData[_leafNodeIdxDepth];

            var ndIndexOffset = (int)toProcessListData[backOffset + _leafNodeIdxIndex] * 2;
            toProcessList.InternalCount--;

            // If this node is a leaf, insert it to the list.

            if (_nodes.Data![ndIndexOffset + _nodeIdxNum] != -1)
            {
                leaves.List.PushBack(toProcessList.Get(backIdx, 0, 6));
            }
            else
            {
                var ndDepth = (int)toProcessListData[backOffset + _leafNodeIdxDepth];
                var mx = toProcessListData[backOffset + _leafNodeIdxMx];
                var my = toProcessListData[backOffset + _leafNodeIdxMy];
                // Otherwise push the children that intersect the rectangle.
                int fc = _nodes.Data[ndIndexOffset + _nodeIdxFc]; //_nodes.Get(ndIndex, _nodeIdxFc);
                var hx = toProcessListData[backOffset + _leafNodeIdxHalfWidth] / 2;
                var hy = toProcessListData[backOffset + _leafNodeIdxHalfHeight] / 2;
                var l = mx - hx;
                var r = mx + hx;

                var offset = toProcessList.InternalCount;
                if(toProcessList.EnsureSpaceAvailable(4))
                    toProcessListData = toProcessList.Data!;


                if (bounds[_eltIdxTop] <= my)
                {
                    var t = my - hx;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 0; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = t; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 1; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                if (bounds[_eltIdxBtm] > my)
                {
                    var b = my + hy;
                    if (bounds[_eltIdxLft] <= mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = l; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 2; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }

                    if (bounds[_eltIdxRgt] > mx)
                    {
                        var thisOffset = offset++ * 6;
                        toProcessListData[thisOffset + _leafNodeIdxMx] = r; // ndMx
                        toProcessListData[thisOffset + _leafNodeIdxMy] = b; // ndMy
                        toProcessListData[thisOffset + _leafNodeIdxHalfWidth] = hx; // ndSx
                        toProcessListData[thisOffset + _leafNodeIdxHalfHeight] = hy; // ndSy
                        toProcessListData[thisOffset + _leafNodeIdxIndex] = fc + 3; // ndIndex
                        toProcessListData[thisOffset + _leafNodeIdxDepth] = ndDepth + 1; // ndDepth
                    }
                }

                toProcessList.InternalCount = offset;
            }
        }

        toProcess.Return();

        return leaves;
    }


    private void NodeInsert(ReadOnlySpan<float> data, in Vector128<float> elementBounds, int elementId)
    {
        var leaves = FindLeaves(data, elementBounds);

        for (int j = 0; j < leaves.List.InternalCount; ++j)
            LeafInsert(elementId, leaves.List.Get(j, 0, 6));

        leaves.Return();
    }

    private void LeafInsert(int element, ReadOnlySpan<float> data)
    {
        var node = (int)data[_leafNodeIdxIndex];
        var depth = (int)data[_leafNodeIdxDepth];

        // Insert the element node to the leaf.
        int ndFc = _nodes.Get(node, _nodeIdxFc);

        _nodes.Set(node, _nodeIdxFc, _eleNodes.Insert());
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _eleNodeIdxNext, ndFc);
        _eleNodes.Set(_nodes.Get(node, _nodeIdxFc), _leNodeIdxElt, element);

        // If the leaf is full, split it.
        if (_nodes.Get(node, _nodeIdxNum) == _maxElements && depth < _maxDepth)
        {
            // Transfer elements from the leaf node to a list of elements.
            IntList elements = new IntList(1);
            while (_nodes.Get(node, _nodeIdxFc) != -1)
            {
                int index = _nodes.Get(node, _nodeIdxFc);
                int nextIndex = _eleNodes.Get(index, _eleNodeIdxNext);
                int elt = _eleNodes.Get(index, _leNodeIdxElt);

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
                NodeInsert(data, _eleBounds.Data[id], id);
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
        if (items == null)
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
