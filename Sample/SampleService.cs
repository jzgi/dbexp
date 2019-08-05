using WebReady.Web;

namespace Sample
{
    public class SampleService : WebService
    {
        protected internal override void OnInitialize()
        {
            LoadScopesFromDb("samp");
            
            LoadActionsFromDb("samp");
        }
    }
}