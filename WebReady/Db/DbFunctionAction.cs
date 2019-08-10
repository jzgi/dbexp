using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbFunctionAction : WebAction
    {
        public DbSource Source { get; internal set; }

        int rettype;

        readonly string specific_name; // for overloadied

        readonly string data_type;

        readonly string type_udt_name;

        readonly bool is_deterministic;

        readonly string security_type;

        readonly Map<string, DbArg> _args = new Map<string, DbArg>(16);

        Map<string, DbCol> _cols;


        internal DbFunctionAction(WebWork work, string name, ISource s) : base(work, name, true)
        {
            s.Get(nameof(specific_name), ref specific_name);
            s.Get(nameof(data_type), ref data_type);
            s.Get(nameof(type_udt_name), ref type_udt_name);
            s.Get(nameof(is_deterministic), ref is_deterministic);
            s.Get(nameof(security_type), ref security_type);
        }

        internal void AddArg(DbArg arg)
        {
            _args.Add(arg);
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
                dc.Sql("SELECT ").T(Name).T("(");
                for (int i = 0; i < _args.Count; i++)
                {
                    var arg = _args.ValueAt(i);
                    arg.ImportArg(src);
                }
            }
        }

        public string SpecificName => specific_name;
    }
}