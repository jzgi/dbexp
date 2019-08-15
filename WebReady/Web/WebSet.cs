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

        bool insertable;

        bool updatable;

        // supported method operations
        readonly Verb[] verbs = new Verb[4]
        {
            new Verb("GET", "SELECT"), null, null, null
        };


        public Setting[] Settings => settings;

        public Verb[] Verbs => verbs;

        public bool PK { get; set; }

        public bool Primary { get; set; }

        public bool Insertable
        {
            get => insertable;
            set
            {
                insertable = value;
                if (verbs[1] == null)
                {
                    verbs[1] = new Verb("POST", "INSERT");
                }
            }
        }

        public bool Updatable
        {
            get => updatable;
            set
            {
                updatable = value;
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


        public void AddSetting(string name)
        {
            settings = settings.AddOf(new Setting
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

            // methods and roles
            //

            h.T("<ul>");
            for (int i = 0; i < verbs.Length; i++)
            {
                var verb = verbs[i];
                if (verb != null)
                {
                    h.T("<li>");
                    h.T(verb.Method);
                    h.T("</li>");
                }
            }
            h.T("</ul>");

            h.T("</article>");
        }
    }
}