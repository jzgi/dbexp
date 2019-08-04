using System;
using System.Reflection;

namespace WebReady.Web
{
    /// <summary>
    /// A handler action based on declared methods in work class.
    /// </summary>
    public abstract class WebAction
    {
        private WebWork _work;

        private bool _get; // GET or POST

        private Map<string, WebParam> _params;


      
        public WebWork Work => _work;
    }
}