using System;
using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// An executable work object for logic processing.
    /// </summary>
    public abstract class WebWork : WebScope
    {
        readonly Map<string, WebAction> _actions = new Map<string, WebAction>(32);

        string[] _roles;
        
        protected void  Initialize(string name)
        {
            var type = GetType();

            // gather method-based actions
            //

            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                // verify the return type
                Type ret = mi.ReturnType;
                bool async;
                if (ret == typeof(Task)) async = true;
                else if (ret == typeof(void)) async = false;
                else continue;

                ParameterInfo[] pis = mi.GetParameters();
                WebAction act;
                if (pis.Length == 1 && pis[0].ParameterType == typeof(WebContext))
                {
                    act = new WebMethodAction(this, mi, async, null);
                }
                else if (pis.Length == 2 && pis[0].ParameterType == typeof(WebContext) && pis[1].ParameterType == typeof(string))
                {
                    act = new WebMethodAction(this, mi, async, pis[1].Name);
                }
                else continue;

                _actions.Add("", act);
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

        protected override Task HandleAsync(string rsc, WebContext wc)
        {
            throw new NotImplementedException();
        }
    }
}