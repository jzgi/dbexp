using WebReady;

namespace Sample
{
    public class SampleApp : Framework
    {
        public static void Main(string[] args)
        {
            MakeService<SampleService>("samp");
        }
    }
}