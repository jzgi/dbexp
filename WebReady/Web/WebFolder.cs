using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A web resource directory.
    /// </summary>
    public abstract class WebFolder : WebActor
    {
        // sub folders
        Map<string, WebFolder> folders;

        // actors 
        Map<string, WebActor> actors;

        Exception except = new Exception();

        public void AddSub(WebFolder sub)
        {
        }

        internal async Task HandleAsync(string rsc, WebContext wc)
        {
            try
            {
                if (!DoAuthorize(wc)) throw except;

                int slash = rsc.IndexOf('/');
                // determine sub-dicrectory or end action
                if (slash != -1)
                {
                    string key = rsc.Substring(0, slash);
                    if (folders != null && folders.TryGet(key, out var wrk))
                    {
                        await wrk.HandleAsync(rsc.Substring(slash + 1), wc);
                    }
                    else
                    {
                        var act = actors[rsc];
                        if (act == null)
                        {
                            wc.Give(404, "action not found", true, 12);
                            return;
                        }

                        wc.Actor = act;
                    }
                }
            }
            finally
            {
            }
        }
    }
}