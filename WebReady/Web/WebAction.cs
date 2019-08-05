using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A handler action based on declared methods in work class.
    /// </summary>
    public abstract class WebAction
    {
        private WebWork _work;

        private bool _get; // GET or POST

        private Map<string, WebParam> _params;

        readonly string _subscript;


        // 4 possible forms of the action method
        readonly Action<WebContext> @do;
        readonly Func<WebContext, Task> doAsync;
        readonly Action<WebContext, string> do2;
        readonly Func<WebContext, string, Task> do2Async;


        public string Name { get; internal set; }

        public bool IsAsync { get; internal set; }

        public WebWork Work => _work;

        public bool HasSubscript => _subscript != null;

        internal void Do(WebContext wc, string subscript)
        {
            if (HasSubscript)
            {
                do2(wc, subscript);
            }
            else
            {
                @do(wc);
            }
        }

        // invoke the right method
        internal async Task DoAsync(WebContext wc, string subscript)
        {
            if (HasSubscript)
            {
                await do2Async(wc, subscript);
            }
            else
            {
                await doAsync(wc);
            }
        }
    }
}