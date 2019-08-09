using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A handler action.
    /// </summary>
    public abstract class WebAction
    {
        readonly WebWork _work;

        readonly string _name;

        readonly bool _async;

        protected WebAction(WebWork work, string name, bool async)
        {
            _work = work;
            _name = name;
            _async = async;
        }

        public WebWork Work => _work;

        public string Name => _name;

        public bool IsAsync => _async;


        /// <summary>
        /// 
        /// </summary>
        internal abstract Task ExecuteAsync(WebContext wc);
    }
}