using System.Collections.Generic;

namespace WebReady.Web
{
    public class Verb
    {
        // http method
        readonly string method;

        // database operation
        readonly string _optype;

        // is the PUBLIC role
        bool @public;

        // specific roles
        readonly List<string> roles = new List<string>(8);

        internal Verb(string method)
        {
            this.method = method;
        }

        public string Method => method;

        public string OpType => _optype;

        public bool IsPublic => @public;

        public IReadOnlyList<string> Roles => roles;

        internal void AddRole(string r)
        {
            if (r == "PUBLIC")
            {
                @public = true;
            }
            else
            {
                roles.Add(r);
            }
        }
    }
}