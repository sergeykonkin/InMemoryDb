using System;
using System.Collections.Generic;

namespace InMemoryDb
{
    /// <summary>
    /// Provides functionality to read data from data source by batches moving using row keys.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    public interface IBatchReader<TValue>
    {
        /// <summary>
        /// Reads next batch from specified row key.
        /// </summary>
        /// <param name="from">Row Key to read from.</param>
        /// <returns>Next batch of values with their row keys.</returns>
        IEnumerable<Tuple<IComparable, TValue>> ReadNextBatch(IComparable from);
    }
}
