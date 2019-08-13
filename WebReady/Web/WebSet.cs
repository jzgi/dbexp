using System;
using System.Threading.Tasks;

namespace WebReady.Web
{
    /// <summary>
    /// A collection of resources, implementing RESTful method handling.
    /// </summary>
    public abstract class WebSet : WebController
    {
        public const string GET = "GET", POST = "POST", PUT = "PUT", DELETE = "DELETE";

        // subscoping variables
        WebVar[] _vars;

        // supported method operations
        readonly WebOp[] _ops =
        {
            new WebOp(GET),
            new WebOp(POST),
            new WebOp(PUT),
            new WebOp(DELETE),
        };

        public WebVar[] Vars => _vars;

        public WebOp[] Ops => _ops;

        public void AddVar(string name)
        {
            _vars = _vars.AddOf(new WebVar
            {
                Name = name
            });
        }

        public void AddOp(string optype, string role)
        {
            if (optype == GET || optype == "SELECT")
            {
                _ops[0].AddRole(role);
            }
            else if (optype == POST || optype == "INSERT")
            {
                _ops[1].AddRole(role);
            }
            else if (optype == POST || optype == "UPDATE")
            {
                _ops[2].AddRole(role);
            }
            else if (optype == DELETE)
            {
                _ops[3].AddRole(role);
            }
        }


        bool Authorize(WebContext wc)
        {
            throw new NotImplementedException();
        }

        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            string[] vars = null;
            if (_vars != null)
            {
                vars = new string[_vars.Length];

                int p = 0;
                int slash = 0;
                for (int i = 0; i < _vars.Length; i++)
                {
                    slash = slash = rsc.IndexOf('/', slash);
                    if (slash == -1) break;
                    vars[i] = rsc.Substring(p, slash - p);

                    p = slash + 1;
                }
            }

            // determine current role
            var prin = wc.Principal;

            foreach (var op in _ops)
            {
                foreach (var r in op.Roles)
                {
//                    if (prin.IsRole(r))
//                    {
//                        wc.Role = r;
//                        goto Handle;
//                    }
                }
            }

            Handle:

            await OperateAsync(wc, wc.Method, vars, null);
        }

        //
        // restful methods
        //
        public abstract Task OperateAsync(WebContext wc, string method, string[] vars, string subscript);

        internal override void Describe(HtmlContent h)
        {
            h.T("<article style=\"border: 1px solid silver; padding: 8px;\">");
            h.T("<h3><code>").TT(Name).T("</code></h3>");
            h.HR();
            h.T("</article>");
        }
    }
}