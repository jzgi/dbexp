using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewSet : WebSet
    {
        // columns in the view
        public DbSource Source { get; internal set; }

        internal readonly string table_name;

        readonly string view_definition;

        readonly string check_option;

        readonly bool is_updateable;

        readonly bool is_insertable_into;

        readonly Map<string, DbCol> _cols = new Map<string, DbCol>(64);

        internal DbViewSet(ISource s)
        {
            s.Get(nameof(table_name), ref table_name);

            Name = table_name;

            s.Get(nameof(view_definition), ref view_definition);
            s.Get(nameof(check_option), ref check_option);
            s.Get(nameof(is_updateable), ref is_updateable);
            s.Get(nameof(is_insertable_into), ref is_insertable_into);
        }

        internal void AddCol(DbCol col)
        {
            _cols.Add(col.Key, col);
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