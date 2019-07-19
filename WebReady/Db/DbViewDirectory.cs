using WebReady.Web;

namespace WebReady.Db
{
    public class DbViewDirectory : WebDirectory
    {
        Map<string, DbCol> cols;

        private bool insertable;

        private bool updatable;
        
        

        public override void GET(WebContext wc, string subscript)
        {
        }

        public override void POST(WebContext wc)
        {
        }

        public override void PUT(WebContext wc, string subscript)
        {
        }

        public override void DELETE(WebContext wc, string subscript)
        {
        }
    }
}