using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebReady.Db;
using WebReady.Web;
using WebException = System.Net.WebException;

namespace WebReady
{
    /// <summary>
    /// The application scope that holds global states.
    /// </summary>
    public class Framework
    {
        public const string WEPAPP_JSON = "webapp.json";

        public const string CERT_PFX = "cert.pfx";

        internal static readonly WebLifetime Lifetime = new WebLifetime();

        internal static readonly ITransportFactory TransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), Lifetime, NullLoggerFactory.Instance);

        //
        // configuration processing
        //

        public static readonly JObj Config;

        public static readonly JObj ConfigWeb, ConfigDb, ConfigExt;

        // the sharding notation for this service instance
        public static string shard;

        // the descriptive information of this service instance
        public static string descr;

        // logging level
        public static int logging = 3;

        public static int cipher;

        public static string certpasswd;


        static readonly Map<string, WebService> webservices = new Map<string, WebService>(4);

        static readonly Map<string, NetPeer> webpeers = new Map<string, NetPeer>(16);

        static readonly Map<string, DbSource> dbsources = new Map<string, DbSource>(4);


        internal static readonly string Sign;

        internal static readonly FrameworkLogger Logger;


        static List<NetPeer> polls;

        // the thread schedules and drives periodic jobs, such as event polling 
        static Thread scheduler;

        public static readonly WebService WebService;

        static Framework()
        {
            // setup logger first
            //
            string logfile = DateTime.Now.ToString("yyyyMM") + ".log";
            Logger = new FrameworkLogger(logfile);
            if (!File.Exists(WEPAPP_JSON))
            {
                Logger.Log(LogLevel.Error, WEPAPP_JSON + " not found");
                return;
            }

            // load configuration
            //
            byte[] bytes = File.ReadAllBytes(WEPAPP_JSON);
            JsonParser parser = new JsonParser(bytes, bytes.Length);
            Config = (JObj) parser.Parse();

            Sign = Config["cipher"];

            Logger.Level = Config["logging"];

            // references
            JObj r = Config["WEBUSE"];
            if (r != null)
            {
                for (int i = 0; i < r.Count; i++)
                {
                    var e = r.EntryAt(i);
                    if (webpeers == null)
                    {
                        webpeers = new Map<string, NetPeer>(16);
                    }

                    webpeers.Add(new NetPeer(e.Key, e.Value)
                    {
                        Clustered = true
                    });
                }
            }


            // create and start the scheduler thead
            if (polls != null)
            {
                // to repeatedly check and initiate event polling activities.
                scheduler = new Thread(() =>
                {
                    while (true)
                    {
                        // interval
                        Thread.Sleep(1000);

                        // a schedule cycle
                        int tick = Environment.TickCount;
                        for (int i = 0; i < polls.Count; i++)
                        {
                            var cli = polls[i];
                            cli.TryPollAsync(tick);
                        }
                    }
                });
                scheduler.Start();
            }
        }

        public static void MakeService<T>(string name) where T : WebService, new()
        {
            JObj cfg = Config["SERVICES"];
            if (cfg == null)
            {
                throw new WebException("Missing services");
            }

            var svc = new T();
            svc.Initialize(cfg);
        }


        public static DbContext NewDbContext(string name, IsolationLevel? level = null)
        {
            JObj ds = Config["DBSOURCE"];
            if (ds == null)
            {
                throw new WebException();
            }

            JObj db = ds[name];
            if (db == null)
            {
                throw new WebException();
            }

            var source = dbsources[name];

            DbContext dc = new DbContext(source);
            if (level != null)
            {
                dc.Begin(level.Value);
            }

            return dc;
        }


        // LOGGING

        public static void TRC(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Trace, 0, msg, ex, null);
            }
        }

        public static void DBG(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Debug, 0, msg, ex, null);
            }
        }

        public static void INF(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Information, 0, msg, ex, null);
            }
        }

        public static void WAR(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Warning, 0, msg, ex, null);
            }
        }

        public static void ERR(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Error, 0, msg, ex, null);
            }
        }


        static readonly CancellationTokenSource Cts = new CancellationTokenSource();

        /// 
        /// Runs a number of web services and block until shutdown.
        /// 
        public static async Task StartAll()
        {
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    cts.Cancel();

                    // wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                // start services
                for (int i = 0; i < webservices.Count; i++)
                {
                    var svc = webservices.ValueAt(i);
                    await svc.StartAsync(cts.Token);
                }

                Console.WriteLine("ctrl_c to shut down");

                cts.Token.Register(state =>
                    {
                        ((IApplicationLifetime) state).StopApplication();
                        // dispose services
                        for (int i = 0; i < webservices.Count; i++)
                        {
                            var svc = webservices.ValueAt(i);

                            svc.Dispose();
                        }
                    },
                    Lifetime);

                Lifetime.ApplicationStopping.WaitHandle.WaitOne();
            }
        }

        /// 
        /// Runs a number of web services and block until shutdown.
        /// 
        public static void StartWebServer()
        {
            var exit = new ManualResetEventSlim(false);


            // start service instances
            WebService.StartAsync(Cts.Token).GetAwaiter().GetResult();

            // handle SIGTERM and CTRL_C 
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Cts.Cancel(false);
                exit.Set(); // release the Main thread
            };
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Cts.Cancel(false);
                exit.Set(); // release the Main thread
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
            Console.WriteLine("CTRL + C to shut down");

            Lifetime.NotifyStarted();

            // wait on the reset event
            exit.Wait(Cts.Token);

            Lifetime.StopApplication();

            WebService.StopAsync(Cts.Token).GetAwaiter().GetResult();

            Lifetime.NotifyStopped();
        }

        public static X509Certificate2 BuildSelfSignedCertificate(string dns, string ipaddr, string issuer, string password)
        {
            SubjectAlternativeNameBuilder sanb = new SubjectAlternativeNameBuilder();
            sanb.AddIpAddress(IPAddress.Parse(ipaddr));
            sanb.AddDnsName(dns);

            X500DistinguishedName subject = new X500DistinguishedName($"CN={issuer}");

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(new OidCollection {new Oid("1.3.6.1.5.5.7.3.1")}, false));

                request.CertificateExtensions.Add(sanb.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                certificate.FriendlyName = issuer;

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            }
        }

        //
        // encrypt / decrypt
        //

        public static string Encrypt(string v)
        {
            byte[] bytebuf = Encoding.ASCII.GetBytes(v);
            int count = bytebuf.Length;
            int mask = cipher;
            int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
            char[] charbuf = new char[count * 2]; // the target
            int p = 0;
            for (int i = 0; i < count; i++)
            {
                // masking
                int b = bytebuf[i] ^ masks[i % 4];

                //transform
                charbuf[p++] = HEX[(b >> 4) & 0x0f];
                charbuf[p++] = HEX[(b) & 0x0f];

                // reordering
            }

            return new string(charbuf, 0, charbuf.Length);
        }

        public static string Encrypt<P>(P prin, byte proj) where P : IData
        {
            JsonContent cnt = new JsonContent(true, 4096);
            try
            {
                cnt.Put(null, prin, proj);
                byte[] bytebuf = cnt.ByteBuffer;
                int count = cnt.Size;

                int mask = cipher;
                int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
                char[] charbuf = new char[count * 2]; // the target
                int p = 0;
                for (int i = 0; i < count; i++)
                {
                    // masking
                    int b = bytebuf[i] ^ masks[i % 4];

                    //transform
                    charbuf[p++] = HEX[(b >> 4) & 0x0f];
                    charbuf[p++] = HEX[(b) & 0x0f];

                    // reordering
                }

                return new string(charbuf, 0, charbuf.Length);
            }
            finally
            {
                // return pool
                BufferUtility.Return(cnt);
            }
        }

        public static P Decrypt<P>(string token) where P : IData, new()
        {
            int mask = cipher;
            int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
            int len = token.Length / 2;
            var str = new Text(1024);
            int p = 0;
            for (int i = 0; i < len; i++)
            {
                // TODO reordering

                // transform to byte
                int b = (byte) (Dv(token[p++]) << 4 | Dv(token[p++]));
                // masking
                str.Accept((byte) (b ^ masks[i % 4]));
            }

            // deserialize
            try
            {
                JObj jo = (JObj) new JsonParser(str.ToString()).Parse();
                P prin = new P();
                prin.Read(jo, 0xff);
                return prin;
            }
            catch
            {
                return default;
            }
        }

        public static string Decrypt(string v)
        {
            int mask = cipher;
            int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
            int len = v.Length / 2;
            var str = new Text(1024);
            int p = 0;
            for (int i = 0; i < len; i++)
            {
                // TODO reordering

                // transform to byte
                int b = (byte) (Dv(v[p++]) << 4 | Dv(v[p++]));
                // masking
                str.Accept((byte) (b ^ masks[i % 4]));
            }

            return str.ToString();
        }

        // hexidecimal characters
        static readonly char[] HEX = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

        // return digit value
        static int Dv(char hex)
        {
            int v = hex - '0';
            if (v >= 0 && v <= 9)
            {
                return v;
            }

            v = hex - 'A';
            if (v >= 0 && v <= 5) return 10 + v;
            return 0;
        }
    }
}