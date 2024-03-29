﻿using System;
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
        // sub-controllers of the service
        readonly Map<string, WebController> controllers = new Map<string, WebController>(64);

        //
        // http implementation

        string[] addrs;

        // shared cache or not
        bool cache = true;

        // cache of responses
        ConcurrentDictionary<string, Response> _cache;

        // the embedded HTTP server
        KestrelServer server;

        // the response cache cleaner thread
        Thread cleaner;

        protected WebService() => Pathing = "/";

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
                server = new KestrelServer(Options.Create(opts), Framework.TransportFactory, logf);

                var coll = server.Features.Get<IServerAddressesFeature>().Addresses;
                foreach (string a in addrs)
                {
                    coll.Add(a.Trim());
                }
            }
        }

        public T AddController<T>(string name) where T : WebController, new()
        {
            var ctr = new T()
            {
                Parent = this,
                Name = name,
                Pathing = "/" + name + "/"
            };

            controllers.Add(name, ctr);

            // applevel init
            ctr.OnInitialize();

            return ctr;
        }

        public T AddController<T>(T ctr) where T : WebController
        {
            controllers.Add(ctr.Name, ctr);
            ctr.Parent = this;
            ctr.Pathing = "/" + ctr.Name + "/";

            // applevel init
            ctr.OnInitialize();

            return ctr;
        }

        public Map<string, WebController> Controllers => controllers;

        public void LoadSubsFromDb(string source)
        {
            // load db types

            var src = Framework.GetDbSource(source);

            using (var dc = src.NewDbContext())
            {
                // load views under the public schema

                var nsp = (uint) dc.Scalar("SELECT oid FROM pg_namespace WHERE nspname = 'public'", prepare: false);

                // load views
                dc.QueryAll("SELECT oid, relname AS name, pg_get_viewdef(oid) AS definition, relhaspkey AS pk, (pg_relation_is_updatable(oid::regclass, false) & 20) = 20 AS updatable, (pg_relation_is_updatable(oid::regclass, false) & 8) = 8 AS insertable FROM pg_class WHERE relnamespace = " + nsp + " AND relkind = 'v'", prepare: false);
                while (dc.Next())
                {
                    var view = new DbViewSet(dc)
                    {
                        Source = src
                    };
                    AddController(view);

                    using (var ndc = src.NewDbContext())
                    {
                        ndc.QueryAll("SELECT attname AS name, atttypid AS typoid, atthasdef AS def, attnotnull AS notnull FROM pg_attribute WHERE attrelid = @1", p => p.Set(view.Oid));
                        while (ndc.Next())
                        {
                            view.AddColumn(new DbField(ndc));
                        }
                    }
                }

                // roles
                //

                var roles = new Map<uint, string>(16)
                {
                    {0, "PUBLIC"}
                };
                dc.Query("SELECT oid, rolname FROM pg_roles");
                while (dc.Next())
                {
                    dc.Let(out uint oid);
                    dc.Let(out string rolname);
                    roles.Add(oid, rolname);
                }

                using (var dcb = src.NewDbContext())
                {
                    dcb.QueryAll("SELECT relname, (aclexplode(COALESCE(pg_class.relacl, acldefault('r'::\"char\", pg_class.relowner)))).grantee AS grantee, (aclexplode(COALESCE(pg_class.relacl, acldefault('r'::\"char\", pg_class.relowner)))).privilege_type AS optype FROM pg_class WHERE relkind = 'v' AND relnamespace = " + nsp);
                    while (dcb.Next())
                    {
                        dcb.Let(out string relname);
                        dcb.Let(out uint grantee);
                        dcb.Let(out string optype);

                        var ctr = controllers.GetValue(relname);
                        ((DbViewSet) ctr)?.AddRole(optype, roles.GetValue(grantee));
                    }
                }

                // load functions
                //
                dc.QueryAll("SELECT NOT (proisagg OR proiswindow) AS callable, oid, proname AS name, provolatile AS volatile, prorettype AS rettype, proretset AS retset, proargmodes, proargnames, proargtypes, proallargtypes, proargdefaults FROM pg_proc WHERE pronamespace = " + nsp, prepare: false);
                while (dc.Next())
                {
                    bool callable = false;
                    dc.Get(nameof(callable), ref callable);
                    if (!callable) continue;

                    string name = null;
                    dc.Get(nameof(name), ref name);
                    var action = new DbFunctionAction(this, name, dc)
                    {
                        Source = src
                    };
                    Actions.Add(action);
                }

                using (var dcb = src.NewDbContext())
                {
                    dcb.QueryAll("SELECT proname, (aclexplode(COALESCE(pg_proc.proacl, acldefault('f'::\"char\", pg_proc.proowner)))).grantee AS grantee, (aclexplode(COALESCE(pg_proc.proacl, acldefault('f'::\"char\", pg_proc.proowner)))).privilege_type AS optype FROM pg_proc WHERE pronamespace = " + nsp);
                    while (dcb.Next())
                    {
                        dcb.Let(out string proname);
                        dcb.Let(out uint grantee);
                        dcb.Let(out string optype);

                        var ctr = Actions.GetValue(proname);
                        ((DbFunctionAction) ctr)?.AddRole(optype, roles.GetValue(grantee));
                    }
                }
            }
        }


        /// <summary>
        /// To asynchronously process the request.
        /// </summary>
        public async Task ProcessRequestAsync(HttpContext context)
        {
            var wc = (WebContext) context;

            string path = wc.Path;

            // determine it is static file
            try
            {
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
                    // do authentication since it is dynamic 
                    if (this is IAuthenticateAsync authAsync)
                    {
                        await authAsync.DoAuthenticateAsync(wc);
                    }
                    else if (this is IAuthenticate auth)
                    {
                        auth.DoAuthenticate(wc);
                    }


                    // a sub controller

                    string rsc = path.Substring(1);

                    int slash = rsc.IndexOf('/');
                    if (slash != -1)
                    {
                        string name = rsc.Substring(0, slash);
                        var ctr = controllers.GetValue(name);
                        if (ctr == null)
                        {
                            throw new WebException("Controller not found: " + name)
                            {
                                Code = 404 // Not Found
                            };
                        }

                        await ctr.HandleAsync(rsc.Substring(slash + 1), wc);
                    }
                    else
                    {
                        // it is an action
                        await HandleAsync(rsc, wc);
                    }
                }
            }
            catch (WebException e)
            {
                wc.Give(e.Code, e.Message);
            }
            catch (Exception e)
            {
                wc.Give(500, e.Message);
            }
            finally
            {
                await wc.SendAsync();
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
            await server.StartAsync(this, token);

            Console.WriteLine(Name + " started at " + addrs[0]);

            // create and start the cleaner thread
            if (_cache != null)
            {
                cleaner = new Thread(() =>
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
                cleaner.Start();
            }
        }

        internal async Task StopAsync(CancellationToken token)
        {
            await server.StopAsync(token);

            // close logger
            //            logWriter.Flush();
            //            logWriter.Dispose();
        }

        public void Dispose()
        {
            server.Dispose();
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

        public void @ref(WebContext wc)
        {
            var h = new HtmlContent(wc, true);
            h.T("<html><head>");
            h.T("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            h.T("<style>pre {overflow-x: scroll; background-color: #f2f2f2; padding: 8px;} pre::-webkit-scrollbar {display: none;}</style>");
            h.T("</head><body>");
            h.H3(Name.ToUpper());
            h.T("<main style=\"display: grid;; grid-template-columns: repeat(auto-fit, minmax(20rem, 36rem)); grid-gap: 12px;\">");

            // describe this work 
            Describe(h);

            // controlers
            for (int i = 0; i < controllers.Count; i++)
            {
                var ctr = controllers[i].Value;
                ctr.Describe(h);
            }

            h.T("</main>");
            h.T("</body></html>");
            wc.Give(200, h, true, 720);
        }

        /// <summary>
        /// A prior response for caching that might be cleared but not removed, for better reusability. 
        /// </summary>
        public class Response
        {
            // response status, 0 means cleared, otherwise one of the cacheable status
            int code;

            // can be set to null
            IContent content;

            // maxage in seconds
            int maxage;

            // time ticks when entered or cleared
            int stamp;

            int hits;

            internal Response(int code, IContent content, int maxage, int stamp)
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