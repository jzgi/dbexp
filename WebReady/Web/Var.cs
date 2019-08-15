namespace WebReady.Web
{
    /// <summary>
    /// The description of a web variable that propagates into SQL session.
    /// </summary>
    public struct Var
    {
        public string Name { get; internal set; }
    }
}