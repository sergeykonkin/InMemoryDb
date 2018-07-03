using System;
using System.Collections;
using System.Collections.Generic;

namespace InMemoryDb
{
    /// <inheritdoc cref="IInMemoryTable" />
    public class InMemoryTable<TValue> : InMemoryTableBase<object, TValue>, IReadOnlyCollection<TValue>
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="InMemoryTable{TValue}" />
        /// </summary>
        public InMemoryTable(
            string connectionString,
            Func<TValue, object> keyFactory = null,
            string tableName = null,
            string rowVersionColumnName = "RowVersion",
            string deletedColumnName = "IsDeleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200)
            : base(
                connectionString,
                keyFactory ?? (x => x.GetHashCode()),
                tableName,
                rowVersionColumnName,
                deletedColumnName,
                commandTimeout,
                batchSize,
                delay)
        {
        }

        public TValue this[object index] => _store[index];

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator()
        {
            return _store.Values.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _store).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => _store.Count;
    }
}
