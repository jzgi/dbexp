﻿using System;
using System.Threading.Tasks;
using Npgsql.BackendMessages;
using Npgsql.PostgresTypes;
using Npgsql.TypeHandling;
using WebReady;

namespace Npgsql.TypeHandlers
{
    interface IMappedCompositeHandler
    {
        /// <summary>
        /// The CLR type mapped to the PostgreSQL composite type.
        /// </summary>
        Type CompositeType { get; }
    }

    class MappedCompositeHandler<T> : NpgsqlTypeHandler<T>, IMappedCompositeHandler where T : new()
    {
        readonly INpgsqlNameTranslator _nameTranslator;
        readonly NpgsqlConnection _conn;
        readonly UnmappedCompositeHandler _wrappedHandler;

        public Type CompositeType => typeof(T);

        internal MappedCompositeHandler(INpgsqlNameTranslator nameTranslator, PostgresType pgType, NpgsqlConnection conn)
        {
            PostgresType = pgType;
            _nameTranslator = nameTranslator;
            _conn = conn;
            _wrappedHandler = (UnmappedCompositeHandler) new UnmappedCompositeTypeHandlerFactory(_nameTranslator).Create(PostgresType, _conn);
        }

        public override ValueTask<T> Read(NpgsqlReadBuffer buf, int len, bool async, FieldDescription fieldDescription = null)
            => _wrappedHandler.Read<T>(buf, len, async, fieldDescription);

        public override int ValidateAndGetLength(T value, ref NpgsqlLengthCache lengthCache, NpgsqlParameter parameter)
            => _wrappedHandler.ValidateAndGetLength(value, ref lengthCache, parameter);

        public override async Task Write(T value, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter parameter, bool async)
        {
            if (value is JObj v)
            {
                await _wrappedHandler.Write(v, buf, lengthCache, parameter, async);
            }
        }
    }

    /// <summary>
    /// Interface implemented by all mapped composite handler factories.
    /// Used to expose the name translator for those reflecting enum mappings (e.g. EF Core).
    /// </summary>
    public interface IMappedCompositeTypeHandlerFactory
    {
        /// <summary>
        /// The name translator used for this enum.
        /// </summary>
        INpgsqlNameTranslator NameTranslator { get; }
    }

    class MappedCompositeTypeHandlerFactory<T> : NpgsqlTypeHandlerFactory<T>, IMappedCompositeTypeHandlerFactory
        where T : new()
    {
        public INpgsqlNameTranslator NameTranslator { get; }

        internal MappedCompositeTypeHandlerFactory(INpgsqlNameTranslator nameTranslator)
        {
            NameTranslator = nameTranslator;
        }

        internal override NpgsqlTypeHandler Create(PostgresType pgType, NpgsqlConnection conn)
            => new MappedCompositeHandler<T>(NameTranslator, pgType, conn);

        protected override NpgsqlTypeHandler<T> Create(NpgsqlConnection conn)
            => throw new InvalidOperationException($"Expect {nameof(PostgresType)}");
    }
}