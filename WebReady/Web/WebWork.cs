using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// An executable work object for logic processing, it consists of a number of actions.
    /// </summary>
    public abstract class WebWork : WebController
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
                WebAction action;
                if (pis.Length == 1 && pis[0].ParameterType == typeof(WebContext))
                {
                    action = new MethodAction(this, mi, async);
                }
                else continue;

                _actions.Add(action.Name, action);
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

        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            // resolve the resource
            string name = rsc;
            var act = _actions[name];
            if (act == null)
            {
                wc.Give(404, "Action not found: " + name, shared: true, maxage: 12);
                return;
            }

            if (!((WebService) Parent).TryGiveFromCache(wc))
            {
                // invoke action method 
                await act.ExecuteAsync(wc);

                ((WebService) Parent).TryAddToCache(wc);
            }
        }
    }
}