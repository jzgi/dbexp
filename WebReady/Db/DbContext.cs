using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using WebReady.Web;

namespace WebReady.Db
{
    /// <summary>
    /// An environment for database operations. It provides strong-typed reads/writes and lightweight O/R mapping.
    /// </summary>
    public sealed class DbContext : ISource, IParams, IDisposable
    {
        static readonly string[] PARAMS =
        {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16",
            "17", "18", "19", "20", "21", "22", "23", "24", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32"
        };

        static readonly string[] INPARAMS =
        {
            "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16",
            "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "v32"
        };

        readonly NpgsqlConnection _connection;

        readonly NpgsqlCommand _command;

        // generator of sql string, can be null
        DbSql _sql;

        NpgsqlTransaction _transact;

        NpgsqlDataReader _reader;

        bool _multi;

        bool _disposing;

        // current parameter index
        int _paramidx;

        internal DbContext(DbSource db)
        {
            _connection = new NpgsqlConnection(db.ConnectionString);
            _command = new NpgsqlCommand
            {
                Connection = _connection
            };
        }

        void Clear()
        {
            // reader reset
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }

            ordinal = 0;
            // command parameter reset
            _command.Parameters.Clear();
            _paramidx = 0;
        }

        public void Dispose()
        {
            if (!_disposing)
            {
                // indicate disposing the instance 
                _disposing = true;
                // return to chars pool
                if (_sql != null) BufferUtility.Return(_sql);
                // commit ongoing transaction
                if (_transact != null && !_transact.IsCompleted)
                {
                    Clear();
                    _transact.Commit();
                }

                _reader?.Close();
                _command.Transaction = null;
                _connection.Close();
            }
        }

        public void Begin(IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            if (_transact == null)
            {
                _transact = _connection.BeginTransaction(level);
                _command.Transaction = _transact;
            }
        }

        public void Commit()
        {
            if (_transact != null)
            {
                _transact.Commit();
                _command.Transaction = null;
                _transact = null;
            }
        }

        public void Rollback()
        {
            if (_transact != null && !_transact.IsCompleted)
            {
                // indicate disposing the instance 
                Clear();
                _transact.Rollback();
                _command.Transaction = null;
                _transact = null;
            }
        }

        public bool DataSet => _multi;

        public bool NextResult()
        {
            ordinal = 0; // reset column ordinal
            if (_reader == null)
            {
                return false;
            }

            return _reader.NextResult();
        }

        public bool Next()
        {
            ordinal = 0; // reset column ordinal

            if (_reader == null)
            {
                return false;
            }

            return _reader.Read();
        }

        public DbSql Sql(string str)
        {
            if (_sql == null)
            {
                _sql = new DbSql(str);
            }
            else
            {
                _sql.Clear(); // reset
                _sql.Add(str);
            }

            return _sql;
        }

        public void ConstructForWeb(WebController dir)
        {
        }

        public bool Query(Action<IParams> p = null, bool prepare = true)
        {
            return Query(_sql.ToString(), p, prepare);
        }

        public bool Query(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _multi = false;
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            _reader = _command.ExecuteReader();
            return _reader.Read();
        }

        public async Task<bool> QueryAsync(Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _multi = false;
            _command.CommandText = _sql.ToString();
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            _reader = (NpgsqlDataReader) await _command.ExecuteReaderAsync();
            return _reader.Read();
        }

        public async Task<bool> QueryAsync(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _multi = false;
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            _reader = (NpgsqlDataReader) await _command.ExecuteReaderAsync();
            return _reader.Read();
        }

        public D Query<D>(Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (Query(p, prepare))
            {
                return ToObject<D>(proj);
            }

            return default;
        }

        public D Query<D>(string sql, Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (Query(sql, p, prepare))
            {
                return ToObject<D>(proj);
            }

            return default;
        }

        public async Task<D> QueryAsync<D>(Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (await QueryAsync(p, prepare))
            {
                return ToObject<D>(proj);
            }

            return default;
        }

        public async Task<D> QueryAsync<D>(string sql, Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (await QueryAsync(sql, p, prepare))
            {
                return ToObject<D>(proj);
            }

            return default;
        }

        public bool QueryAll(Action<IParams> p = null, bool prepare = true)
        {
            return QueryAll(_sql.ToString(), p, prepare);
        }

        public bool QueryAll(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _multi = true;
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            _reader = _command.ExecuteReader();
            return _reader.HasRows;
        }

        public async Task<bool> QueryAllAsync(Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _multi = true;
            _command.CommandText = _sql.ToString();
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            _reader = (NpgsqlDataReader) await _command.ExecuteReaderAsync();
            return _reader.HasRows;
        }

