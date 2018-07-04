using System;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <summary>
    /// In-memory replica of database table.
    /// </summary>
    public interface ITable
    {
        /// <summary>
        /// Starts continuous data reading routine.
        /// </summary>
        void Start(CancellationToken cancellationToken = default(CancellationToken), Action<Exception> handleException = null);

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        Task WhenInitialReadFinished(CancellationToken cancellationToken = default(CancellationToken));
    }
}
