using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <summary>
    /// Holds Table definitions and helps to initialize them.
    /// </summary>
    public abstract class Database
    {
        private readonly string _connectionString;
        private readonly ITable[] _tables;

        private string _rowVersionColumnName = "RowVersion";
        private string _deletedColumnName = "IsDeleted";
        private int _commandTimeout = 30;
        private int _batchSize = 1000;
        private int _delay = 200;
        private Action<Exception> _handleException;

        private bool _initCalled;

        /// <summary>
        /// Initializes new instance of <see cref="Database"/>
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        protected Database(string connectionString)
        {
            _connectionString = connectionString;

            _tables = ReflectionHelper.GetWritableMembers(this.GetType())
                .Where(member => member.GetMemberType().GetInterfaces().Any(i => i == typeof(ITable)))
                .Select(member => member.GetValue(this))
                .Cast<ITable>()
                .ToArray();
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
            foreach (var table in _tables)
            {
                table.Start(cancellationToken, _handleException);
            }

            _initCalled = true;
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_initCalled)
            {
                throw new InvalidOperationException("Reading was not started. Call Init() first.");
            }

            return Task.WhenAll(_tables.Select(t => t.WhenInitialReadFinished(cancellationToken)));
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
