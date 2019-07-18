using WebReady.Web;

namespace WebReady.Db
{
    /// <summary>
    /// A function or procedure that returns a void type, a base type, or composite type.
    /// </summary>
    public class DbProcedure : WebExe
    {
        readonly Map<string, DbCol> args;
    }
}