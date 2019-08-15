using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A handler action.
    /// </summary>
    public abstract class WebAction : IKeyable<string>
    {
        readonly WebWork work;

        readonly string name;

        readonly bool async;

        bool @public;

        readonly List<string> roles = new List<string>(8);

        protected WebAction(WebWork work, string name, bool async)
        {
            this.work = work;
            this.name = name;
            this.async = async;
        }

        internal void AddRole(string optype, string role)
        {
            if (role == "postgres") return;

            if (role == "PUBLIC")
            {
                @public = true;
                return;
            }

            roles.Add(role);
        }

        public WebWork Work => work;

        public string Name => name;

        public bool IsAsync => async;

        public bool IsPublic => @public;

        public IReadOnlyList<string> Roles => roles;


        /// <summary>
        /// 
        /// </summary>
        internal abstract Task ExecuteAsync(WebContext wc);

        public string Key => name;
    }
}