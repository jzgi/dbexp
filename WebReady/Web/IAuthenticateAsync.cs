using System.Threading.Tasks;

namespace WebReady.Web
{
    public interface IAuthenticateAsync
    {
        /// <summary>
        /// The synchronous version of authentication check.
        /// </summary>
        /// <remarks>The method only tries to establish principal identity within current web context, not responsible for any related user interaction.</remarks>
        /// <param name="wc">current web request/response context</param>
        /// <returns>true to indicate the web context should continue with processing; false otherwise</returns>
        Task<bool> DoAuthenticateAsync(WebContext wc);
    }
}