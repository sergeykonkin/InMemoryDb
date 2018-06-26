using System;

namespace InMemoryDb
{
    /// <summary>
    /// Represents full in-memory copy of SQL Server table.
    /// </summary>
    /// <typeparam name="TKey">Type of the data key.</typeparam>
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public class SqlTableReplica<TKey, TValue> : InMemoryReplica<TKey, TValue>
        where TKey : IComparable
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="T:InMemoryDb.SqlTableReplica`2" />
        /// </summary>
        /// <param name="keyFactory">In-memory dictionary's key factory.</param>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        /// <param name="timestampColumnName">The name of TIMESTAMP / ROWVERSION column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value. Can be null if deletion handling is not required.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        public SqlTableReplica(
            string connectionString,
            Func<TValue, TKey> keyFactory,
            string tableName = null,
            string timestampColumnName = "Timestamp",
            string deletedColumnName = "Deleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200)
            : base(new ContinuousReader<TValue>(
                    new SqlTimestampReader<TValue>(
                        connectionString,
                        tableName,
                        timestampColumnName,
                        deletedColumnName,
                        commandTimeout,
                        batchSize),
                    delay),
                keyFactory)
        {
        }
    }

    /// <summary>
    /// Represents full in-memory copy of SQL Server table.
    /// </summary>
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public class SqlTableReplica<TValue> : InMemoryReplica<TValue>
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="SqlTableReplica{TKey,TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        /// <param name="timestampColumnName">The name of TIMESTAMP / ROWVERSION column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value. Can be null if deletion handling is not required.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        public SqlTableReplica(
            string connectionString,
            string tableName = null,
            string timestampColumnName = "Timestamp",
            string deletedColumnName = "Deleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200)
            : base(new ContinuousReader<TValue>(
                    new SqlTimestampReader<TValue>(
                        connectionString,
                        tableName,
                        timestampColumnName,
                        deletedColumnName,
                        commandTimeout,
                        batchSize),
                    delay))
        {
        }
    }
}
