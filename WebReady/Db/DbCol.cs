using System;

namespace WebReady.Db
{
    /// <summary>
    /// An argument for database object such as function and procedure.
    /// </summary>
    public class DbCol : IKeyable<string>, IDatum
    {
        public Type Type { get; }

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