using System;
using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbFunctionAction : WebAction
    {
        int rettype;

        readonly string specific_name; // for overloadied

        readonly string data_type;

        readonly string type_udt_name;

        readonly bool is_deterministic;

        readonly string security_type;

        readonly Map<string, DbArg> _args = new Map<string, DbArg>(16);

        Map<string, DbCol> _cols;


        public DbFunctionAction(WebWork work, string name, DbContext s) : base(work, name, true)
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

        internal override Task ExecuteAsync(WebContext wc)
        {
            throw new NotImplementedException();
        }

        public string SpecificName => specific_name;
    }
}