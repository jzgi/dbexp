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


        // IN args
        readonly Map<string, DbField> _inArgs = new Map<string, DbField>(16);

        // TABLE args
        Map<string, DbField> _tableArgs;


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
                var arg = new DbField(
                    proargmodes?[i] ?? 'i',
                    proargnames[i],
                    proargtypes[i],
                    proargdefs?[i] != null
                );
                _inArgs.Add(arg);
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
                for (int i = 0; i < _inArgs.Count; i++)
                {
                    var arg = _inArgs.ValueAt(i);
                    arg.Convert(src, dc);
                }

                sql.T(");");

                // set parameters
                for (int i = 0; i < _inArgs.Count; i++)
                {
                    var arg = _inArgs.ValueAt(i);
//                    arg.SqlParam(src, dc);
                }

                await dc.ExecuteAsync();
            }
        }
    }
}