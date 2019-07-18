using Microsoft.Extensions.Logging;
using WebReady;
using WebReady.Web;

namespace Sample
{
    public class SampleService : WebService<Principal>
    {
        public SampleService() : base(null, null)
        {
        }

        public SampleService(AppJson.Web cfg, ILoggerProvider logprov) : base(cfg, logprov)
        {
        }
    }
}