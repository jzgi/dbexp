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
        Var[] vars;

        // supported method operations
        Op[] ops;


        public Var[] Vars => vars;

        public Op[] Ops => ops;

        public void AddVar(string name)
        {
            vars = vars.AddOf(new Var
            {
                Name = name
            });
        }

        public void AddOp(string method, string[] grents)
        {
            ops = ops.AddOf(new Op
            {
                Method = method, Roles = grents
            });
        }

        public override bool Authorize(WebContext wc)
        {
            throw new NotImplementedException();
        }

        protected override Task HandleAsync(string rsc, WebContext wc)
        {
            throw new NotImplementedException();
        }

        //
        // restful methods
        //
        readonly Exception NotImplemented = new NotImplementedException();

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