using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <summary>
    /// Provides functionality to continuously and incrementally read data from SQL server table and map records to <typeparamref name="TValue" /> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    internal sealed class ContinuousReader<TValue>
        where TValue : new()
    {
        private readonly string _connectionString;
        private readonly string _rowVersionColumnName;
        private readonly string _deletedColumnName;
        private readonly int _commandTimeout;
        private readonly int _batchSize;
        private readonly int _delay;

        private readonly Type _type;
        private readonly string _tableName;

        private bool _isInitialReadFinished;
        private TaskCompletionSource<bool> _initialReadFinishedSource;

        /// <summary>
        /// Initializes new instance of <see cref="ContinuousReader{TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        /// <param name="rowVersionColumnName">The name of RowVersion (Timestamp) column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value.</param>
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
            _tableName = tableName ?? ReflectionHelper.GetTableName(_type);

            if (delay <= 0) throw new ArgumentOutOfRangeException(nameof(delay));
            _delay = delay;
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished(CancellationToken cancellationToken)
        {
            if (_initialReadFinishedSource == null)
            {
                throw new InvalidOperationException("Reading was not started.");
            }

            using (cancellationToken.Register(() => _initialReadFinishedSource.TrySetCanceled()))
            {
                return _initialReadFinishedSource.Task;
            }
        }

        /// <summary>
        /// Starts continuous data reading routine.
        /// </summary>
        public void Start(Action<TValue> onNewValue, Action<TValue> onDeletedValue, CancellationToken cancellationToken, Action<Exception> handleException)
        {
            _initialReadFinishedSource = new TaskCompletionSource<bool>();

            Task.Run(async () =>
            {
                try
                {
                    ulong since = 0;
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batch = await ReadNextBatchAsync(since, cancellationToken);
                        if (batch.Count == 0)
                        {
                            if (!_isInitialReadFinished)
                            {
                                _isInitialReadFinished = true;
                                _initialReadFinishedSource.SetResult(true);
                            }

                            await Task.Delay(_delay, cancellationToken);
                            continue;
                        }

                        foreach (var tuple in batch)
                        {
                            var value = tuple.Item1;
                            var isDeleted = tuple.Item2;

                            if (isDeleted)
                            {
                                onDeletedValue.Invoke(value);
                            }
                            else
                            {
                                onNewValue.Invoke(value);
                            }
                        }

                        since = batch.Max(t => t.Item3);
                    }
                }
                catch (Exception ex)
                {
                    if (!_isInitialReadFinished)
                    {
                        _initialReadFinishedSource.SetException(ex);
                    }

                    handleException?.Invoke(ex);
                }
            }, cancellationToken);
        }

        private async Task<List<Tuple<TValue, bool, ulong>>> ReadNextBatchAsync(ulong since, CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = CreateCommand(connection, since))
            {
                await connection.OpenAsync(cancellationToken);

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    var result = new List<Tuple<TValue, bool, ulong>>();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var value = ParseValue(reader);

                        var isDeleted = (bool)reader[_deletedColumnName];
                        var rowVersion = ConvertToUInt64((byte[])reader[_rowVersionColumnName]);

                        result.Add(new Tuple<TValue, bool, ulong>(value, isDeleted, rowVersion));
                    }

                    return result;
                }
            }
        }

        private SqlCommand CreateCommand(SqlConnection connection, ulong since)
        {
            var table = EncodeSqlObjectName(_tableName);
            var isDeletedColumn = EncodeSqlObjectName(_deletedColumnName);
            var rowVersionColumn = EncodeSqlObjectName(_rowVersionColumnName);

            var typeMap = ReflectionHelper.GetColumnMap(_type);
            var columns = typeMap.Select(t => t.Item2).Select(EncodeSqlObjectName);

            var commandText =
                $@"SELECT TOP (@batchSize)
                       {string.Join(",", columns)},
                       {isDeletedColumn},
                       {rowVersionColumn}
                   FROM {table}
                   WHERE {rowVersionColumn} > @rowVersion
                   ORDER BY {rowVersionColumn} ASC";

            var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandTimeout = _commandTimeout;
            command.Parameters.AddWithValue("batchSize", _batchSize);
            command.Parameters.AddWithValue("rowVersion", ConvertToBytes(since));

            return command;
        }

        private TValue ParseValue(IDataRecord row)
        {
            var value = new TValue();
            foreach (var map in ReflectionHelper.GetColumnMap(_type))
            {
                var member = map.Item1;
                var columnName = map.Item2;
                member.SetValue(value, row[columnName]);
            }

            return value;
        }

        private static string EncodeSqlObjectName(string name)
        {
            var split = name.Split('.');
            var enсoded = split.Select(p => p.Trim('[', ']', ' ')).Select(x => $"[{x}]");
            return string.Join(".", enсoded);
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
