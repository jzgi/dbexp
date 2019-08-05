using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebReady.Db;

namespace WebReady.Web
{
    /// <summary>
    /// An embedded web server that wraps around the kestrel HTTP engine.
    /// </summary>
    public abstract class WebService : WebWork, IHttpApplication<HttpContext>
    {
        // subscopes of the service
        readonly Map<string, WebScope> _scopes = new Map<string, WebScope>(64);

        //
        // http implementation

        string[] addrs;

        // shared cache or not
        bool cache = true;

        // cache of responses
        ConcurrentDictionary<string, Response> _cache;

        // the embedded HTTP server
        KestrelServer _server;

        // the response cache cleaner thread
        Thread _cleaner;

        public Map<string, WebScope> Scopes => _scopes;


        internal JObj Config
        {
            set
            {
                var cfg = value;

                // retrieve config settings
                cfg.Get(nameof(addrs), ref addrs);
                if (addrs == null)
                {
                    throw new FrameworkException("Missing 'addrs' configuration");
                }

                cfg.Get(nameof(cache), ref cache);
                if (cache)
                {
                    int factor = (int) Math.Log(Environment.ProcessorCount, 2) + 1;
                    // create the response cache
                    _cache = new ConcurrentDictionary<string, Response>(factor * 4, 1024);
                }

                // create the HTTP embedded server
                //
                var opts = new KestrelServerOptions();
                var logf = new LoggerFactory();
                logf.AddProvider(Framework.Logger);
                _server = new KestrelServer(Options.Create(opts), Framework.TransportFactory, logf);

                var coll = _server.Features.Get<IServerAddressesFeature>().Addresses;
                foreach (string a in addrs)
                {
                    coll.Add(a.Trim());
                }
            }
        }

        public T AddScope<T>(string name) where T : WebScope, new()
        {
            var scp = new T()
            {
                Parent = this,
                Name = name
            };

            _scopes.Add(name, scp);

            // applevel init
            scp.OnInitialize();

            return scp;
        }

        public T AddScope<T>(T scope) where T : WebScope
        {
            _scopes.Add(scope.Name, scope);
            scope.Parent = this;

            // applevel init
            scope.OnInitialize();

            return scope;
        }

        public void LoadScopesFromDb(string source)
        {
            var src = Framework.GetDbSource(source);

            using (var dc = src.NewDbContext())
            {
                // load views under the public schema
                dc.Query("SELECT * FROM information_schema.views WHERE table_schema = 'public'", prepare: false);
                while (dc.Next())
                {
                    string table_name = null;
                    dc.Get(nameof(table_name), ref table_name);
                    string view_definition = null;
                    dc.Get(nameof(view_definition), ref view_definition);
                    string check_option = null;
                    dc.Get(nameof(check_option), ref check_option);
                    bool is_updateable = false;
                    dc.Get(nameof(is_updateable), ref is_updateable);
                    bool is_insertable_into = false;
                    dc.Get(nameof(is_insertable_into), ref is_insertable_into);

                    var vset = new DbViewSet(table_name, view_definition, check_option, is_updateable, is_insertable_into)
                    {
                        Name = table_name,
                        Source = src
                    };
                    AddScope(vset);
                }
            }
        }

        public void LoadActionsFromDb(string source)
        {
            using (var dc = Framework.NewDbContext(source))
            {
                // load views

                // load functions
            }
        }


        /// <summary>
        /// To asynchronously process the request.
        /// </summary>
        public async Task ProcessRequestAsync(HttpContext context)
        {
            var wc = (WebContext) context;

            string path = wc.Path;

            // determine it is file or action content
            //
            int dot = path.LastIndexOf('.');
            if (dot != -1)
            {
                // try to give file content from cache or file system
                if (!TryGiveFromCache(wc))
                {
                    GiveStaticFile(path, path.Substring(dot), wc);
                    TryAddToCache(wc);
                }
            }
            else
            {
                // authenticate
                var prin = wc.Principal;
                string h = wc.Header("Authorization");
                if (h != null)
                {
                }


                await HandleAsync(path.Substring(1), wc);

                wc.Give(404, "not found");
                return;
            }

            // sending
            try
            {
                await wc.SendAsync();
            }
            catch (Exception e)
            {
                wc.Give(500, e.Message);
            }
        }

