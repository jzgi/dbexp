using System;

namespace WebReady.Db
{
    public interface IDatum
    {
        Type Type { get; }

        string Name { get; }
    }

    public static class DbUtility
    {
        public static Type GetType(string dbtype)
        {
            switch (dbtype)
            {
                case "smallint":
                    return typeof(short);
                case "int":
                    return typeof(int);
                case "bigint":
                    return typeof(long);
                case "varchar":
                    return typeof(short);
                default:
                    break;
            }

            return null;
        }

        public static void SqlParam(this IDatum dat, ISource src, DbContext ps)
        {
            var typ = dat.Type;
            var name = dat.Name;
            if (typ == typeof(int))
            {
                int v = 0;
                src.Get(name, ref v);

                ps.Put(name, v);
            }
        }
    }
}