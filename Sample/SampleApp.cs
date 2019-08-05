using System.Threading.Tasks;
using WebReady;

namespace Sample
{
    public class SampleApp : Framework
    {
        public static async Task Main(string[] args)
        {
            CreateService<SampleService>("samp");

            await StartAsync();
        }
    }
}