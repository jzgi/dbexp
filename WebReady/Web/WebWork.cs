using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// An executable work object for logic processing, it consists of a number of actions.
    /// </summary>
    public abstract class WebWork : WebScope
    {
        readonly Map<string, WebAction> _actions = new Map<string, WebAction>(32);

        string[] _roles;

        protected WebWork()
        {
            var type = GetType();

            // gather method-based actions
            //
            foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                // verify the return type
                var ret = mi.ReturnType;
                bool async;
                if (ret == typeof(Task))
                {
                    async = true;
                }
                else if (ret == typeof(void))
                {
                    async = false;
                }
                else continue;

                var pis = mi.GetParameters();
                WebAction act;
                if (pis.Length == 1 && pis[0].ParameterType == typeof(WebContext))
                {
                    act = new WebMethodAction(this, mi, async, null);
                }
                else continue;

                _actions.Add(act.Name, act);
            }
        }

        public Map<string, WebAction> Actions => _actions;

        public override bool Authorize(WebContext wc)
        {
            if (_roles == null) return true;

            var prin = wc.Principal;
            if (prin == null)
            {
                throw new AuthenticationException();
            }

            for (int i = 0; i < _roles.Length; i++)
            {
                if (prin.IsRole(_roles[i])) return true;
            }

            return false;
        }

        protected override async Task HandleAsync(string rsc, WebContext wc)
        {
            // resolve the resource
            string name = rsc;
            string subscpt = null;
            int dash = rsc.LastIndexOf('-');
            if (dash != -1)
            {
                name = rsc.Substring(0, dash);
                wc.Subscript = subscpt = rsc.Substring(dash + 1);
            }

            var act = _actions[name];
            if (act == null)
            {
                wc.Give(404, "action not found", true, 12);
                return;
            }

            if (!((WebService) Parent).TryGiveFromCache(wc))
            {
                // invoke action method 
                if (act.IsAsync) await act.DoAsync(wc, subscpt);
                else act.Do(wc, subscpt);

                ((WebService) Parent).TryAddToCache(wc);
            }
        }
    }
}