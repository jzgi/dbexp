namespace WebReady.Db
{
    /// <summary>
    /// An argument for database object such as function and procedure.
    /// </summary>
    public class DbCol : IKeyable<string>
    {
        readonly string column_name;

        readonly string ordinal_position;

        readonly string column_default;

        readonly bool is_nullable;

        readonly string data_type;

        readonly bool is_updatable;

        bool arguable;

        internal DbCol(ISource s)
        {
            s.Get(nameof(column_name), ref column_name);

            s.Get(nameof(ordinal_position), ref ordinal_position);

            s.Get(nameof(column_default), ref column_default);

            s.Get(nameof(is_nullable), ref is_nullable);

            s.Get(nameof(data_type), ref data_type);

            s.Get(nameof(is_updatable), ref is_updatable);
        }

        public string Key => column_name;
    }
}