using System;
using System.Security.Authentication;

namespace WebReady.Web
{
    /// <summary>
    /// An executable object for logic handling.
    /// </summary>
    public abstract class WebWork
    {
        static readonly Exception NotImplemented = new NotImplementedException();

        string key;

        // granted roles
        string[] roles;

        public WebDirectory Directory { get; internal set; }

        protected internal virtual void OnInitialize()
        {
        }

        internal bool DoAuthorize(WebContext wc)
        {
            if (roles == null) return true;

            var prin = wc.Principal;
            if (prin == null)
            {
                throw new AuthenticationException();
            }

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