using System;

namespace WebReady.Db
{
    /// <summary>
    /// A database column or argument field. 
    /// </summary>
    public class DbCol : IKeyable<string>
    {
        public DbType Type { get; }

        readonly string name;

        readonly uint typoid;

        readonly bool def;

        readonly bool notnull;

        bool arguable;

        internal DbCol(DbContext s)
        {
            s.Get(nameof(name), ref name);

            s.Get(nameof(typoid), ref typoid);

            s.Get(nameof(def), ref def);

            s.Get(nameof(notnull), ref notnull);
        }

        public string Key => name;

        public string Name => name;
    }
}