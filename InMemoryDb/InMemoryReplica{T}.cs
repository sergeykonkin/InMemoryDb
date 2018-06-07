using System.Collections.Generic;

namespace InMemoryDb
{
    /// <inheritdoc cref="InMemoryReplica{TKey,TValue}"/>
    /// <summary>
    /// In-memory replica of origin data source.
    /// </summary>
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public class InMemoryReplica<TValue> : InMemoryReplica<int, TValue>, IReadOnlyCollection<TValue>
        where TValue : new()
    {
        public InMemoryReplica(IContinuousReader<TValue> reader)
            : base(reader, x => x.GetHashCode())
        {
        }

        #region Implementation of IEnumerable<out TValue>

        public IEnumerator<TValue> GetEnumerator()
        {
            return Store.Values.GetEnumerator();
        }

        #endregion
    }
}
