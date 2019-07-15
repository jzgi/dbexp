using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A web resource folder that may contain child folders and actors.
    /// </summary>
    public abstract class WebFolder
    {
        static readonly Exception NotImplemented = new NotImplementedException();

        // child folders
        Map<string, WebFolder> folders;

        // actors 
        Map<string, WebActor> actors;

        Exception except = new Exception();

        public void AddFolder<F>() where F : WebFolder
        {
        }

        public void AddActor<A>() where A : WebActor
        {
        }

        internal async Task HandleAsync(string rsc, WebContext wc)
        {
            try
            {
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

        public virtual void GET(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        public virtual void POST(WebContext wc)
        {
            throw NotImplemented;
        }

        public virtual void PUT(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        public virtual void DELETE(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }
    }
}