        public async Task<bool> QueryAllAsync(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _multi = true;
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            _reader = (NpgsqlDataReader) await _command.ExecuteReaderAsync();
            return _reader.HasRows;
        }

        public D[] QueryAll<D>(Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (QueryAll(p, prepare))
            {
                return ToArray<D>(proj);
            }

            return null;
        }

        public D[] QueryAll<D>(string sql, Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (QueryAll(sql, p, prepare))
            {
                return ToArray<D>(proj);
            }

            return null;
        }

        public async Task<D[]> QueryAllAsync<D>(Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (await QueryAllAsync(p, prepare))
            {
                return ToArray<D>(proj);
            }

            return null;
        }

        public async Task<D[]> QueryAllAsync<D>(string sql, Action<IParams> p = null, byte proj = 0x0f, bool prepare = true) where D : IData, new()
        {
            if (await QueryAllAsync(sql, p, prepare))
            {
                return ToArray<D>(proj);
            }

            return null;
        }

        public Map<K, D> QueryAll<K, D>(Action<IParams> p = null, byte proj = 0x0f, Func<D, K> keyer = null, bool prepare = true) where D : IData, new()
        {
            if (QueryAll(p, prepare))
            {
                return ToMap(proj, keyer);
            }

            return null;
        }

        public Map<K, D> QueryAll<K, D>(string sql, Action<IParams> p = null, byte proj = 0x0f, Func<D, K> keyer = null, bool prepare = true) where D : IData, new()
        {
            if (QueryAll(sql, p, prepare))
            {
                return ToMap(proj, keyer);
            }

            return null;
        }

        public async Task<Map<K, D>> QueryAllAsync<K, D>(Action<IParams> p = null, byte proj = 0x0f, Func<D, K> keyer = null, bool prepare = true) where D : IData, new()
        {
            if (await QueryAllAsync(p, prepare))
            {
                return ToMap(proj, keyer);
            }

            return null;
        }

        public async Task<Map<K, D>> QueryAllAsync<K, D>(string sql, Action<IParams> p = null, byte proj = 0x0f, Func<D, K> keyer = null, bool prepare = true) where D : IData, new()
        {
            if (await QueryAllAsync(sql, p, prepare))
            {
                return ToMap(proj, keyer);
            }

            return null;
        }

        public bool HasRows
        {
            get
            {
                if (_reader == null)
                {
                    return false;
                }

                return _reader.HasRows;
            }
        }

        public int Execute(Action<IParams> p = null, bool prepare = true)
        {
            return Execute(_sql.ToString(), p, prepare);
        }

        public int Execute(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            return _command.ExecuteNonQuery();
        }

        public async Task<int> ExecuteAsync(Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _command.CommandText = _sql.ToString();
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            return await _command.ExecuteNonQueryAsync();
        }

        public async Task<int> ExecuteAsync(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            return await _command.ExecuteNonQueryAsync();
        }

        public object Scalar(Action<IParams> p = null, bool prepare = true)
        {
            return Scalar(_sql.ToString(), p, prepare);
        }

        public object Scalar(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            object res = _command.ExecuteScalar();
            return res == DBNull.Value ? null : res;
        }

        public async Task<object> ScalarAsync(Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _command.CommandText = _sql.ToString();
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            return await _command.ExecuteScalarAsync();
        }

        public async Task<object> ScalarAsync(string sql, Action<IParams> p = null, bool prepare = true)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            Clear();
            _command.CommandText = sql;
            _command.CommandType = CommandType.Text;
            if (p != null)
            {
                p(this);
                if (prepare) _command.Prepare();
            }

