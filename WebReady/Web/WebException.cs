using System;

namespace WebReady.Web
{
    ///
    /// Thrown to indicate an error during a web message processing.
    ///
    public class WebException : Exception
    {
        /// <summary>
        /// The status code.
        /// </summary>
        public int Code { get; internal set; }

        public WebException()
        {
        }

        public WebException(string message) : base(message)
        {
        }

        public WebException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}