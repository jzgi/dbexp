using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbFunctionAction : WebAction
    {
        public DbSource Source { get; internal set; }

        public DbType Type { get; }

        readonly uint oid;

        readonly bool @volatile;

        readonly uint rettype;

        readonly bool retset;


        readonly Map<string, DbArg> _args = new Map<string, DbArg>(16);

        Map<string, DbCol> _cols;


        internal DbFunctionAction(WebWork work, string name, DbContext s) : base(work, name, true)
        {
            s.Get(nameof(oid), ref oid);
            s.Get(nameof(rettype), ref rettype);
            s.Get(nameof(retset), ref retset);

            string proargmodes = null;
            s.Get(nameof(proargmodes), ref proargmodes);

            string[] proargnames = null;
            s.Get(nameof(proargnames), ref proargnames);

            uint[] proargtypes = null;
            s.Get(nameof(proargtypes), ref proargtypes);

            string[] proargdefs = null;
            s.Get(nameof(proargdefs), ref proargdefs);

            for (int i = 0; i < proargnames?.Length; i++)
            {
                var arg = new DbArg(
                    proargmodes?[i] ?? 'i',
                    proargnames[i],
                    proargtypes[i],
                    proargdefs?[i]
                );
                _args.Add(arg);
            }
        }


        internal override async Task ExecuteAsync(WebContext wc)
        {
            ISource src = null;
            if (wc.IsGet)
            {
                src = wc.Query;
            }
            else
            {
                src = await wc.ReadAsync<JObj>();
            }

            using (var dc = Source.NewDbContext())
            {
                var sql = dc.Sql("SELECT ").T(Name).T("(");
                for (int i = 0; i < _args.Count; i++)
                {
                    var arg = _args.ValueAt(i);
                    arg.ImportArg(src);
                }

                sql.T(");");

                // set parameters
                for (int i = 0; i < _args.Count; i++)
                {
                    var arg = _args.ValueAt(i);
//                    arg.SqlParam(src, dc);
                }

                await dc.ExecuteAsync();
            }
        }
    }
}