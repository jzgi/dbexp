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
         bool _async;

         private Func<WebContext, Task> _doAsync;
         
         Action<WebContext> _do;

        internal MethodAction(WebWork work, MethodInfo mi, bool async, string subscript)
        {
            _async = async;

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

    }
}