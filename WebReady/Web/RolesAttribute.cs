using System;

namespace WebReady.Web
{
    /// <summary>
    /// To implement access roles to the target resources.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RolesAttribute : Attribute
    {
        readonly string[] roles;

        public RolesAttribute(params string[] roles)
        {
            this.roles = roles;
        }

        public string[] Roles => roles;
    }
}