using System;

namespace WebReady.Db
{
    public class DbType : IKeyable<uint>
    {
        // system base types
        //

        public static readonly Map<uint, DbType> BASE = new Map<uint, DbType>()
        {
            new DbType(16, "bool")
            {
                Converter = (name, src, snk) =>
                {
                    bool v = false;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(17, "bytea")
            {
                Converter = (name, src, snk) =>
                {
                    ArraySegment<byte> v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(18, "char")
            {
                Converter = (name, src, snk) =>
                {
                    char v = (char) 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(20, "int8")
            {
                Converter = (name, src, snk) =>
                {
                    long v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(21, "int2")
            {
                Converter = (name, src, snk) =>
                {
                    short v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(23, "int4")
            {
                Converter = (name, src, snk) =>
                {
                    int v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(25, "text")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(114, "json")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(142, "xml")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(790, "money")
            {
                Converter = (name, src, snk) =>
                {
                    decimal v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(701, "float8")
            {
                Converter = (name, src, snk) =>
                {
                    double v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1043, "varchar")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1082, "date")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1083, "time")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1114, "timestamp")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1184, "timestamptz")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1266, "timetz")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1700, "numeric")
            {
                Converter = (name, src, snk) =>
                {
                    decimal v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(2950, "uuid")
            {
                Converter = (name, src, snk) =>
                {
                    Guid v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(3802, "jsonb")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
        };

        readonly uint _oid;

        readonly string _name;

        internal DbType(uint oid, string name)
        {
            _oid = oid;
            _name = name;
        }

        public Action<string, ISource, ISink> Converter { get; private set; }

        public uint Key => _oid;
    }
}