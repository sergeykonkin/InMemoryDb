using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;

namespace InMemoryDb
{
    /// <summary>
    /// Provides functionality to continuously and incrementally read data from origin data source and map records to <typeparamref name="TValue" /> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    internal sealed class ContinuousReader<TValue>
    {
        private readonly Type _type;

        private readonly string _connectionString;
        private readonly string _rowVersionColumnName;
        private readonly string _deletedColumnName;
        private readonly int _commandTimeout;
        private readonly int _batchSize;
        private readonly string _tableName;
        private readonly int _delay;
        private readonly TaskCompletionSource<bool> _initialReadFinishedSource;

        private bool _isInitialReadFinished;

        /// <summary>
        /// Initializes new instance of <see cref="ContinuousReader{TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        /// <param name="rowVersionColumnName">The name of RowVersion (Timestamp) column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value. Can be null if deletion handling is not required.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        public ContinuousReader(
            string connectionString,
            string tableName,
            string rowVersionColumnName,
            string deletedColumnName,
            int commandTimeout,
            int batchSize,
            int delay)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _rowVersionColumnName = rowVersionColumnName ?? throw new ArgumentNullException(nameof(rowVersionColumnName));
            _deletedColumnName = deletedColumnName;
            if (commandTimeout < 0) throw new ArgumentOutOfRangeException(nameof(commandTimeout));
            _commandTimeout = commandTimeout;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            _batchSize = batchSize;

            _type = typeof(TValue);
            _tableName = tableName ?? GetTableNameFromAttribute(_type) ?? _type.Name;

            if (delay <= 0) throw new ArgumentOutOfRangeException(nameof(delay));
            _delay = delay;
            _initialReadFinishedSource = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished()
        {
            return _initialReadFinishedSource.Task;
        }

        /// <summary>
        /// Occurs when new value was read from origin data source.
        /// </summary>
        public event Action<TValue> NewValue;

        /// <summary>
        /// Occurs when value has been deleted from origin data source.
        /// </summary>
        public event Action<TValue> DeletedValue;

        /// <summary>
        /// Starts data reading process.
        /// </summary>
        public void Start()
        {
            Task.Run(async () =>
            {
                var since = 0ul;
                while (true)
                {
                    var batch = ReadNextBatch(since);
                    if (batch.Count == 0)
                    {
                        if (!_isInitialReadFinished)
                        {
                            _isInitialReadFinished = true;
                            _initialReadFinishedSource.SetResult(true);
                        }

                        await Task.Delay(_delay);
                    }

                    foreach (var tuple in batch)
                    {
                        var value = tuple.Item1;
                        var isDeleted = tuple.Item2;
                        var rowVersion = tuple.Item3;

                        if (isDeleted)
                        {
                            DeletedValue?.Invoke(value);
                        }
                        else
                        {
                            NewValue?.Invoke(value);
                        }

                        if (rowVersion.CompareTo(since) > 0)
                        {
                            since = rowVersion;
                        }
                    }
                }
            });
        }

        private List<Tuple<TValue, bool, ulong>> ReadNextBatch(ulong since)
        {
            var table = EncodeSqlObjectName(_tableName);
            var isDeletedColumn = EncodeSqlObjectName(_deletedColumnName);
            var rowVersionColumn = EncodeSqlObjectName(_rowVersionColumnName);

            var columns = GetPropNamesCached();

            var commandText =
                $@"
SELECT TOP (@batchSize)
    {string.Join(",",columns)},
    {isDeletedColumn},
    {rowVersionColumn}
FROM {table}
WHERE {rowVersionColumn} > @rowVersion
ORDER BY {rowVersionColumn} ASC";

            using (var conn = new SqlConnection(_connectionString))
            {
                try
                {
                    return conn.Query<TValue, bool, byte[], Tuple<TValue, bool, ulong>>(
                            commandText,
                            (value, isDel, rowVersionBytes) => new Tuple<TValue, bool, ulong>(value, isDel, ConvertToUInt64(rowVersionBytes)),
                            new {batchSize = _batchSize, rowVersion = ConvertToBytes(since)},
                            commandTimeout: _commandTimeout,
                            splitOn: $"{_deletedColumnName},{_rowVersionColumnName}")
                        .ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private static IDictionary<Type, List<string>> _propNamesCache = new ConcurrentDictionary<Type, List<string>>();

        private IEnumerable<string> GetPropNamesCached()
        {
            if (_propNamesCache.ContainsKey(_type))
                return _propNamesCache[_type];

            var propNames = _type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(prop => prop.Name)
                .ToList();

            _propNamesCache.Add(_type, propNames);
            return propNames;
        }

        private static string EncodeSqlObjectName(string name)
        {
            var split = name.Split('.');
            var endoded = split.Select(p => p.Trim('[', ']', ' ')).Select(x => $"[{x}]");
            return string.Join(".", endoded);
        }

        private string GetTableNameFromAttribute(Type type)
        {
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr == null)
                return null;

            return (tableAttr.Schema == null ? "" : tableAttr.Schema + ".")
                   + tableAttr.Name;
        }

        private static ulong ConvertToUInt64(byte[] bytes)
        {
            var input = new byte[bytes.Length];
            bytes.CopyTo(input, 0);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(input);
            }

            return BitConverter.ToUInt64(input, 0);
        }

        private static byte[] ConvertToBytes(ulong uint64)
        {
            byte[] result = BitConverter.GetBytes(uint64);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(result);
            }

            return result;
        }
    }
}
