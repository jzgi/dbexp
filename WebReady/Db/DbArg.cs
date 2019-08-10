namespace WebReady.Db
{
    public class DbArg : IKeyable<string>
    {
        readonly string parameter_mode;

        readonly string parameter_name;

        readonly string data_type;

        readonly string parameter_default;

        public string Key => parameter_name;

        internal DbArg(ISource s)
        {
            s.Get(nameof(parameter_mode), ref parameter_mode);
            s.Get(nameof(parameter_name), ref parameter_name);
            s.Get(nameof(data_type), ref data_type);
            s.Get(nameof(parameter_default), ref parameter_default);
        }
    }
}