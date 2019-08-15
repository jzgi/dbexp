using System.Collections.Generic;

namespace WebReady.Web
{
    public class Verb
    {
        // http method
        readonly string method;

        // database operation
        readonly string op;

        // is the PUBLIC role
        bool @public;

        // specific roles
        readonly List<string> roles = new List<string>(8);

        internal Verb(string method, string op)
        {
            this.method = method;
            this.op = op;
        }

        public string Method => method;

        public string Op => op;

        public bool IsPublic => @public;

        public IReadOnlyList<string> Roles => roles;

        internal void AddRole(string role)
        {
            if (role == "PUBLIC")
            {
                @public = true;
            }
            else
            {
                if (!roles.Contains(role))
                {
                    roles.Add(role);
                }
            }
        }
    }
}