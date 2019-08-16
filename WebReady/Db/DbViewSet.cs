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


        readonly Map<string, DbField> columns = new Map<string, DbField>(64);

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

            bool pk = false;
            bool updatable = false;
            bool insertable = false;
            s.Get(nameof(pk), ref pk);
            s.Get(nameof(updatable), ref updatable);
            s.Get(nameof(insertable), ref insertable);

            Updatable = updatable;
            Insertable = insertable;
        }

        public uint Oid => oid;

        internal void AddColumn(DbField field)
        {
            columns.Add(field);
        }

        public override bool Identifiable => columns[0].Key == "id";

        public override async Task OperateAsync(WebContext wc, string method, string[] vars, string subscript)
        {
            var sql = new StringBuilder();

            // set vars as session variables
            for (int i = 0; i < Vars?.Count; i++)
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
            var cnt = new JsonContent(true, 8192);
            if (single)
            {
                cnt.OBJ_();
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i].Value;
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
                    for (int i = 0; i < columns.Count; i++)
                    {
                        var col = columns[i].Value;
                        col.Convert(dc, cnt);
                    }

                    cnt._OBJ();
                }

                cnt._ARR();
            }

            return cnt;
        }

        internal override void Describe(HtmlContent h)
        {
            h.T("<article style=\"border: 1px solid silver; padding: 8px;\">");
            h.T("<header>");
            h.T("<code>").TT(Name);
            h.T("/");
            for (int i = 0; i < Vars.Count; i++)
            {
                h.T("&lt;");
                var var = Vars[i];
                h.T(var.Name);
                h.T("&gt;");
                h.T("/");
            }

            if (Identifiable)
            {
                h.T("[id]");
            }

            h.T("</code>");
            h.T("</header>");

            h.T("<ul>");
            for (int i = 0; i < columns.Count; i++)
            {
                h.T("<li>");
                var col = columns[i].Value;
                h.T(col.Name).T(" ").T(col.Type.Name);
                h.T("</li>");
            }
            h.T("</ul>");

            // methods and roles
            //

            h.T("<ul>");
            for (int i = 0; i < Verbs.Length; i++)
            {
                var verb = Verbs[i];
                if (verb != null)
                {
                    h.T("<li>");
                    h.T(verb.Method);
                    h.T("</li>");
                }
            }

            h.T("</ul>");

            h.T("</article>");
        }
    }
}