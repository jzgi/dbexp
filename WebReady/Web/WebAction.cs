using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A handler action.
    /// </summary>
    public abstract class WebAction : IKeyable<string>
    {
        readonly WebWork _work;

        readonly string _name;

        readonly bool _async;

        readonly List<string> _roles = new List<string>(8);

        protected WebAction(WebWork work, string name, bool async)
        {
            _work = work;
            _name = name;
            _async = async;
        }

        internal void AddOp(string optype, string role)
        {
            _roles.Add(role);
        }

        public WebWork Work => _work;

        public string Name => _name;

        public bool IsAsync => _async;


        /// <summary>
        /// 
        /// </summary>
        internal abstract Task ExecuteAsync(WebContext wc);

        public string Key => _name;
    }
}