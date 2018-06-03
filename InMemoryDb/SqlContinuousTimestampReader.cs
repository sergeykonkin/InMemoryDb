using System;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// Provides functionality to continuously and incrementally read data from origin data source and map records to <typeparamref name="TValue" /> <see cref="T:System.Type" />.
    /// Reads data by TIMESTAMP (or ROWVERSION) column.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    public class SqlContinuousTimestampReader<TValue> : SqlContinuousReader<ulong, TValue>
        where TValue : new()
    {
        private readonly string _timestampColumnName;

        /// <inheritdoc />
        /// <summary>
        /// Initializes new instance of <see cref="T:InMemoryDb.SqlIncrementalReader`2" />
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="timestampColumnName">Column name that contains TIMESTAMP or ROWVERSION.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        public SqlContinuousTimestampReader(
            string connectionString,
            string timestampColumnName,
            int batchSize = 1000,
            int delay = 200,
            int commandTimeout = 30)
            : base(connectionString, batchSize, delay, commandTimeout)
        {
            _timestampColumnName = timestampColumnName ?? throw new ArgumentNullException(nameof(timestampColumnName));
        }

        /// <inheritdoc />
        protected override string GetRowKeyColumnName()
        {
            return _timestampColumnName;
        }

        /// <inheritdoc />
        protected override object ConvertRowKey(ulong rowKey)
        {
            return ConvertToBytes(rowKey);
        }

        /// <inheritdoc />
        protected override ulong ConvertRowKey(object rowKey)
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
