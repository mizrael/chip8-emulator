using System.Collections.Generic;

namespace Chip8Emulator.Core.Utils;

public class LRUCache<TKey, TValue>
    where TKey : notnull
{
    private class Node
    {
        public required TKey Key { get; init; }
        public required TValue Value { get; set; }
        public Node? Next { get; set; }
        public Node? Previous { get; set; }
    }

    private readonly Dictionary<TKey, Node> _cache;
    private readonly uint _capacity;
    private Node? _head;
    private Node? _tail;

    public LRUCache(uint capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<TKey, Node>((int)capacity);
    }

    public void Remove(TKey key)
    {
        if (!_cache.TryGetValue(key, out var node))
            return;

        if (_head == node)
            _head = node.Next;

        if (_tail == node)
            _tail = node.Previous;

        if (node.Previous != null)
            node.Previous.Next = node.Next;

        if (node.Next != null)
            node.Next.Previous = node.Previous;

        _cache.Remove(key);
    }

    public bool ContainsKey(TKey key)
    => _cache.ContainsKey(key);

    public void AddOrUpdate(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            node.Value = value;
            MoveToHead(node);
        }
        else
        {
            Add(key, value);
        }
    }

    private Node Add(TKey key, TValue value)
    {
        if (_cache.Count == _capacity)
        {
            if (_tail is not null)
            {
                _cache.Remove(_tail.Key);

                _tail = _tail.Previous;
            }

            _tail?.Next = null;
        }

        var node = new Node { Key = key, Value = value, Next = _head };
        if (_head != null)
            _head.Previous = node;

        _head = node;
        _tail ??= node;

        _cache.Add(key, node);
        return node;
    }

    private void MoveToHead(Node node)
    {
        if (node == _head)
            return;

        if (node.Previous != null)
            node.Previous.Next = node.Next;

        if (node.Next != null)
            node.Next.Previous = node.Previous;

        if (_tail == node)
            _tail = node.Previous;

        node.Previous = null;
        node.Next = _head;

        if (_head != null)
            _head.Previous = node;

        _head = node;
    }

    public (TKey?, TValue?) GetLast()
        => (_tail == null) ?
                (default, default) :
                (_tail.Key, _tail.Value);

    public uint Count => (uint)_cache.Count;

    public IEnumerable<TKey> Keys => _cache.Keys;
}