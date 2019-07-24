using System;
using WebReady;

namespace Sample
{
    public class User : IPrincipal, IKeyable<string>
    {
        internal string id;
        internal string inf;
        public string credential;
        internal string addr;
        internal string wx; // wexin openid
        internal short ptid;
        internal DateTime created;
        internal string admly;
        internal string orgly;
        internal short orgid;
        internal short status;

        public void Read(ISource s, byte proj = 0x0f)
        {
            s.Get(nameof(id), ref id);
            s.Get(nameof(inf), ref inf);
            s.Get(nameof(credential), ref credential);
            s.Get(nameof(addr), ref addr);
            s.Get(nameof(wx), ref wx);
            s.Get(nameof(ptid), ref ptid);
            s.Get(nameof(created), ref created);
            s.Get(nameof(admly), ref admly);
            s.Get(nameof(orgly), ref orgly);
            s.Get(nameof(orgid), ref orgid);
            s.Get(nameof(status), ref status);
        }

        public void Write(ISink s, byte proj = 0x0f)
        {
            s.Put(nameof(id), id);
            s.Put(nameof(inf), inf);
            s.Put(nameof(credential), credential);
            s.Put(nameof(addr), addr);
            s.Put(nameof(wx), wx);
            s.Put(nameof(ptid), ptid);
            s.Put(nameof(created), created);
            s.Put(nameof(admly), admly);
            s.Put(nameof(orgly), orgly);
            s.Put(nameof(orgid), orgid);
            s.Put(nameof(status), status);
        }


        public string Key => id;

        public override string ToString() => inf;

        public bool IsRole(string role)
        {
            return false;
        }

        public string UserVar => id;

        public string GetRoleVar(string role)
        {
            if (role.StartsWith("ctr") || role.StartsWith("pt"))
            {
                return orgid.ToString();
            }

            return null;
        }
    }
}