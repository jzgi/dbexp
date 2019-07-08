using WebCase.Web;

namespace WebCase.Db
{
    /// <summary>
    /// A function or procedure that returns a void type, a base type, or composite type.
    /// </summary>
    public class DbStoredProcAct : WebAct
    {
        readonly Map<string, DbArg> args;
    }
}