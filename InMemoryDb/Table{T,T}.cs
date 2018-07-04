using System;
using System.Collections;
using System.Collections.Generic;

namespace InMemoryDb
{
    /// <inheritdoc cref="ITable" />
    public class Table<TKey, TValue> : TableBase<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="Table{TKey,TValue}" />
        /// </summary>
        public Table(
            string connectionString,
            Func<TValue, TKey> keyFactory,
            string tableName = null,
            string rowVersionColumnName = "RowVersion",
            string deletedColumnName = "IsDeleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200)
            : base(
                connectionString,
                keyFactory,
                tableName,
                rowVersionColumnName,
                deletedColumnName,
                commandTimeout,
                batchSize,
                delay)
        {
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
