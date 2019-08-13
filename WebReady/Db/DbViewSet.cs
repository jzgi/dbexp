using System;
using System.Text;
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

        readonly uint oid;

        readonly string name;

        readonly string definition;

        readonly string check_option;

        readonly bool updatable;

        readonly bool insertable;

        readonly Map<string, DbField> _columns = new Map<string, DbField>(64);

        internal DbViewSet(DbContext s)
        {
            s.Get(nameof(oid), ref oid);
            s.Get(nameof(name), ref name);

            Name = name;

            s.Get(nameof(definition), ref definition);

            const string BEGIN = "current_setting('";
            const string END = "'";
            int p = 0;
            for (;;)
            {
                p = definition.IndexOf(BEGIN, p, StringComparison.CurrentCultureIgnoreCase);
                if (p == -1) break;
                int p2 = definition.IndexOf(END, p + BEGIN.Length, StringComparison.CurrentCultureIgnoreCase);
                if (p2 == -1) break;
                string var_name = definition.Substring(p + BEGIN.Length, p2 - p - BEGIN.Length);
                // create a variable
                AddVar(var_name);
                // adjust position
                p = p2 + END.Length;
            }

            s.Get(nameof(check_option), ref check_option);
            s.Get(nameof(updatable), ref updatable);
            s.Get(nameof(insertable), ref insertable);
        }

        public uint Oid => oid;

        internal void AddColumn(DbField field)
        {
            _columns.Add(field);
        }

        public override async Task OperateAsync(WebContext wc, string method, string[] vars, string subscript)
        {
            var sql = new StringBuilder();

            // set vars as session variables
            for (int i = 0; i < Vars?.Length; i++)
            {
                var v = Vars[i];
                sql.Append("SET ").Append(v.Name).Append(" = @").Append(vars[i]).Append(";");
            }

            if (method == "GET")
            {
                sql.Append("SELECT * FROM ").Append(Name);

                using (var dc = Source.NewDbContext())
                {
                    await dc.QueryAsync(sql.ToString());
                    var cnt = Dump(dc, false);
                    wc.Give(200, cnt);
                }
            }
            else if (method == "POST")
            {
//                sql.T("INSERT INTO ").T(Name);
//                sql._VALUES_()
                JObj f = await wc.ReadAsync<JObj>();
            }
            else if (method == "PUT")
            {
                sql.Append("UPDATE ").Append(Name).Append(" SET ");
//                sql._VALUES_()
            }
            else if (method == "DELETE")
            {
                sql.Append("DELETE FROM ").Append(Name);
//                sql._VALUES_()
                using (var dc = Source.NewDbContext())
                {
                    await dc.QueryAsync();
                }
            }
        }

        JsonContent Dump(DbContext dc, bool single)
        {
            var cnt = new JsonContent(false, 8192);
            if (single)
            {
                cnt.OBJ_();
                for (int i = 0; i < _columns.Count; i++)
                {
                    var col = _columns.ValueAt(i);
                    col.Convert(dc, cnt);
                }

                cnt._OBJ();
            }
            else
            {
                cnt.ARR_();
                while (dc.Next())
                {
                    cnt.OBJ_();
                    for (int i = 0; i < _columns.Count; i++)
                    {
                        var col = _columns.ValueAt(i);
                        col.Convert(dc, cnt);
                    }

                    cnt._OBJ();
                }

                cnt._ARR();
            }

            return cnt;
        }
    }
}