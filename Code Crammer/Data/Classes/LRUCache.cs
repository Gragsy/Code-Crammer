namespace Code_Crammer.Data
{
    public class LRUCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _maxSize;
        private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _cache;
        private readonly LinkedList<(TKey Key, TValue Value)> _lruList;
        private readonly object _lock = new object();

        public LRUCache(int maxSize)
        {
            _maxSize = maxSize;
            _cache = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(maxSize);
            _lruList = new LinkedList<(TKey, TValue)>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default!;
                return false;
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.ContainsKey(key))
                {
                    _lruList.Remove(_cache[key]);
                    _cache.Remove(key);
                }

                if (_cache.Count >= _maxSize)
                {
                    var last = _lruList.Last;
                    if (last != null)
                    {
                        _cache.Remove(last.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                var node = _lruList.AddFirst((key, value));
                _cache[key] = node;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }
    }
}