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
        // 2 possible forms of action methods
        //
        
        Action<WebContext> _do;
        
        Func<WebContext, Task> _doAsync;

        internal MethodAction(WebWork work, MethodInfo mi, bool async) : base(work, mi.Name, async)
        {
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

        internal override async Task ExecuteAsync(WebContext wc)
        {
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