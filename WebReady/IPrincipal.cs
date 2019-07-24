namespace WebReady
{
    public interface IPrincipal : IData
    {
        bool IsRole(string role);

        string UserVar { get; }

        string GetRoleVar(string role);
    }
}