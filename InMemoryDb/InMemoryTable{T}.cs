using System;

namespace InMemoryDb
{
    /// <inheritdoc />
    public class InMemoryTable<TValue> : InMemoryTable<object, TValue>
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="InMemoryTable{T}" />
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
    }
}
