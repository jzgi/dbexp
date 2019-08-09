using System;
using System.Reflection;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A class method-based action.
    /// </summary>
    public class MethodAction : WebAction
    {
        readonly string[] _roles;

        // 2 possible forms of action methods
        //

        readonly Action<WebContext> _do;

        readonly Func<WebContext, Task> _doAsync;

        internal MethodAction(WebWork work, MethodInfo mi, bool async) : base(work, mi.Name, async)
        {
            _roles = ((RolesAttribute) mi.GetCustomAttribute(typeof(RolesAttribute), true))?.Roles;

            // create a doer delegate
            if (async)
            {
                _doAsync = (Func<WebContext, Task>) mi.CreateDelegate(typeof(Func<WebContext, Task>), work);
            }
            else
            {
                _do = (Action<WebContext>) mi.CreateDelegate(typeof(Action<WebContext>), work);
            }
        }

        public string[] Roles => _roles;

        internal override async Task ExecuteAsync(WebContext wc)
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

            if (IsAsync)
            {
                await _doAsync(wc);
            }
            else
            {
                _do(wc);
            }
        }
    }
}