            return await _command.ExecuteScalarAsync();
        }

        //
        // RESULTSET
        //

        public D ToObject<D>(byte proj = 0x0f) where D : IData, new()
        {
            D obj = new D();
            obj.Read(this, proj);
            return obj;
        }

        public D[] ToArray<D>(byte proj = 0x0f) where D : IData, new()
        {
            ValueList<D> lst = new ValueList<D>(32);
            while (Next())
            {
                D obj = new D();
                obj.Read(this, proj);
                lst.Add(obj);
            }

            return lst.ToArray();
        }

        public Map<K, D> ToMap<K, D>(byte proj = 0x0f, Func<D, K> keyer = null) where D : IData, new()
        {
            Map<K, D> map = new Map<K, D>(64);
            while (Next())
            {
                D obj = new D();
                obj.Read(this, proj);
                K key;
                if (keyer != null)
                {
                    key = keyer(obj);
                }
                else if (obj is IKeyable<K> keyable)
                {
                    key = keyable.Key;
                }
                else
                {
                    throw new FrameworkException("neither keyer nor IKeyable<D>");
                }

                map.Add(key, obj);
            }

            return map;
        }

        // current column ordinal
        int ordinal;

        public bool Get(string name, ref bool v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetBoolean(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref char v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetChar(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref short v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetInt16(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref int v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetInt32(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref long v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetInt64(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref double v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetDouble(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref decimal v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetDecimal(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref DateTime v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetDateTime(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref string v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetString(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref byte[] v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    int len;
                    if ((len = (int) _reader.GetBytes(ord, 0, null, 0, 0)) > 0)
                    {
                        v = new byte[len];
                        _reader.GetBytes(ord, 0, v, 0, len); // read data into the buffer
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref ArraySegment<byte> v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    int len;
                    if ((len = (int) _reader.GetBytes(ord, 0, null, 0, 0)) > 0)
                    {
                        byte[] buf = new byte[len];
                        _reader.GetBytes(ord, 0, buf, 0, len); // read data into the buffer
                        v = new ArraySegment<byte>(buf, 0, len);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref Map<string, string> v)
        {
            throw new NotImplementedException();
        }

        public bool Get<D>(string name, ref D v, byte proj = 0x0f) where D : IData, new()
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    string str = _reader.GetString(ord);
                    JsonParser p = new JsonParser(str);
                    JObj jo = (JObj) p.Parse();
                    v = new D();
                    v.Read(jo, proj);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref JObj v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    string str = _reader.GetString(ord);
                    JsonParser p = new JsonParser(str);
                    v = (JObj) p.Parse();
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref JArr v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    string str = _reader.GetString(ord);
                    JsonParser parser = new JsonParser(str);
                    v = (JArr) parser.Parse();
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref ISource v)
        {
            throw new NotImplementedException();
        }

        public bool Get(string name, ref short[] v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<short[]>(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref int[] v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<int[]>(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref long[] v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<long[]>(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get(string name, ref string[] v)
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<string[]>(ord);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool Get<D>(string name, ref D[] v, byte proj = 0x0f) where D : IData, new()
        {
            try
            {
                int ord = _reader.GetOrdinal(name);
                if (!_reader.IsDBNull(ord))
                {
                    string str = _reader.GetString(ord);
                    JsonParser parser = new JsonParser(str);
                    JArr ja = (JArr) parser.Parse();
                    int len = ja.Count;
                    v = new D[len];
                    for (int i = 0; i < len; i++)
                    {
                        JObj jo = ja[i];
                        D obj = new D();
                        obj.Read(jo, proj);
                        v[i] = obj;
                    }

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }


        //
        // LET
        //

        public ISource Let(out bool v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetBoolean(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = false;
            return this;
        }

        public ISource Let(out char v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetChar(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = '\0';
            return this;
        }

        public ISource Let(out short v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetInt16(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = 0;
            return this;
        }

        public ISource Let(out int v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetInt32(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = 0;
            return this;
        }

        public ISource Let(out long v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetInt64(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = 0;
            return this;
        }

        public ISource Let(out double v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetDouble(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = 0;
            return this;
        }

        public ISource Let(out decimal v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetDecimal(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = 0;
            return this;
        }

        public ISource Let(out DateTime v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetDateTime(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = default;
            return this;
        }

        public ISource Let(out string v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetString(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = null;
            return this;
        }

        public ISource Let(out ArraySegment<byte> v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    int len;
                    if ((len = (int) _reader.GetBytes(ord, 0, null, 0, 0)) > 0)
                    {
                        byte[] buf = new byte[len];
                        _reader.GetBytes(ord, 0, buf, 0, len); // read data into the buffer
                        v = new ArraySegment<byte>(buf, 0, len);
                        return this;
                    }
                }
            }
            catch
            {
            }

            v = default;
            return this;
        }

        public ISource Let(out byte[] v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    int len;
                    if ((len = (int) _reader.GetBytes(ord, 0, null, 0, 0)) > 0)
                    {
                        byte[] buf = new byte[len];
                        _reader.GetBytes(ord, 0, buf, 0, len); // read data into the buffer
                        v = buf;
                        return this;
                    }
                }
            }
            catch
            {
            }

            v = default;
            return this;
        }

        public ISource Let(out short[] v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<short[]>(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = null;
            return this;
        }

        public ISource Let(out int[] v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<int[]>(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = null;
            return this;
        }

        public ISource Let(out long[] v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<long[]>(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = null;
            return this;
        }

        public ISource Let(out string[] v)
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    v = _reader.GetFieldValue<string[]>(ord);
                    return this;
                }
            }
            catch
            {
            }

            v = null;
            return this;
        }

        public ISource Let(out JObj v)
        {
            throw new NotImplementedException();
        }

        public ISource Let(out JArr v)
        {
            throw new NotImplementedException();
        }

        public ISource Let<D>(out D v, byte proj = 0x0f) where D : IData, new()
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    string str = _reader.GetString(ord);
                    JsonParser p = new JsonParser(str);
                    JObj jo = (JObj) p.Parse();
                    v = new D();
                    v.Read(jo, proj);
                    return this;
                }
            }
            catch
            {
            }

            v = default;
            return this;
        }

        public ISource Let<D>(out D[] v, byte proj = 0x0f) where D : IData, new()
        {
            try
            {
                int ord = ordinal++;
                if (!_reader.IsDBNull(ord))
                {
                    string str = _reader.GetString(ord);
                    JsonParser parser = new JsonParser(str);
                    JArr ja = (JArr) parser.Parse();
                    int len = ja.Count;
                    v = new D[len];
                    for (int i = 0; i < len; i++)
                    {
                        JObj jo = ja[i];
                        D obj = new D();
                        obj.Read(jo, proj);
                        v[i] = obj;
                    }

                    return this;
                }
            }
            catch
            {
            }

            v = null;
            return this;
        }

        public void Write<C>(C cnt) where C : IContent, ISink
        {
            int fc = _reader.FieldCount;
            for (int i = 0; i < fc; i++)
            {
                string name = _reader.GetName(i);
                if (_reader.IsDBNull(i))
                {
                    cnt.PutNull(name);
                    continue;
                }

                var typ = _reader.GetFieldType(i);
                if (typ == typeof(bool))
                {
                    cnt.Put(name, _reader.GetBoolean(i));
                }
                else if (typ == typeof(short))
                {
                    cnt.Put(name, _reader.GetInt16(i));
                }
                else if (typ == typeof(int))
                {
                    cnt.Put(name, _reader.GetInt32(i));
                }
                else if (typ == typeof(long))
                {
                    cnt.Put(name, _reader.GetInt64(i));
                }
                else if (typ == typeof(string))
                {
                    cnt.Put(name, _reader.GetString(i));
                }
                else if (typ == typeof(decimal))
                {
                    cnt.Put(name, _reader.GetDecimal(i));
                }
                else if (typ == typeof(double))
                {
                    cnt.Put(name, _reader.GetDouble(i));
                }
                else if (typ == typeof(DateTime))
                {
                    cnt.Put(name, _reader.GetDateTime(i));
                }
            }
        }

        public IContent Dump()
        {
            JsonContent cnt = new JsonContent(false, 8192);
            int fc = _reader.FieldCount;
            while (_reader.Read())
            {
                cnt.ARR_();
                for (int i = 0; i < fc; i++)
                {
                    cnt.OBJ_();
                    var typ = _reader.GetFieldType(i);
                    if (typ == typeof(short))
                    {
                        cnt.Put(_reader.GetName(i), _reader.GetInt16(i));
                    }
                    else if (typ == typeof(int))
                    {
                        cnt.Put(_reader.GetName(i), _reader.GetInt32(i));
                    }
                    else if (typ == typeof(string))
                    {
                        cnt.Put(_reader.GetName(i), _reader.GetString(i));
                    }

                    cnt._OBJ();
                }

                cnt._ARR();
            }

            return cnt;
        }


        //
        // PARAMETERS

        public void PutNull(string name)
        {
            _command.Parameters.AddWithValue(name, DBNull.Value);
        }

        public void Put(string name, JNumber v)
        {
            _command.Parameters.Add(new NpgsqlParameter<decimal>(name, NpgsqlDbType.Numeric)
            {
                TypedValue = v.Decimal
            });
        }

        public void Put(string name, bool v)
        {
            _command.Parameters.Add(new NpgsqlParameter<bool>(name, NpgsqlDbType.Boolean)
            {
                TypedValue = v
            });
        }

        public void Put(string name, char v)
        {
            _command.Parameters.Add(new NpgsqlParameter<char>(name, NpgsqlDbType.Char)
            {
                TypedValue = v
            });
        }

        public void Put(string name, short v)
        {
            _command.Parameters.Add(new NpgsqlParameter<short>(name, NpgsqlDbType.Smallint)
            {
                TypedValue = v
            });
        }

        public void Put(string name, int v)
        {
            _command.Parameters.Add(new NpgsqlParameter<int>(name, NpgsqlDbType.Integer)
            {
                TypedValue = v
            });
        }

        public void Put(string name, long v)
        {
            _command.Parameters.Add(new NpgsqlParameter<long>(name, NpgsqlDbType.Bigint)
            {
                TypedValue = v
            });
        }

        public void Put(string name, double v)
        {
            _command.Parameters.Add(new NpgsqlParameter<double>(name, NpgsqlDbType.Double)
            {
                TypedValue = v
            });
        }

        public void Put(string name, decimal v)
        {
            _command.Parameters.Add(new NpgsqlParameter<decimal>(name, NpgsqlDbType.Money)
            {
                TypedValue = v
            });
        }

        public void Put(string name, DateTime v)
        {
            bool date = v.Hour == 0 && v.Minute == 0 && v.Second == 0 && v.Millisecond == 0;
            _command.Parameters.Add(new NpgsqlParameter<DateTime>(name, date ? NpgsqlDbType.Date : NpgsqlDbType.Timestamp)
            {
                TypedValue = v
            });
        }

        public void Put(string name, string v)
        {
            int len = v?.Length ?? 0;
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Varchar, len)
            {
                Value = (v != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, ArraySegment<byte> v)
        {
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Bytea, v.Count)
            {
                Value = (v.Array != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, byte[] v)
        {
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Bytea, v?.Length ?? 0)
            {
                Value = (v != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, short[] v)
        {
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Smallint)
            {
                Value = (v != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, int[] v)
        {
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Integer)
            {
                Value = (v != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, long[] v)
        {
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Bigint)
            {
                Value = (v != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, string[] v)
        {
            _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = (v != null) ? (object) v : DBNull.Value
            });
        }

        public void Put(string name, JObj v)
        {
            if (v == null)
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = DBNull.Value
                });
            }
            else
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = v.ToString()
                });
            }
        }

        public void Put(string name, JArr v)
        {
            if (v == null)
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = DBNull.Value
                });
            }
            else
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = v.ToString()
                });
            }
        }

        public void Put(string name, IData v, byte proj = 0x0f)
        {
            if (v == null)
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb) {Value = DBNull.Value});
            }
            else
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = DataUtility.ToString(v, proj)
                });
            }
        }

        public void Put<D>(string name, D[] v, byte proj = 0x0f) where D : IData
        {
            if (v == null)
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = DBNull.Value
                });
            }
            else
            {
                _command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
                {
                    Value = DataUtility.ToString(v, proj)
                });
            }
        }

        public void PutFrom(ISource s)
        {
            throw new NotImplementedException();
        }
        //
        // positional
        //

        public IParams SetNull()
        {
            PutNull(PARAMS[_paramidx++]);
            return this;
        }

        public IParams Set(bool v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(char v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(short v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(int v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(long v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(double v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(decimal v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(JNumber v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(DateTime v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(string v)
        {
            if (v == string.Empty)
            {
                v = null;
            }

            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(ArraySegment<byte> v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(byte[] v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(short[] v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(int[] v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(long[] v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(string[] v)
        {
            Put(PARAMS[_paramidx++], v);
            return this;
        }

        public IParams Set(JObj v)
        {
            throw new NotImplementedException();
        }

        public IParams Set(JArr v)
        {
            throw new NotImplementedException();
        }

        public IParams Set(IData v, byte proj = 0x0f)
        {
            Put(PARAMS[_paramidx++], v, proj);
            return this;
        }

        public IParams Set<D>(D[] v, byte proj = 0x0f) where D : IData
        {
            Put(PARAMS[_paramidx++], v, proj);
            return this;
        }

        public IParams SetIn(string[] v)
        {
            for (int i = 0; i < v.Length; i++)
            {
                Put(INPARAMS[i], v[i]);
            }

            return this;
        }

        public IParams SetIn(short[] v)
        {
            for (int i = 0; i < v.Length; i++)
            {
                Put(INPARAMS[i], v[i]);
            }

            return this;
        }

        public IParams SetIn(int[] v)
        {
            for (int i = 0; i < v.Length; i++)
            {
                Put(INPARAMS[i], v[i]);
            }

            return this;
        }

        public IParams SetIn(long[] v)
        {
            for (int i = 0; i < v.Length; i++)
            {
                Put(INPARAMS[i], v[i]);
            }

            return this;
        }
    }
}