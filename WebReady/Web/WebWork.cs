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

        readonly Map<string, WebAction> actions = new Map<string, WebAction>(32);

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

                actions.Add(action.Name, action);
            }
        }

        public string[] Roles => _roles;

        public Map<string, WebAction> Actions => actions;


        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            // do access check
            //

            if (_roles != null)
            {
                var prin = wc.Principal;
                if (prin == null)
                {
                    throw new WebException {Code = 401}; // Unauthorized
                }

                for (int i = 0; i < _roles.Length; i++)
                {
                    if (prin.IsRole(_roles[i])) goto Okay;
                }

                throw new WebException {Code = 403}; // Forbidden
            }

            Okay:

            // resolve the resource
            string name = rsc;
            WebAction act;
            if (!actions.TryGetValue(name, out act))
            {
                wc.Give(404, "Action not found: " + name, shared: true, maxage: 12);
                return;
            }

            if (!Service.TryGiveFromCache(wc))
            {
                // invoke action method 
                await act.ExecuteAsync(wc);

                Service.TryAddToCache(wc);
            }
        }

        internal override void Describe(HtmlContent h)
        {
            h.T("<ul>");
            for (int i = 0; i < actions.Count; i++)
            {
                var a = actions[i].Value;
                h.T("<li style=\"border: 1px solid silver; padding: 8px;\">");
                h.T("<em><code>").TT(a.Name).T("</code></em>");
                h.HR();

                if (a.IsPublic)
                {
                    h.T("PUBLIC");
                }
                else
                {
                    var roles = a.Roles;
                    for (var k = 0; k < roles.Count; k++)
                    {
                        if (k > 0)
                        {
                            h.T(", ");
                        }

                        var role = roles[k];
                        h.T(role);
                    }
                }

                h.T("</li>");
            }

            h.T("</ul>");
        }
    }
}