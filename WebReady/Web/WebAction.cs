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

        readonly List<string> _roles = new List<string>(8);

        protected WebAction(WebWork work, string name, bool async)
        {
            this.work = work;
            this.name = name;
            this.async = async;
        }

        internal void AddOp(string optype, string role)
        {
            _roles.Add(role);
        }

        public WebWork Work => work;

        public string Name => name;

        public bool IsAsync => async;


        /// <summary>
        /// 
        /// </summary>
        internal abstract Task ExecuteAsync(WebContext wc);

        public string Key => name;
    }
}