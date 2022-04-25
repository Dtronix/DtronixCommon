// Based on LinkedList<T> from the dot net foundation.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DtronixCommon.Collections;

/// <summary>
/// Simplified double linked list based on LinkedList&lt;T&gt;
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimpleLinkedList<T>
{
    // This SimpleLinkedList is a doubly-Linked circular list.
    internal SimpleLinkedListNode<T>? head;
    internal int count;

    public int Count => count;

    public SimpleLinkedListNode<T>? First => head;

    public SimpleLinkedListNode<T>? Last => head?.prev;

    public SimpleLinkedListNode<T> AddAfter(SimpleLinkedListNode<T> node, T value)
    {
        SimpleLinkedListNode<T> result = new SimpleLinkedListNode<T>(node.list!, value);
        InternalInsertNodeBefore(node.next!, result);
        return result;
    }

    public void AddAfter(SimpleLinkedListNode<T> node, SimpleLinkedListNode<T> newNode)
    {
        InternalInsertNodeBefore(node.next!, newNode);
        newNode.list = this;
    }

    public SimpleLinkedListNode<T> AddBefore(SimpleLinkedListNode<T> node, T value)
    {
        SimpleLinkedListNode<T> result = new SimpleLinkedListNode<T>(node.list!, value);
        InternalInsertNodeBefore(node, result);
        if (node == head)
        {
            head = result;
        }

        return result;
    }

    public void AddBefore(SimpleLinkedListNode<T> node, SimpleLinkedListNode<T> newNode)
    {
        InternalInsertNodeBefore(node, newNode);
        newNode.list = this;
        if (node == head)
        {
            head = newNode;
        }
    }

    public SimpleLinkedListNode<T> AddFirst(T value)
    {
        SimpleLinkedListNode<T> result = new SimpleLinkedListNode<T>(this, value);
        if (head == null)
        {
            InternalInsertNodeToEmptyList(result);
        }
        else
        {
            InternalInsertNodeBefore(head, result);
            head = result;
        }

        return result;
    }

    public void AddFirst(SimpleLinkedListNode<T> node)
    {
        if (head == null)
        {
            InternalInsertNodeToEmptyList(node);
        }
        else
        {
            InternalInsertNodeBefore(head, node);
            head = node;
        }

        node.list = this;
    }

    public SimpleLinkedListNode<T> AddLast(T value)
    {
        SimpleLinkedListNode<T> result = new SimpleLinkedListNode<T>(this, value);
        if (head == null)
        {
            InternalInsertNodeToEmptyList(result);
        }
        else
        {
            InternalInsertNodeBefore(head, result);
        }

        return result;
    }

    public void AddLast(SimpleLinkedListNode<T> node)
    {
        if (head == null)
        {
            InternalInsertNodeToEmptyList(node);
        }
        else
        {
            InternalInsertNodeBefore(head, node);
        }

        node.list = this;
    }

    public void Clear()
    {
        head = null;
        count = 0;
    }

    public void Remove(SimpleLinkedListNode<T> node)
    {
        InternalRemoveNode(node);
    }

    public void RemoveFirst()
    {
        InternalRemoveNode(head);
    }

    public void RemoveLast()
    {
        InternalRemoveNode(head.prev!);
    }

    private void InternalInsertNodeBefore(SimpleLinkedListNode<T> node, SimpleLinkedListNode<T> newNode)
    {
        newNode.next = node;
        newNode.prev = node.prev;
        node.prev!.next = newNode;
        node.prev = newNode;
        count++;
    }

    private void InternalInsertNodeToEmptyList(SimpleLinkedListNode<T> newNode)
    {
        newNode.next = newNode;
        newNode.prev = newNode;
        head = newNode;
        count++;
    }

    public int BreakAtNode(SimpleLinkedListNode<T> node, bool after)
    {
        if (node == head || node == node.prev)
        {
            InternalRemoveNode(node);
            return 1;
        }

        if (node.next == node)
        {
            head = null;
            count--;
            return 1;
        }

        int removed = 1;
        var current = node;
        if (after)
        {

            while (current != null && current.next != head)
            {
                current = current.next;
                removed++;
            }

            node.prev!.next = head;
            head!.prev = node.prev;
        }
        else
        {
            while (current != null)
            {
                removed++;

                if (current.prev == head)
                    break;

                current = current.prev;
            }

            head = node.next;
            node.next!.prev = head;

        }

        count -= removed;
        return removed;
    }

    internal void InternalRemoveNode(SimpleLinkedListNode<T> node)
    {
        if (node.next == node)
        {
            head = null;
        }
        else
        {
            node.next!.prev = node.prev;
            node.prev!.next = node.next;
            if (head == node)
            {
                head = node.next;
            }
        }

        count--;
    }
}