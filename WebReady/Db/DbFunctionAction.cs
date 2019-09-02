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
        readonly Map<string, DbField> inargs = new Map<string, DbField>(16);

        // TABLE args
        Map<string, DbField> tableargs;


        internal DbFunctionAction(WebWork work, string name, DbContext s) : base(work, name, true)
        {
            s.Get(nameof(oid), ref oid);
            s.Get(nameof(rettype), ref rettype);
            s.Get(nameof(retset), ref retset);

            Type = DbType.GetBaseType(rettype);
            if (Type == null)
            {
//                throw new DbException("Unsupported db type: " + oid);
            }

            char[] proargmodes = null;
            s.Get(nameof(proargmodes), ref proargmodes);

            string[] proargnames = null;
            s.Get(nameof(proargnames), ref proargnames);

            uint[] proargtypes = null;
            s.Get(nameof(proargtypes), ref proargtypes);

            uint[] proallargtypes = null;
            s.Get(nameof(proallargtypes), ref proallargtypes);

            string[] proargdefs = null;
            s.Get(nameof(proargdefs), ref proargdefs);

            var argtypes = proallargtypes ?? proargtypes; // the value of proallargtypes can be null
            for (int i = 0; i < argtypes?.Length; i++)
            {
                if (proargmodes == null || proargmodes[i] == 'i')
                {
                    var arg = new DbField(
                        'i', proargnames[i], argtypes[i], proargdefs?[i] != null
                    );
                    inargs.Add(arg);
                }
                else
                {
                    var arg = new DbField(
                        't', proargnames[i], argtypes[i], proargdefs?[i] != null
                    );
                    if (tableargs == null) tableargs = new Map<string, DbField>(32);
                    tableargs.Add(arg);
                }
            }
        }

        protected internal override void Describe(HtmlContent h)
        {
            h.T("<li>");
            h.T("<code>").TT(Name).T("</code>");
            // arguments
            //
            h.T("(<br>");
            for (var k = 0; k < inargs.Count; k++)
            {
                var fld = inargs[k].Value;
                h.T("&nbsp;&nbsp;&nbsp;&nbsp;").T(fld.Name).T(": ").T(fld.Type.Name).T("<br>");
            }

            h.T(")<br>");

            if (IsPublic)
            {
                h.T("PUBLIC");
            }
            else
            {
                var roles = Roles;
                for (var k = 0; k < roles.Count; k++)
                {
                    if (k > 0)
                    {
                        h.T(", ");
                    }

                    var role = roles[k];
                    h.T(role);
                }
            }

            h.T("</li>");
        }

        internal override async Task ExecuteAsync(WebContext wc)
        {
            ISource src;
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
                for (int i = 0; i < inargs.Count; i++)
                {
                    if (i > 0)
                    {
                        sql.T(", ");
                    }

                    var arg = inargs[i].Value;
                    sql.T("@").T(arg.Name);
                }

                sql.T(");");

                // result
                if (tableargs != null)
                {
                    await dc.QueryAllAsync(p =>
                    {
                        // set parameters
                        for (int i = 0; i < inargs.Count; i++)
                        {
                            var arg = inargs[i].Value;
                            arg.Convert(src, dc);
                        }
                    });

                    var cnt = new JsonContent(true, 32 * 1024);
                    cnt.ARR_();
                    while (dc.Next())
                    {
                        cnt.OBJ_();
                        for (int i = 0; i < tableargs.Count; i++)
                        {
                            var arg = tableargs[i].Value;
                            arg.Convert(dc, cnt);
                        }

                        cnt._OBJ();
                    }

                    cnt._ARR();

                    wc.Give(200, cnt);
                }
                else
                {
                    object obj = await dc.ScalarAsync(p =>
                    {
                        // set parameters
                        for (int i = 0; i < inargs.Count; i++)
                        {
                            var arg = inargs[i].Value;
                            arg.Convert(src, dc);
                        }
                    });

                    wc.Give(200, obj.ToString());
                }
            }
        }
    }
}