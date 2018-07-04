using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InMemoryDb
{
    /// <inheritdoc cref="ITable" />
    public class Table<TValue> : TableBase<object, TValue>, IReadOnlyCollection<TValue>
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="Table{TKey,TValue}" />
        /// </summary>
        public Table(
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
                keyFactory ?? GetKey,
                tableName,
                rowVersionColumnName,
                deletedColumnName,
                commandTimeout,
                batchSize,
                delay)
        {
        }

        /// <summary>
        /// Gets value from in-memory store by it's key.
        /// </summary>
        /// <param name="index">Value's key.</param>
        /// <returns>Value by key.</returns>
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

        private static object GetKey(TValue value)
        {
            var type = value.GetType();
            var keyMembers = ReflectionHelper.GetKeyMembers(type, "Id");
            if (keyMembers.Length == 0)
            {
                throw new InvalidOperationException("Cannot infer key of type " + type);
            }

            return keyMembers
                .Select(key => key.GetValue(value))
                .Select(val => val.GetHashCode())
                .Aggregate((acc, cur) => unchecked ((acc * 397) ^ cur ));
        }
    }
}
