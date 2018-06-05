using System;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// SQL Server batch reader where row key is represented by TIMESTAMP / ROWVERSION column.
    /// </summary>
    public class SqlTimestampBatchReader<TValue> : SqlBatchReader<TValue>
        where TValue : new()
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="SqlTimestampBatchReader{TValue}" />
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="timestampColumnName">The name of TIMESTAMP / ROWVERSION column.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        public SqlTimestampBatchReader(
            string connectionString,
            string timestampColumnName,
            int commandTimeout = 30,
            int batchSize = 1000)
            : base(connectionString, commandTimeout, batchSize)
        {
            RowKeyColumnName = timestampColumnName ?? throw new ArgumentNullException(nameof(timestampColumnName));
        }

        /// <inheritdoc />
        protected override string RowKeyColumnName { get; }

        /// <inheritdoc />
        protected override object ConvertToSql(IComparable rowKey)
        {
            if (rowKey is ulong uint64)
            {
                return ConvertToBytes(uint64);
            }

            return ConvertToBytes(Convert.ToUInt64(rowKey));
        }

        /// <inheritdoc />
        protected override IComparable ConvertFromSql(object rowKey)
        {
            return ConvertToUInt64((byte[]) rowKey);
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
