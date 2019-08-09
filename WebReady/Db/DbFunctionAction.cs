using System;
using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbFunctionAction : WebAction
    {
        private Map<string, DbArg> args;

        private int rettype;

        Map<string, DbCol> cols;

        bool is_determinic;


        public DbFunctionAction(WebWork work, string name, DbContext source) : base(work, name, true)
        {
        }

        internal override Task ExecuteAsync(WebContext wc)
        {
            throw new NotImplementedException();
        }
    }
}