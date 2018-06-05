using System;
using System.Collections.Generic;
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
    public class SqlBatchReader<TValue> : IBatchReader<TValue>
        where TValue : new()
    {
        private readonly string _connectionString;
        private readonly int _commandTimeout;
        private readonly int _batchSize;

        private bool _isInitialized;
        private string _tableName;
        private string _rowKeyColumnName;

        /// <summary>
        /// Initializes new instance of <see cref="SqlBatchReader{TValue}"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        public SqlBatchReader(string connectionString, int commandTimeout = 30, int batchSize = 1000)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            if (commandTimeout < 0) throw new ArgumentOutOfRangeException(nameof(commandTimeout));
            _commandTimeout = commandTimeout;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            _batchSize = batchSize;
        }

        /// <inheritdoc />
        public virtual IEnumerable<Tuple<IComparable, TValue>> ReadNextBatch(IComparable from)
        {
            Init();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();

                var rowKeyColumn = FixSqlObjectName(_rowKeyColumnName);
                var table = FixSqlObjectName(_tableName);

                cmd.CommandText =
                    $@"
SELECT TOP (@batchSize)
    *
FROM {table}
WHERE {rowKeyColumn} > @rowKey
ORDER BY {rowKeyColumn} ASC";

                cmd.CommandTimeout = _commandTimeout;
                cmd.Parameters.Add(new SqlParameter("batchSize", _batchSize));
                cmd.Parameters.Add(new SqlParameter("rowKey", ConvertToSql(from)));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TValue value = ParseRow(reader, out IComparable rowKey);
                        yield return new Tuple<IComparable, TValue>(rowKey, value);
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
        protected virtual TValue ParseRow(IDataRecord row, out IComparable rowKey)
        {
            var result = new TValue();
            var type = typeof(TValue);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null);

            foreach (var prop in props)
            {
                var columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                object value = row[columnName];
                prop.SetValue(result, value);
            }

            rowKey = ConvertFromSql(row[_rowKeyColumnName]);
            return result;
        }

        /// <summary>
        /// Converts row key to some another SQL-compatible type.
        /// </summary>
        /// <param name="rowKey">Row key.</param>
        /// <returns>Converted row key.</returns>
        protected virtual object ConvertToSql(IComparable rowKey)
        {
            return rowKey;
        }

        /// <summary>
        /// Converts row key that was read from DB to some another type.
        /// </summary>
        /// <param name="rowKey">Row key.</param>
        /// <returns>Converted row key.</returns>
        protected virtual IComparable ConvertFromSql(object rowKey)
        {
            return (IComparable) rowKey;
        }

        /// <summary>
        /// Performs one-time initialization.
        /// </summary>
        protected virtual void Init()
        {
            if (_isInitialized)
                return;

            _tableName = GetTableName();
            _rowKeyColumnName = GetRowKeyColumnName();

            _isInitialized = true;
        }

        /// <summary>
        /// Gets the name of the table to read data from.
        /// </summary>
        /// <returns>TableName name.</returns>
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

        /// <summary>
        /// Prepares object name to be used in SQL server.
        /// </summary>
        /// <param name="name">Object name.</param>
        /// <returns>Fixed value.</returns>
        private static string FixSqlObjectName(string name)
        {
            var parts = name.Split('.');
            var fixedParts = parts.Select(p => p.Trim('[', ']', ' ')).Select(p => $"[{p}]");
            return string.Join(".", fixedParts);
        }
    }
}
