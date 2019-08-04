using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A collection of resources, implementing RESTful method handling.
    /// </summary>
    public abstract class WebSet : WebScope
    {
        // subscoping variables
        VarDef[] _vars;

        // supported method operations
        OpDef[] _ops;


        internal new void Initialize(WebScope parent, string name)
        {
            base.Initialize(parent, name);
        }

        public VarDef[] Vars => _vars;

        public OpDef[] Ops => _ops;

        public void AddVar(string name, string[] grents)
        {
        }

        public void AddOp(string method, string[] grents)
        {
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

        public virtual void Get(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        public virtual void Post(WebContext wc)
        {
            throw NotImplemented;
        }

        public virtual void Put(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }

        public virtual void Delete(WebContext wc, string subscript)
        {
            throw NotImplemented;
        }
    }

    public struct VarDef
    {
        string name;

        string[] grants;
    }

    public struct OpDef
    {
        string method;

        string[] grants;
    }
}