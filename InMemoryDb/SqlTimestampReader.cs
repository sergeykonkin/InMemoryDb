using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// SQL Server batch reader.
    /// </summary>
    public class SqlTimestampReader<TValue> : IOriginReader<TValue>
        where TValue : new()
    {
        protected readonly string ConnectionString;
        protected readonly string TimestampColumnName;
        protected readonly string DeletedColumnName;
        protected readonly int CommandTimeout;
        protected readonly int BatchSize;
        protected readonly string TableName;

        public Type RowKeyType { get; } = typeof(ulong);

        /// <summary>
        /// Initializes new instance of <see cref="SqlTimestampReader{TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        /// <param name="timestampColumnName">The name of TIMESTAMP / ROWVERSION column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value. Can be null if deletion handling is not required.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        public SqlTimestampReader(
            string connectionString,
            string tableName = null,
            string timestampColumnName = "Timestamp",
            string deletedColumnName = "Deleted",
            int commandTimeout = 30,
            int batchSize = 1000)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            TimestampColumnName = timestampColumnName ?? throw new ArgumentNullException(nameof(timestampColumnName));
            DeletedColumnName = deletedColumnName;
            if (commandTimeout < 0) throw new ArgumentOutOfRangeException(nameof(commandTimeout));
            CommandTimeout = commandTimeout;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            BatchSize = batchSize;

            var type = typeof(TValue);
            TableName = tableName ?? GetTableNameFromAttribute(type) ?? type.Name;
        }


        /// <inheritdoc />
        public virtual IEnumerable<Tuple<IComparable, TValue, bool>> Read(IComparable since)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var timestamp = (ulong)since;
                while (true)
                {
                    using (SqlCommand command = CreateCommand(timestamp))
                    {
                        command.Connection = connection;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                                break;

                            while (reader.Read())
                            {
                                TValue value = ParseRow(reader, out ulong rowTimestamp);
                                timestamp = Math.Max(timestamp, rowTimestamp);

                                bool isDeleted = false;
                                if (!string.IsNullOrWhiteSpace(DeletedColumnName))
                                {
                                    isDeleted = (bool) reader[DeletedColumnName];
                                }

                                yield return new Tuple<IComparable, TValue, bool>(rowTimestamp, value, isDeleted);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates SQL command for data retrieval.
        /// </summary>
        /// <param name="timestamp">Timestamp to read since.</param>
        /// <returns>SQL command object ready for execution.</returns>
        protected virtual SqlCommand CreateCommand(ulong timestamp)
        {
            var command = new SqlCommand();

            var table = FixSqlObjectName(TableName);
            var timestampColumn = FixSqlObjectName(TimestampColumnName);

            command.CommandText =
                $@"
SELECT TOP (@batchSize)
    *
FROM {table}
WHERE {timestampColumn} > @timestamp
ORDER BY {timestampColumn} ASC";

            command.CommandTimeout = CommandTimeout;
            command.Parameters.Add(new SqlParameter("batchSize", BatchSize));
            command.Parameters.Add(new SqlParameter("timestamp", ConvertToBytes(timestamp)));

            return command;
        }

        /// <summary>
        /// Parses row data and maps it to <typeparamref name="TValue"/> type.
        /// </summary>
        /// <param name="row">Record with raw data.</param>
        /// <param name="rowTimestamp">Timestamp value of this row.</param>
        /// <returns>Data mapped to <typeparamref name="TValue"/> type.</returns>
        protected virtual TValue ParseRow(IDataRecord row, out ulong rowTimestamp)
        {
            var result = new TValue();
            var type = typeof(TValue);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null);

            foreach (var prop in props)
            {
                var columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                object value = row[columnName];
                prop.SetValue(result, value);
            }

            rowTimestamp = ConvertToUInt64((byte[]) row[TimestampColumnName]);
            return result;
        }

        protected static string FixSqlObjectName(string name)
        {
            var parts = name.Split('.');
            var fixedParts = parts.Select(p => p.Trim('[', ']', ' ')).Select(p => $"[{p}]");
            return string.Join(".", fixedParts);
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
