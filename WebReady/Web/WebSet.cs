using System;
using System.Collections.Generic;
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
        readonly List<Var> varlst = new List<Var>(4);

        bool insertable;

        bool updatable;

        // supported method operations
        readonly Verb[] verbs = new Verb[4]
        {
            new Verb("GET", "SELECT"), null, null, null
        };


        public IReadOnlyList<Var> Vars => varlst;

        public Verb[] Verbs => verbs;

        public abstract bool Identifiable { get; }

        public bool Insertable
        {
            get => insertable;
            set
            {
                insertable = value;
                if (insertable)
                {
                    if (verbs[1] == null)
                    {
                        verbs[1] = new Verb("POST", "INSERT");
                    }
                }
            }
        }

        public bool Updatable
        {
            get => updatable;
            set
            {
                updatable = value;
                if (updatable)
                {
                    if (verbs[2] == null)
                    {
                        verbs[2] = new Verb("PUT", "UPDATE");
                    }

                    if (verbs[3] == null)
                    {
                        verbs[3] = new Verb("DELETE", "DELETE");
                    }
                }
            }
        }


        public void AddVar(string name)
        {
            varlst.Add(new Var
            {
                Name = name
            });
        }

        public void AddRole(string op, string role)
        {
            if (op == null || role == null) return;

            // ignore system role
            if (role == "postgres") return;

            for (int i = 0; i < verbs.Length; i++)
            {
                var verb = verbs[i];
                if (verb.Method == op || verb.Op == op)
                {
                    verb.AddRole(role);
                }
            }
        }


        bool Authorize(WebContext wc)
        {
            throw new NotImplementedException();
        }

        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            string[] vars = null;
            if (this.varlst != null)
            {
                vars = new string[varlst.Count];

                int p = 0;
                int slash = 0;
                for (int i = 0; i < varlst.Count; i++)
                {
                    slash = rsc.IndexOf('/', slash);
                    if (slash == -1) break;
                    vars[i] = rsc.Substring(p, slash - p);

                    p = slash + 1;
                }
            }

            // determine current role
            var prin = wc.Principal;

            foreach (var op in verbs)
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

    }
}