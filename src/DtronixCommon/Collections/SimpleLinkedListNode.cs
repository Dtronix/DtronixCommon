namespace DtronixCommon.Collections;

public sealed class SimpleLinkedListNode<T>
{
    internal SimpleLinkedList<T>? list;
    internal SimpleLinkedListNode<T>? next;
    internal SimpleLinkedListNode<T>? prev;
    internal T item;

    public SimpleLinkedListNode(T value)
    {
        item = value;
    }

    internal SimpleLinkedListNode(SimpleLinkedList<T> list, T value)
    {
        this.list = list;
        item = value;
    }

    public SimpleLinkedList<T>? List => list;

    public SimpleLinkedListNode<T>? Next => next == null || next == list!.head ? null : next;

    public SimpleLinkedListNode<T>? Previous => prev == null || this == list!.head ? null : prev;

    public T Value
    {
        get => item;
        set => item = value;
    }

    /// <summary>Gets a reference to the value held by the node.</summary>
    public ref T ValueRef => ref item;
}