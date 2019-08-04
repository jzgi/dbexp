using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewSet : WebSet
    {
        // columns in the view
        Map<string, DbCol> _cols;

        bool insertable;

        bool updatable;


        public override void Get(WebContext wc, string subscript)
        {
        }

        public override void Post(WebContext wc)
        {
        }

        public override void Put(WebContext wc, string subscript)
        {
        }

        public override void Delete(WebContext wc, string subscript)
        {
        }
    }
}