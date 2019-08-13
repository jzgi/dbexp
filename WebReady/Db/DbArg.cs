using System;

namespace WebReady.Db
{
    public class DbArg : IKeyable<string>
    {
        public Type Type { get; }

        public DbType DbType { get; }

        readonly char _mode;

        readonly string _name;

        readonly uint _type;

        readonly string _def;

        public string Key => _name;

        public DbArg(char mode, string name, uint type, string def)
        {
            _mode = mode;
            _name = name;
            _type = type;
            _def = def;
        }


        internal void ImportArg(ISource s)
        {
        }
    }
}