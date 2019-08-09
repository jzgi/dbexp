using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A resource controller that represents a scope of processing.
    /// </summary>
    public abstract class WebController
    {
        /// <summary>
        /// The parent controller, null if this is the root.
        /// </summary>
        public WebController Parent { get; internal set; }

        /// <summary>
        /// The unique name of the controller.
        /// </summary>
        public string Name { get; internal set; }


        protected internal virtual void OnInitialize()
        {
        }


        protected internal abstract Task HandleAsync(string rsc, WebContext wc);


        //
        // local object provider, for Attach() and Obtain() operations
        //

        Cell[] _cells;

        int _size;

        public void Attach(object value, byte flag = 0)
        {
            if (_cells == null)
            {
                _cells = new Cell[16];
            }

            _cells[_size++] = new Cell(value, flag);
        }

        public void Attach<V>(Func<V> fetch, int maxage = 60, byte flag = 0) where V : class
        {
            if (_cells == null)
            {
                _cells = new Cell[8];
            }

            _cells[_size++] = new Cell(typeof(V), fetch, maxage, flag);
        }

        public void Attach<V>(Func<Task<V>> fetchAsync, int maxage = 60, byte flag = 0) where V : class
        {
            if (_cells == null)
            {
                _cells = new Cell[8];
            }

            _cells[_size++] = new Cell(typeof(V), fetchAsync, maxage, flag);
        }

        /// <summary>
        /// Search for typed object in this scope and the scopes of ancestors; 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>the result object or null</returns>
        public T Obtain<T>(byte flag = 0) where T : class
        {
            if (_cells != null)
            {
                for (int i = 0; i < _size; i++)
                {
                    var h = _cells[i];
                    if (h.Flag == 0 || (h.Flag & flag) > 0)
                    {
                        if (!h.IsAsync && typeof(T).IsAssignableFrom(h.Typ))
                        {
                            return h.GetValue() as T;
                        }
                    }
                }
            }

            return Parent?.Obtain<T>(flag);
        }

        public async Task<T> ObtainAsync<T>(byte flag = 0) where T : class
        {
            if (_cells != null)
            {
                for (int i = 0; i < _size; i++)
                {
                    var cell = _cells[i];
                    if (cell.Flag == 0 || (cell.Flag & flag) > 0)
                    {
                        if (cell.IsAsync && typeof(T).IsAssignableFrom(cell.Typ))
                        {
                            return await cell.GetValueAsync() as T;
                        }
                    }
                }
            }

            if (Parent == null)
            {
                return null;
            }

            return await Parent.ObtainAsync<T>(flag);
        }


        /// <summary>
        /// A object holding cell in registry.
        /// </summary>
        class Cell
        {
            readonly Type _typ;

            readonly Func<object> _fetch;

            readonly Func<Task<object>> _fetchAsync;

            readonly int _maxage; //in seconds

            // tick count,   
            int _expiry;

            object _value;

            readonly byte _flag;

            internal Cell(object value, byte flag)
            {
                _typ = value.GetType();
                _value = value;
                _flag = flag;
            }

            internal Cell(Type typ, Func<object> fetch, int maxage, byte flag)
            {
                _typ = typ;
                _flag = flag;
                if (fetch is Func<Task<object>> fetch2)
                {
                    _fetchAsync = fetch2;
                }
                else
                {
                    _fetch = fetch;
                }

                _maxage = maxage;
            }

            public Type Typ => _typ;

            public byte Flag => _flag;

            public bool IsAsync => _fetchAsync != null;

            public object GetValue()
            {
                if (_fetch == null) // simple object
                {
                    return _value;
                }

                lock (_fetch) // cache object
                {
                    if (Environment.TickCount >= _expiry)
                    {
                        _value = _fetch();
                        _expiry = (Environment.TickCount & int.MaxValue) + _maxage * 1000;
                    }

                    return _value;
                }
            }

            public async Task<object> GetValueAsync()
            {
                if (_fetchAsync == null) // simple object
                {
                    return _value;
                }

                int lexpiry = _expiry;
                int ticks = Environment.TickCount;
                if (ticks >= lexpiry)
                {
                    _value = await _fetchAsync();
                    _expiry = (Environment.TickCount & int.MaxValue) + _maxage * 1000;
                }

                return _value;
            }
        }
    }
}