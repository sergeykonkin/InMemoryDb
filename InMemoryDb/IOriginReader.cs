using System;
using System.Collections.Generic;

namespace InMemoryDb
{
    /// <summary>
    /// Provides functionality to read data since origin data source.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    public interface IOriginReader<TValue>
        where TValue : new()
    {
        /// <summary>
        /// Gets the type of the increment row key.
        /// </summary>
        /// <remarks>Must be IComparable.</remarks>
        Type RowKeyType { get; }

        /// <summary>
        /// Reads next batch since specified row key.
        /// </summary>
        /// <param name="since">Row Key to read since.</param>
        /// <returns>Next batch of values with their row keys.</returns>
        IEnumerable<Tuple<IComparable, TValue, bool>> Read(IComparable since);
    }
}
