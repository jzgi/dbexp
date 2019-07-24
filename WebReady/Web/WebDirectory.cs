using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A web resource folder that may contain child folders and actors.
    /// </summary>
    public abstract class WebDirectory
    {
        static readonly Exception NotImplemented = new NotImplementedException();


        // sub-directories
        Map<string, WebDirectory> _dirs;

        // runnable tasks 
        public Map<string, WebWork> _works;

        Exception except = new Exception();

        public WebDirectory Parent { get; internal set; }

        protected internal virtual void OnInitialize()
        {
        }

        public void MakeDirectory<T>(string name) where T : WebDirectory, new()
        {
            T dir = new T {Parent = this};

            dir.OnInitialize();

            if (_dirs == null)
            {
                _dirs = new Map<string, WebDirectory>(8);
            }

            _dirs.Add(name, dir);
        }

        public void MakeWork<T>(string name) where T : WebWork, new()
        {
            T wrk = new T {Directory = this};

            wrk.OnInitialize();

            if (_works == null)
            {
                _works = new Map<string, WebWork>(8);
            }

            _works.Add(name, wrk);
        }

        public void MakeSubFromDb(string dbname)
        {
            using (var dc = Framework.NewDbContext(dbname))
            {
                // load views

                // load functions
            }
        }

        internal async Task HandleAsync(string rsc, WebContext wc)
        {
            try
            {
                int slash = rsc.IndexOf('/');
                // determine sub-dicrectory or end action
                if (slash != -1)
                {
                    string key = rsc.Substring(0, slash);
                    if (_dirs != null && _dirs.TryGet(key, out var wrk))
                    {
                        await wrk.HandleAsync(rsc.Substring(slash + 1), wc);
                    }
                    else
                    {
                        var run = _works[rsc];
                        if (run == null)
                        {
                            wc.Give(404, "action not found", true, 12);
                            return;
                        }

                        wc.Work = run;
                    }
                }
            }
            finally
            {
            }
        }

        public virtual void Get(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        public virtual void Post(WebContext wc)
        {
            throw NotImplemented;
        }

        public virtual void Put(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        public virtual void Delete(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        //
        // local object provider, for Attach() and Obtain() operations
        //

        Hold[] _holds;

        int _size;

        public void Attach(object value, byte flag = 0)
        {
            if (_holds == null)
            {
                _holds = new Hold[16];
            }

            _holds[_size++] = new Hold(value, flag);
        }

        public void Attach<V>(Func<V> fetch, int maxage = 60, byte flag = 0) where V : class
        {
            if (_holds == null)
            {
                _holds = new Hold[8];
            }

            _holds[_size++] = new Hold(typeof(V), fetch, maxage, flag);
        }

        public void Attach<V>(Func<Task<V>> fetchAsync, int maxage = 60, byte flag = 0) where V : class
        {
            if (_holds == null)
            {
                _holds = new Hold[8];
            }

            _holds[_size++] = new Hold(typeof(V), fetchAsync, maxage, flag);
        }

        /// <summary>
        /// Search for typed object in this scope and the scopes of ancestors; 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>the result object or null</returns>
        public T Obtain<T>(byte flag = 0) where T : class
        {
            if (_holds != null)
            {
                for (int i = 0; i < _size; i++)
                {
                    var h = _holds[i];
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
            if (_holds != null)
            {
                for (int i = 0; i < _size; i++)
                {
                    var cell = _holds[i];
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
        /// A object holder in registry.
        /// </summary>
        class Hold
        {
            readonly Type _typ;

            readonly Func<object> _fetch;

            readonly Func<Task<object>> _fetchAsync;

            readonly int _maxage; //in seconds

            // tick count,   
            int _expiry;

            object _value;

            readonly byte _flag;

            internal Hold(object value, byte flag)
            {
                _typ = value.GetType();
                _value = value;
                _flag = flag;
            }

            internal Hold(Type typ, Func<object> fetch, int maxage, byte flag)
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