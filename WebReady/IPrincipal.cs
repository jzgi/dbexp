namespace WebReady
{
    public interface IPrincipal : IData
    {
        bool IsInRole(string role);
    }
}