using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A collection of resources, implementing RESTful method handling.
    /// </summary>
    public abstract class WebSet : WebScope
    {
        // subscoping variables
        Var[] _vars;

        // supported method operations
        Op[] _ops;


        public Var[] Vars => _vars;

        public Op[] Ops => _ops;

        public void AddVar(string name)
        {
            _vars = _vars.AddOf(new Var
            {
                Name = name
            });
        }

        public void AddOp(string method, string[] grents)
        {
            _ops = _ops.AddOf(new Op
            {
                Method = method, Roles = grents
            });
        }

        public override bool Authorize(WebContext wc)
        {
            throw new NotImplementedException();
        }

        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            string[] vars = null;
            if (_vars != null)
            {
                vars = new string[_vars.Length];
                // get vars from rsc path
                int slash = 0;
                int p = 0;
                int level = 0;
                while ((slash = rsc.IndexOf('/')) != -1)
                {
                    vars[level] = rsc.Substring(p, slash - p);
                    level++;
                    p = slash + 1;
                }
            }

            await OperateAsync(wc, wc.Method, vars, null);
        }

        //
        // restful methods
        //
        public abstract Task OperateAsync(WebContext wc, string method, string[] vars, string subscript);
    }

    public struct Var
    {
        public string Name { get; internal set; }
    }

    public struct Op
    {
        public string Method { get; internal set; }

        public string[] Roles { get; internal set; }
    }
}