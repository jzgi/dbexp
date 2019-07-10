using WebReady.Web;

namespace WebReady.Db
{
    public class DbDatabaseFolder : WebFolder
    {
         Map<string, DbTableFolder> tables;

         Map<string, DbViewFolder> views;
         
         Map<string, DbTableFuncFolder> tablefuncs;
         
         Map<string, DbStoredProcActor> storedprocs;
    }
}