using System;

namespace WebReady.Web
{
    /// <summary>
    /// The resouce handling
    /// </summary>
    public abstract class WebActor
    {
        static readonly Exception NotImplemented = new NotImplementedException();

        string key;

        // granted roles
        string[] roles;

        internal bool DoAuthorize(WebContext wc)
        {
            if (roles == null) return true;

            var prin = wc.Principal;
            if (roles.Overlaps(prin.Roles))
            {
                return true;
            }

            return false;
        }

        internal void ProcessRequest(WebContext wc)
        {
        }

        public virtual void GET(WebContext wc)
        {
            throw NotImplemented;
        }

        public virtual void POST(WebContext wc)
        {
            throw NotImplemented;
        }
    }
}