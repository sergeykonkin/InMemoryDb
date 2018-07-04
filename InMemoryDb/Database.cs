using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryDb
{
    public abstract class Database
    {
        private readonly string _connectionString;

        private string _rowVersionColumnName = "RowVersion";
        private string _deletedColumnName = "IsDeleted";
        private int _commandTimeout = 30;
        private int _batchSize = 1000;
        private int _delay = 200;
        private Action<Exception> _handleException;

        /// <summary>
        /// Initializes new instance of <see cref="Database"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        protected Database(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Sets up default parameters for database.
        /// </summary>
        /// <param name="rowVersionColumnName">The name of RowVersion (Timestamp) column.</param>
        /// <param name="deletedColumnName">The name of column that identifies deleted value.</param>
        /// <param name="commandTimeout">SQL Command timeout in secconds.</param>
        /// <param name="batchSize">Batch size of single read operation.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        /// <param name="handleException">Exception handling delegate.</param>
        public void Setup(
            string rowVersionColumnName = "RowVersion",
            string deletedColumnName = "IsDeleted",
            int commandTimeout = 30,
            int batchSize = 1000,
            int delay = 200,
            Action<Exception> handleException = null)
        {
            _rowVersionColumnName = rowVersionColumnName;
            _deletedColumnName = deletedColumnName;
            _commandTimeout = commandTimeout;
            _batchSize = batchSize;
            _delay = delay;
            _handleException = handleException;
        }

        /// <summary>
        /// Initializes this <see cref="Database"/>.
        /// </summary>
        public void Init(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tables = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.PropertyType.GetInterfaces().Any(i => i == typeof(ITable)))
                .Select(prop => prop.GetValue(this))
                .Cast<ITable>();

            var initTasks = new List<Task>();
            foreach (var table in tables)
            {
                table.Start(cancellationToken, _handleException);
                initTasks.Add(table.WhenInitialReadFinished(cancellationToken));
            }

            Task.WaitAll(initTasks.ToArray());
        }

        /// <summary>
        /// Initializes table.
        /// </summary>
        /// <param name="keyFactory">In-memory dictionary's key factory.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        protected Table<TKey, TValue> Table<TKey, TValue>(Func<TValue, TKey> keyFactory, string tableName = null)
            where TValue : new()
        {
            return new Table<TKey, TValue>(
                _connectionString,
                keyFactory,
                tableName,
                _rowVersionColumnName,
                _deletedColumnName,
                _commandTimeout,
                _batchSize,
                _delay);
        }

        /// <summary>
        /// Initializes table.
        /// </summary>
        /// <param name="keyFactory">In-memory dictionary's key factory.</param>
        /// <param name="tableName">The name of the table to read data from.</param>
        protected Table<TValue> Table<TValue>(Func<TValue, object> keyFactory = null, string tableName = null)
            where TValue : new()
        {
            return new Table<TValue>(
                _connectionString,
                keyFactory,
                tableName,
                _rowVersionColumnName,
                _deletedColumnName,
                _commandTimeout,
                _batchSize,
                _delay);
        }
    }
}
