using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// Provides functionality to continuously and incrementally read data from origin data source and map records to <typeparamref name="TValue" /> <see cref="T:System.Type" />.
    /// </summary>
    /// <typeparam name="TKey">Type of increment row key.</typeparam>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    public class SqlContinuousReader<TKey, TValue> : IContinuousReader<TValue>
        where TKey : IComparable
        where TValue : new()
    {
        private readonly string _connectionString;
        private readonly int _batchSize;
        private readonly int _delay;
        private readonly int _commandTimeout;
        private readonly TaskCompletionSource<bool> _initialReadFinishedSource;

        private string _table;
        private string _rowKeyColumn;

        /// <summary>
        /// Initializes new instance of <see cref="SqlContinuousReader{TKey,TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        public SqlContinuousReader(
            string connectionString,
            int batchSize = 1000,
            int delay = 200,
            int commandTimeout = 30)
        {
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (delay <= 0) throw new ArgumentOutOfRangeException(nameof(delay));
            if (commandTimeout < 0) throw new ArgumentOutOfRangeException(nameof(commandTimeout));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _batchSize = batchSize;
            _delay = delay;
            _commandTimeout = commandTimeout;
            _initialReadFinishedSource = new TaskCompletionSource<bool>();
            InitialReadFinished += () => { _initialReadFinishedSource.SetResult(true); };
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished()
        {
            return _initialReadFinishedSource.Task;
        }

        /// <inheritdoc />
        /// <summary>
        /// Occurs when new value was read from origin data source.
        /// </summary>
        public event Action<IComparable, TValue> NewValue;

        /// <inheritdoc />
        /// <summary>
        /// Occurs when initial data read is finished.
        /// </summary>
        public event Action InitialReadFinished;

        /// <inheritdoc />
        /// <summary>
        /// Indicates that initial data read is finished and this reader now in polling mode.
        /// </summary>
        public bool IsInitialReadFinished { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// Starts data reading process.
        /// </summary>
        public virtual void Start()
        {
            _table = GetTableName();
            _rowKeyColumn = GetRowKeyColumnName();

            Task.Run(async () =>
            {
                var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TKey rowKey = default(TKey);
                while (true)
                {
                    var results = GetNextBatch(connection, rowKey).ToList();
                    if (results.Count == 0)
                    {
                        if (!IsInitialReadFinished)
                        {
                            IsInitialReadFinished = true;
                            OnInitialReadFinished();
                        }

                        await Task.Delay(_delay);
                        continue;
                    }

                    foreach (var tuple in results)
                    {
                        OnNewValue(tuple.Item1, tuple.Item2);
                    }

                    rowKey = results.Select(t => t.Item1).Max();
                }
            });
        }

        /// <summary>
        /// Rises <see cref="NewValue"/> event.
        /// </summary>
        /// <param name="rowKey">This value row key.</param>
        /// <param name="value">New value.</param>
        protected virtual void OnNewValue(TKey rowKey, TValue value)
        {
            NewValue?.Invoke(rowKey, value);
        }

        /// <summary>
        /// Rises <see cref="InitialReadFinished"/> event.
        /// </summary>
        protected virtual void OnInitialReadFinished()
        {
            InitialReadFinished?.Invoke();
        }

        /// <summary>
        /// Reads next batch from specified row key.
        /// </summary>
        /// <param name="connection">Current SQL connection.</param>
        /// <param name="fromRowKey">Row Key to read from.</param>
        /// <returns>Next batch of values with their row keys.</returns>
        protected virtual IEnumerable<Tuple<TKey, TValue>> GetNextBatch(SqlConnection connection, TKey fromRowKey)
        {
            using (SqlCommand cmd = connection.CreateCommand())
            {
                var tableName = FixSqlObjectName(_table);
                var rowKeyColumn = FixSqlObjectName(_rowKeyColumn);

                cmd.CommandText =
                    $@"
SELECT TOP (@batchSize)
    *
FROM {tableName}
WHERE {rowKeyColumn} > @rowKey
ORDER BY {rowKeyColumn} ASC";

                cmd.CommandTimeout = _commandTimeout;
                cmd.Parameters.Add(new SqlParameter("batchSize", _batchSize));
                cmd.Parameters.Add(new SqlParameter("rowKey", ConvertRowKey(fromRowKey)));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TValue value = ParseRow(reader, out TKey rowKey);
                        yield return new Tuple<TKey, TValue>(rowKey, value);
                    }
                }
            }
        }

        /// <summary>
        /// Parses row data and maps it to <typeparamref name="TValue"/> type.
        /// </summary>
        /// <param name="row">Record with raw data.</param>
        /// <param name="rowKey">Increment row key of this row.</param>
        /// <returns>Data mapped to <typeparamref name="TValue"/> type.</returns>
        protected virtual TValue ParseRow(IDataRecord row, out TKey rowKey)
        {
            var result = new TValue();
            var type = typeof(TValue);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                object value = row[columnName];
                prop.SetValue(result, value);
            }

            rowKey = ConvertRowKey(row[_rowKeyColumn]);
            return result;
        }

        /// <summary>
        /// Converts row key of <typeparamref name="TKey"/> type to some another SQL-compatible type.
        /// </summary>
        /// <param name="rowKey">Row key.</param>
        /// <returns>Converted row key.</returns>
        protected virtual object ConvertRowKey(TKey rowKey)
        {
            return rowKey;
        }

        /// <summary>
        /// Converts raw row key to <typeparamref name="TKey"/> type.
        /// </summary>
        /// <param name="rowKey">Raw row key.</param>
        /// <returns>Row key of <typeparamref name="TKey"/> type.</returns>
        protected virtual TKey ConvertRowKey(object rowKey)
        {
            return (TKey) rowKey;
        }

        /// <summary>
        /// Prepares object name to be used in SQL server.
        /// </summary>
        /// <param name="name">Object name.</param>
        /// <returns>Fixed value.</returns>
        protected virtual string FixSqlObjectName(string name)
        {
            return $"[{name.Trim().Trim('[', ']').Trim()}]";
        }

        /// <summary>
        /// Gets the name of the table to read data from.
        /// </summary>
        /// <returns>Table name.</returns>
        protected virtual string GetTableName()
        {
            Type type = typeof(TValue);
            return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
        }

        /// <summary>
        /// Gets the name of the row key column.
        /// </summary>
        /// <returns>Row key column name.</returns>
        protected virtual string GetRowKeyColumnName()
        {
            var type = typeof(TValue);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var rowKeyProps = props.Where(p => p.GetCustomAttribute<RowKeyAttribute>() != null).ToArray();

            if (rowKeyProps.Length > 1)
            {
                throw new InvalidOperationException("Ambiguous multiple [RowKey] attributes.");
            }

            if (rowKeyProps.Length == 1)
            {
                return rowKeyProps[0].GetCustomAttribute<ColumnAttribute>()?.Name ?? rowKeyProps[0].Name;
            }

            var idProp = props.FirstOrDefault(p => p.Name == "Id");
            if (idProp != null)
            {
                return idProp.Name;
            }

            throw new InvalidOperationException(
                $"Row key column wasn't specified explicitly with [RowKey] attribute and Type {type.Name} has no \"Id\" prop.");
        }
    }
}
