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

        WebFolder parent;

        // child folders
        Map<string, WebFolder> folders;

        // actors 
        Map<string, WebExe> routines;

        Exception except = new Exception();

        protected virtual void Initialize()
        {
        }

        public void MakeFolder<F>(string name) where F : WebFolder, new()
        {
            F folder = new F();
            folder.parent = this;
            folders.Add(name, folder);
        }

        public void MakeRoutine<T>(string name) where T : WebExe, new()
        {
            T exe = new T();
            exe.folder = this;
            routines.Add(name, exe);
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
                        var act = routines[rsc];
                        if (act == null)
                        {
                            wc.Give(404, "action not found", true, 12);
                            return;
                        }

                        wc.Exe = act;
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