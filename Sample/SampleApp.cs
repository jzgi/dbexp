using System.Threading.Tasks;
using WebReady;

namespace Sample
{
    public class SampleApp : Framework
    {
        public static async Task Main(string[] args)
        {
            MakeService<SampleService>("samp");

            await StartAsync();
        }
    }
}