using System;
using System.Collections;
using System.Collections.Generic;

namespace WebReady
{
    /// <summary>
    /// An add-only data collection that can act as both a list, a dictionary and/or a two-layered tree.
    /// </summary>
    public class Map<K, V> : IEnumerable<Map<K, V>.Entry>
    {
        int[] _buckets;

        protected Entry[] _entries;

        int _count;

        // current group head
        int _head = -1;

        public Map(int capacity = 16)
        {
            // find a least power of 2 that is greater than or equal to capacity
            int size = 8;
            while (size < capacity)
            {
                size <<= 1;
            }

            ReInit(size);
        }

        void ReInit(int size) // size must be power of 2
        {
            if (_entries == null || size > _entries.Length) // allocalte new arrays as needed
            {
                _buckets = new int[size];
                _entries = new Entry[size];
            }

            for (int i = 0; i < _buckets.Length; i++) // initialize all buckets to -1
            {
                _buckets[i] = -1;
            }

            _count = 0;
        }

        public int Count => _count;

        public Entry EntryAt(int idx) => _entries[idx];

        public K KeyAt(int idx) => _entries[idx].key;

        public V ValueAt(int idx) => _entries[idx].value;

        public V[] GroupOf(K key)
        {
            int idx = IndexOf(key);
            if (idx > -1)
            {
                int tail = _entries[idx].tail;
                int ret = tail - idx; // number of returned elements
                V[] arr = new V[ret];
                for (int i = 0; i < ret; i++)
                {
                    arr[i] = _entries[idx + 1 + i].value;
                }

                return arr;
            }

            return null;
        }

        public int IndexOf(K key)
        {
            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % _buckets.Length; // target bucket
            int idx = _buckets[buck];
            while (idx != -1)
            {
                Entry e = _entries[idx];
                if (e.Match(code, key))
                {
                    return idx;
                }

                idx = _entries[idx].next; // adjust for next index
            }

            return -1;
        }

        public void Clear()
        {
            if (_entries != null)
            {
                ReInit(_entries.Length);
            }
        }

        public void Add(K key, V value)
        {
            Add(key, value, false);
        }

        public void Add<M>(M v) where M : V, IKeyable<K>
        {
            Add(v.Key, v, false);
        }

        void Add(K key, V value, bool rehash)
        {
            // ensure double-than-needed capacity
            if (!rehash && _count >= _entries.Length / 2)
            {
                Entry[] old = _entries;
                int oldc = _count;
                ReInit(_entries.Length * 2);
                // re-add old elements
                for (int i = 0; i < oldc; i++)
                {
                    Add(old[i].key, old[i].value, true);
                }
            }

            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % _buckets.Length; // target bucket
            int idx = _buckets[buck];
            while (idx != -1)
            {
                Entry e = _entries[idx];
                if (e.Match(code, key))
                {
                    e.value = value;
                    return; // replace the old value
                }

                idx = _entries[idx].next; // adjust for next index
            }

            // add a new entry
            idx = _count;
            _entries[idx] = new Entry(code, _buckets[buck], key, value);
            _buckets[buck] = idx;
            _count++;

            // decide group
            if (value is IGroupKeyable<K> gkeyable)
            {
                // compare to current head
                if (_head == -1 || !gkeyable.GroupAs(_entries[_head].key))
                {
                    _head = idx;
                }

                _entries[_head].tail = idx;
            }
        }

        public bool Contains(K key)
        {
            if (TryGet(key, out _))
            {
                return true;
            }

            return false;
        }

        public bool TryGet(K key, out V value)
        {
            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % _buckets.Length; // target bucket
            int idx = _buckets[buck];
            while (idx != -1)
            {
                var e = _entries[idx];
                if (e.Match(code, key))
                {
                    value = e.value;
                    return true;
                }

                idx = _entries[idx].next; // adjust for next index
            }

            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return new Enumerator(this);
        }

        //
        // advanced search operations that can be overridden with concurrency constructs

        public V this[K key]
        {
            get
            {
                if (key == null) return default;

                if (TryGet(key, out var val))
                {
                    return val;
                }

                return default;
            }
            set => Add(key, value);
        }

        public V[] All(Predicate<V> cond = null)
        {
            var list = new ValueList<V>(16);
            for (int i = 0; i < _count; i++)
            {
                var v = _entries[i].value;
                if (cond == null || cond(v))
                {
                    list.Add(v);
                }
            }

            return list.ToArray();
        }

        public V Find(Predicate<V> cond = null)
        {
            for (int i = 0; i < _count; i++)
            {
                var v = _entries[i].value;
                if (cond == null || cond(v))
                {
                    return v;
                }
            }

            return default;
        }

        public void ForEach(Func<K, V, bool> cond, Action<K, V> hand)
        {
            for (int i = 0; i < _count; i++)
            {
                K key = _entries[i].key;
                V value = _entries[i].value;
                if (cond == null || cond(key, value))
                {
                    hand(_entries[i].key, _entries[i].value);
                }
            }
        }

        public struct Entry
        {
            readonly int code; // lower 31 bits of hash code

            internal readonly K key; // entry key

            internal V value; // entry value

            internal readonly int next; // index of next entry, -1 if last

            internal int tail; // the index of group tail, when this is the head entry

            internal Entry(int code, int next, K key, V value)
            {
                this.code = code;
                this.next = next;
                this.key = key;
                this.value = value;
                this.tail = -1;
            }

            internal bool Match(int code, K key)
            {
                return this.code == code && this.key.Equals(key);
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public K Key => key;

            public V Value => value;

            public bool IsHead => tail > -1;
        }

        public struct Enumerator : IEnumerator<Entry>
        {
            readonly Map<K, V> map;

            int current;

            internal Enumerator(Map<K, V> map)
            {
                this.map = map;
                current = -1;
            }

            public bool MoveNext()
            {
                return ++current < map.Count;
            }

            public void Reset()
            {
                current = -1;
            }

            public Entry Current => map._entries[current];

            object IEnumerator.Current => map._entries[current];

            public void Dispose()
            {
            }
        }

        static void Test()
        {
            Map<string, string> m = new Map<string, string>
            {
                {"010101", "mike"},
                {"010102", "jobs"},
                {"010103", "tim"},
                {"010104", "john"},
                {"010301", "abigae"},
                {"010302", "stephen"},
                {"010303", "cox"},
            };

            var r = m.GroupOf("010101");
        }
    }
}