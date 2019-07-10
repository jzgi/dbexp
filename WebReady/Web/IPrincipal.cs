namespace WebReady.Web
{
    public interface IPrincipal : IData
    {
        string[] Roles { get; }
    }
}