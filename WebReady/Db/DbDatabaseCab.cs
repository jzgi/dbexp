using WebCase.Web;

namespace WebCase.Db
{
    public class DbDatabaseCab : WebCab
    {
         Map<string, DbTableCab> tables;

         Map<string, DbViewCab> views;
         
         Map<string, DbTableFuncCab> tablefuncs;
         
         Map<string, DbStoredProcAct> storedprocs;
    }
}