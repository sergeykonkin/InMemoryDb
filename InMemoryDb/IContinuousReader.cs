using System;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <summary>
    /// Provides functionality to continuously and incrementally read data from origin data source and map records to <typeparamref name="TValue"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    public interface IContinuousReader<out TValue>
        where TValue : new()
    {
        /// <summary>
        /// Starts data reading process.
        /// </summary>
        void Start();

        /// <summary>
        /// Indicates that initial data read is finished and this reader now in polling mode.
        /// </summary>
        bool IsInitialReadFinished { get; }

        /// <summary>
        /// Occurs when new value was read from origin data source.
        /// </summary>
        event Action<IComparable, TValue> NewValue;

        /// <summary>
        /// Occurs when value has been deleted from origin data source.
        /// </summary>
        event Action<IComparable, TValue> DeletedValue;

        /// <summary>
        /// Occurs when initial data read is finished.
        /// </summary>
        event Action InitialReadFinished;

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        Task WhenInitialReadFinished();
    }
}
