using WebReady;
using WebReady.Web;

namespace Sample
{
    public class SampleService : WebService, IAuthenticate
    {
        protected internal override void OnInitialize()
        {
            LoadSetsFromDb("samp");

            LoadActionsFromDb("samp");
        }

        public bool DoAuthenticate(WebContext wc)
        {
            // try resolve in cookie
            if (wc.Cookies.TryGetValue("Token", out var token))
            {
                wc.Principal = Framework.Decrypt<User>(token);
                return true;
            }

            return false;
        }
    }
}