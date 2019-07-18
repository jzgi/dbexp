using WebReady;
using WebReady.Web;

namespace Sample
{
    public class Principal : IPrincipal
    {
        public void Read(ISource s, byte proj = 15)
        {
            throw new System.NotImplementedException();
        }

        public void Write(ISink s, byte proj = 15)
        {
            throw new System.NotImplementedException();
        }

        public string[] Roles { get; }
    }
}