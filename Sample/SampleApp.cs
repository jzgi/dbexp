using System.Threading.Tasks;
using WebReady;

namespace Sample
{
    public class SampleApp : Framework
    {
        public static async Task Main(string[] args)
        {
            var rootdir = MakeService<SampleService>("samp");

            rootdir.LoadFromDb("postgres");

            await StartAsync();
        }
    }
}