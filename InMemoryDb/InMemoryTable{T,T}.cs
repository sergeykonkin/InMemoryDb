using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// In-memory replica of database table.
    /// </summary>
    /// <typeparam name="TKey">Type of the data key.</typeparam>
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public class InMemoryTable<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _store;
        private readonly ContinuousReader<TValue> _reader;

        /// <summary>
        /// Initializes new instance of <see cref="InMemoryTable{TKey,TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="keyFactory">In-memory dictionary's key factory.</param>
        /// <param name="tableName">The name of the table to read data from. If null - will be inferred from <typeparamref name="TValue"/> type.</param>
        /// <param name="rowVersionColumnName">The name of RowVersion (Timestamp) column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value. Can be null if deletion handling is not required.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        public InMemoryTable(
            string connectionString,
            Func<TValue, TKey> keyFactory,
            string tableName = null,
            string rowVersionColumnName = "RowVersion",
            string deletedColumnName = "IsDeleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200)
        {
            if (keyFactory == null) throw new ArgumentNullException(nameof(keyFactory));

            _store = new ConcurrentDictionary<TKey, TValue>();
            _reader = new ContinuousReader<TValue>(
                connectionString,
                tableName,
                rowVersionColumnName,
                deletedColumnName,
                commandTimeout,
                batchSize,
                delay);

            _reader.NewValue += value => _store[keyFactory(value)] = value;
            _reader.DeletedValue += value => _store.Remove(keyFactory(value));
        }

        /// <summary>
        /// Starts continuous data reading routine.
        /// </summary>
        public void Start()
        {
            _reader.Start();
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished()
        {
            return _reader.WhenInitialReadFinished();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _store).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => _store.Count;

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return _store.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _store.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public TValue this[TKey key] => _store[key];

        /// <inheritdoc />
        public IEnumerable<TKey> Keys => _store.Keys;

        /// <inheritdoc />
        public IEnumerable<TValue> Values => _store.Values;
    }
}
