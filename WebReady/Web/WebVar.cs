namespace WebReady.Web
{
    /// <summary>
    /// The description of a web variable that propagates into SQL session.
    /// </summary>
    public struct WebVar
    {
        public string Name { get; internal set; }
    }
}