using static System.StringComparison;

namespace WebReady.Web
{
    public class WebOp
    {
        readonly string _method;

        // granted roles

        bool _public; // PUBLIC role

        string[] _roles; // specific roles

        internal WebOp(string method)
        {
            _method = method;
        }

        public string Method => _method;

        public string[] Roles => _roles;

        internal void AddRole(string r)
        {
            _roles = _roles.AddOf(r);
        }

        internal void AddRole(string[] r)
        {
            _roles = _roles.AddOf(r);
        }
    }
}