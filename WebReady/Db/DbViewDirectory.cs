using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewDirectory : WebDirectory
    {
        Map<string, DbCol> cols;

        private bool insertable;

        private bool updatable;
        
        

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