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
        Setting[] settings;

        // supported method operations
        readonly Verb[] verbs =
        {
            new Verb(GET),
            new Verb(POST),
            new Verb(PUT),
            new Verb(DELETE),
        };

        public Setting[] Settings => settings;

        public Verb[] Verbs => verbs;

        public void AddSetting(string name)
        {
            settings = settings.AddOf(new Setting
            {
                Name = name
            });
        }

        public void AddOpRole(string optype, string role)
        {
            if (optype == null || role == null) return;

            // ignore system role
            if (role == "postgres") return;

            if (optype == GET || optype == "SELECT")
            {
                verbs[0].AddRole(role);
            }
            else if (optype == POST || optype == "INSERT")
            {
                verbs[1].AddRole(role);
            }
            else if (optype == PUT || optype == "UPDATE")
            {
                verbs[2].AddRole(role);
            }
            else if (optype == DELETE)
            {
                verbs[3].AddRole(role);
            }
        }


        bool Authorize(WebContext wc)
        {
            throw new NotImplementedException();
        }

        protected internal override async Task HandleAsync(string rsc, WebContext wc)
        {
            string[] vars = null;
            if (settings != null)
            {
                vars = new string[settings.Length];

                int p = 0;
                int slash = 0;
                for (int i = 0; i < settings.Length; i++)
                {
                    slash = slash = rsc.IndexOf('/', slash);
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

        internal override void Describe(HtmlContent h)
        {
            h.T("<article style=\"border: 1px solid silver; padding: 8px;\">");
            h.T("<h3><code>").TT(Name).T("</code></h3>");
            h.HR();
            h.T("</article>");
        }
    }
}