        public void GiveStaticFile(string filename, string ext, WebContext wc)
        {
            if (!StaticContent.TryGetType(ext, out var ctyp))
            {
                wc.Give(415); // unsupported media type
                return;
            }

            string path = Path.Join(Name, filename);
            if (!File.Exists(path))
            {
                wc.Give(404); // not found
                return;
            }

            // load file content
            var modified = File.GetLastWriteTime(path);
            byte[] bytes;
            bool gzip = false;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int len = (int) fs.Length;
                if (len > 2048)
                {
                    var ms = new MemoryStream(len);
                    using (var gzs = new GZipStream(ms, CompressionMode.Compress))
                    {
                        fs.CopyTo(gzs);
                    }

                    bytes = ms.ToArray();
                    gzip = true;
                }
                else
                {
                    bytes = new byte[len];
                    fs.Read(bytes, 0, len);
                }
            }

            var cnt = new StaticContent(bytes, bytes.Length)
            {
                Key = filename,
                Type = ctyp,
                Modified = modified,
                GZip = gzip
            };
            wc.Give(200, cnt, shared: true, maxage: 60 * 15);
        }


        ///
        /// Returns a framework custom context.
        ///
        public HttpContext CreateContext(IFeatureCollection features)
        {
            return new WebContext(features)
            {
                Service = this
            };
        }

        public void DisposeContext(HttpContext context, Exception excep)
        {
            // dispose the context
            ((WebContext) context).Dispose();
        }

        internal async Task StartAsync(CancellationToken token)
        {
            await _server.StartAsync(this, token);

            Console.WriteLine(Name + " started at " + addrs[0]);

            // create and start the cleaner thread
            if (_cache != null)
            {
                _cleaner = new Thread(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        // cleaning cycle
                        Thread.Sleep(30000); // every 30 seconds 
                        // loop to clear or remove each expired items
                        int now = Environment.TickCount;
                        foreach (var re in _cache)
                        {
                            if (!re.Value.TryClean(now))
                            {
                                _cache.TryRemove(re.Key, out _);
                            }
                        }
                    }
                });
                _cleaner.Start();
            }
        }

        internal async Task StopAsync(CancellationToken token)
        {
            await _server.StopAsync(token);

            // close logger
            //            logWriter.Flush();
            //            logWriter.Dispose();
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        //
        // RESPONSE CACHE

        internal void TryAddToCache(WebContext wc)
        {
            if (wc.IsGet)
            {
                if (!wc.IsInCache && wc.Shared == true && Response.IsCacheable(wc.StatusCode))
                {
                    var re = new Response(wc.StatusCode, wc.Content, wc.MaxAge, Environment.TickCount);
                    _cache.AddOrUpdate(wc.Uri, re, (k, old) => re.MergeWith(old));
                    wc.IsInCache = true;
                }
            }
        }

        internal bool TryGiveFromCache(WebContext wc)
        {
            if (wc.IsGet)
            {
                if (_cache.TryGetValue(wc.Uri, out var resp))
                {
                    return resp.TryGive(wc, Environment.TickCount);
                }
            }

            return false;
        }


        /// <summary>
        /// A prior response for caching that might be cleared but not removed, for better reusability. 
        /// </summary>
        public class Response
        {
            // response status, 0 means cleared, otherwise one of the cacheable status
            short code;

            // can be set to null
            IContent content;

            // maxage in seconds
            int maxage;

            // time ticks when entered or cleared
            int stamp;

            int hits;

            internal Response(short code, IContent content, int maxage, int stamp)
            {
                this.code = code;
                this.content = content;
                this.maxage = maxage;
                this.stamp = stamp;
            }

            /// <summary>
            ///  RFC 7231 cacheable status codes.
            /// </summary>
            public static bool IsCacheable(int code)
            {
                return code == 200 || code == 203 || code == 204 || code == 206 || code == 300 || code == 301 || code == 404 || code == 405 || code == 410 || code == 414 || code == 501;
            }

            public int Hits => hits;

            public bool IsCleared => code == 0;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="now"></param>
            /// <returns>false to indicate a removal of the entry</returns>
            internal bool TryClean(int now)
            {
                lock (this)
                {
                    int pass = now - (stamp + maxage * 1000);

                    if (code == 0) return pass < 900 * 1000; // 15 minutes

                    if (pass >= 0) // to clear this reply
                    {
                        code = 0; // set to cleared
                        content = null; // NOTE: the buffer won't return to the pool
                        maxage = 0;
                        stamp = now; // time being cleared
                    }

                    return true;
                }
            }

            internal bool TryGive(WebContext wc, int now)
            {
                lock (this)
                {
                    if (code == 0)
                    {
                        return false;
                    }

                    short remain = (short) (((stamp + maxage * 1000) - now) / 1000); // remaining in seconds
                    if (remain > 0)
                    {
                        wc.IsInCache = true;
                        wc.Give(code, content, true, remain);
                        Interlocked.Increment(ref hits);
                        return true;
                    }

                    return false;
                }
            }

            internal Response MergeWith(Response old)
            {
                Interlocked.Add(ref hits, old.Hits);
                return this;
            }
        }
    }
}