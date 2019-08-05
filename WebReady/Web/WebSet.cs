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
        VarDesc[] _vars;

        // supported method operations
        OpDesc[] _ops;


        public VarDesc[] Vars => _vars;

        public OpDesc[] Ops => _ops;

        public void AddVar(string name, string[] grents)
        {
            _vars = _vars.AddOf(new VarDesc
            {
                Name = name
            });
        }

        public void AddOp(string method, string[] grents)
        {
            _ops = _ops.AddOf(new OpDesc
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

    public class VarDesc
    {
        public string Name { get; internal set; }
    }

    public struct OpDesc
    {
        public string Method { get; internal set; }

        public string[] Roles { get; internal set; }
    }
}