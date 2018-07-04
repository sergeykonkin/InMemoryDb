using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <inheritdoc cref="ITable" />
    /// <typeparam name="TKey">Type of the data key.</typeparam>
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public abstract class TableBase<TKey, TValue> : ITable
        where TValue : new()
    {
        protected readonly IDictionary<TKey, TValue> _store;
        private readonly Func<TValue, TKey> _keyFactory;
        private readonly ContinuousReader<TValue> _reader;

        /// <summary>
        /// Initializes new instance of <see cref="TableBase{TKey,TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="keyFactory">In-memory dictionary's key factory.</param>
        /// <param name="tableName">The name of the table to read data from. If null - will be inferred from <typeparamref name="TValue"/> type.</param>
        /// <param name="rowVersionColumnName">The name of RowVersion (Timestamp) column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        protected TableBase(
            string connectionString,
            Func<TValue, TKey> keyFactory,
            string tableName = null,
            string rowVersionColumnName = "RowVersion",
            string deletedColumnName = "IsDeleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200)
        {
            _keyFactory = keyFactory ?? throw new ArgumentNullException(nameof(keyFactory));

            _store = new ConcurrentDictionary<TKey, TValue>();
            _reader = new ContinuousReader<TValue>(
                connectionString,
                tableName,
                rowVersionColumnName,
                deletedColumnName,
                commandTimeout,
                batchSize,
                delay);
        }

        /// <summary>
        /// Starts continuous data reading routine.
        /// </summary>
        public void Start(CancellationToken cancellationToken = default(CancellationToken), Action<Exception> handleException = null)
        {
            _reader.Start(
                newValue => _store[_keyFactory(newValue)] = newValue,
                deletedValue => _store.Remove(_keyFactory(deletedValue)),
                cancellationToken,
                handleException);
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _reader.WhenInitialReadFinished(cancellationToken);
        }
    }
}
