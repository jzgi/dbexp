using System;
using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewSet : WebSet
    {
        /// <summary>
        /// The database source that governs this view set.
        /// </summary>
        public DbSource Source { get; internal set; }

        internal readonly string table_name;

        readonly string view_definition;

        readonly string check_option;

        readonly bool is_updatable;

        readonly bool is_insertable_into;

        readonly Map<string, DbCol> cols = new Map<string, DbCol>(64);

        internal DbViewSet(ISource s)
        {
            s.Get(nameof(table_name), ref table_name);

            Name = table_name;

            s.Get(nameof(view_definition), ref view_definition);

            const string BEGIN = "current_setting('";
            const string END = "'";
            int p = 0;
            for (;;)
            {
                p = view_definition.IndexOf(BEGIN, p, StringComparison.CurrentCultureIgnoreCase);
                if (p == -1) break;
                int p2 = view_definition.IndexOf(END, p + BEGIN.Length, StringComparison.CurrentCultureIgnoreCase);
                if (p2 == -1) break;
                string var_name = view_definition.Substring(p + BEGIN.Length, p2 - p - BEGIN.Length);
                // create a variable
                AddVar(var_name);
                // adjust position
                p = p2 + END.Length;
            }

            s.Get(nameof(check_option), ref check_option);
            s.Get(nameof(is_updatable), ref is_updatable);
            s.Get(nameof(is_insertable_into), ref is_insertable_into);
        }

        internal void AddCol(DbCol col)
        {
            cols.Add(col.Key, col);
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