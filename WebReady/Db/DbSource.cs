using System.Data;
using System.Text;

namespace WebReady.Db
{
    public class DbSource : IKeyable<string>
    {
        // IP host or unix domain socket
        readonly string host;

        // IP port
        readonly int port;

        // default database name
        readonly string database;

        readonly string username;

        readonly string password;

        readonly string connstr;

        readonly Map<uint, DbType> composites = new Map<uint, DbType>(64);

        internal DbSource(JObj s)
        {
            s.Get(nameof(host), ref host);
            s.Get(nameof(port), ref port);
            s.Get(nameof(database), ref database);
            s.Get(nameof(username), ref username);
            s.Get(nameof(password), ref password);

            // initialize connection string
            //
            var sb = new StringBuilder();
            sb.Append("Host=").Append(host);
            sb.Append(";Port=").Append(port);
            sb.Append(";Database=").Append(database);
            sb.Append(";Username=").Append(username);
            sb.Append(";Password=").Append(password);
            sb.Append(";Read Buffer Size=").Append(1024 * 32);
            sb.Append(";Write Buffer Size=").Append(1024 * 32);
            sb.Append(";No Reset On Close=").Append(true);

            connstr = sb.ToString();

            // load public composite types
            //
            using (var dc = NewDbContext())
            {
                // composite types
                dc.QueryAll("SELECT t.oid, t.typname, t.typarray FROM pg_type t, pg_class c WHERE t.oid = c.reltype AND c.relkind = 'c'");
                while (dc.Next())
                {
                    dc.Let(out uint oid);
                    dc.Let(out string name);
                    dc.Let(out uint typarray);
                    composites.Add(new DbType(oid, name, typarray)
                    {
                        Converter = (n, src, snk) =>
                        {
                            JObj v = null;
                            src.Get(n, ref v);
                            snk.Put(n, v);
                        }
                    });
                }

                // add columns and the related array type
                int count = composites.Count;
                for (int i = 0; i < count; i++)
                {
                    var comp = composites[i].Value;
                    dc.QueryAll("SELECT attname AS name, atttypid AS typoid, atthasdef AS def, attnotnull AS notnull FROM pg_attribute WHERE attrelid = @1", p => p.Set(comp.Key));
                    while (dc.Next())
                    {
                        comp.AddColumn(new DbField(dc));
                    }

                    // the corresponding array type
                    dc.Query("SELECT oid, typname, typarray FROM pg_type WHERE oid = @1", p => p.Set(comp.arrayoid));
                    dc.Let(out uint oid);
                    dc.Let(out string name);
                    composites.Add(new DbType(oid, name)
                    {
                        ElementType = comp,
                        Converter = (n, src, snk) =>
                        {
                            JArr v = null;
                            src.Get(n, ref v);
                            snk.Put(n, v);
                        }
                    });
                }
            }
        }

        public string Name { get; internal set; }

        public string Key => Name;

        public string ConnectionString => connstr;

        public DbContext NewDbContext(IsolationLevel? level = null)
        {
            var dc = new DbContext(this);
            if (level != null)
            {
                dc.Begin(level.Value);
            }

            return dc;
        }
    }
}