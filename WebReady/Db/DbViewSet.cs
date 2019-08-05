using System;
using System.Threading.Tasks;
using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewSet : WebSet
    {
        // columns in the view
        Map<string, DbCol> _cols;

        string source;

        bool insertable;

        bool updatable;


        public override async Task OperateAsync(WebContext wc, string method, string[] vars, string subscript)
        {
            if (method == "GET")
            {
                var sql = new DbSql("SELECT * FROM ").T(Name);

                using (var dc = Framework.NewDbContext(source))
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