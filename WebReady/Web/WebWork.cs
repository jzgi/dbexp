using System.Reflection;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// An executable work object for logic processing, it consists of a number of actions.
    /// </summary>
    public abstract class WebWork : WebController
    {
        readonly string[] _roles;

        readonly Map<string, WebAction> _actions = new Map<string, WebAction>(32);

        protected WebWork()
        {
            var typ = GetType();

            _roles = ((RolesAttribute) typ.GetCustomAttribute(typeof(RolesAttribute), true))?.Roles;


            // Gather method-based actions
            //
            foreach (var mi in typ.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                // Verify the return type
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

        public string[] Roles => _roles;

        public Map<string, WebAction> Actions => _actions;


        bool DoAuthorize(WebContext wc, out int code)
        {
            code = 0;
            if (_roles == null) return true;

            var prin = wc.Principal;
            if (prin == null)
            {
                code = 401; // Unauthorized
                return false;
            }

            for (int i = 0; i < _roles.Length; i++)
            {
                if (prin.IsRole(_roles[i])) return true;
            }

            code = 403; // Forbidden
            return false;
        }

        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            if (!DoAuthorize(wc, out var code))
            {
                throw new WebException
                {
                    Code = code
                };
            }

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