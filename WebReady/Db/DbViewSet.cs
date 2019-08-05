using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewSet : WebSet
    {
        // columns in the view
        public DbSource Source { get; internal set; }

        string _name;

        string _definition;

        string _check_option;

        bool _updatable;

        bool _insertable;

        Map<string, DbCol> _cols = new Map<string, DbCol>(64);

        internal DbViewSet(string name, string definition, string checkOption, bool updatable, bool insertable)
        {
            _name = name;
            _definition = definition;
            _check_option = checkOption;
            _updatable = updatable;
            _insertable = insertable;
        }


        public override async Task OperateAsync(WebContext wc, string method, string[] vars, string subscript)
        {
            if (method == "GET")
            {
                var sql = new DbSql("SELECT * FROM ").T(Name);

                using (var dc = Source.NewDbContext())
                {
                    await dc.QueryAsync();
                }
            }
            else if (method == "POST")
            {
            }
        }
    }
}