using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// In-memory replica of origin data source.
    /// </summary>
    /// <typeparam name="TKey">Type of the data key.</typeparam>
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public class InMemoryReplica<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : IComparable
        where TValue : new()
    {
        private readonly IContinuousReader<TValue> _reader;
        protected IDictionary<TKey, TValue> Store { get; }

        /// <summary>
        /// Initializes new instance of <see cref="InMemoryReplica{TKey,TValue}"/>
        /// </summary>
        /// <param name="reader">Reader of original data source.</param>
        /// <param name="keyFactory">Required when table key and increment row key differs.</param>
        public InMemoryReplica(IContinuousReader<TValue> reader, Func<TValue, TKey> keyFactory = null)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            Store = new ConcurrentDictionary<TKey, TValue>();

            TKey GetKey(IComparable rowKey, TValue value)
            {
                if (keyFactory != null)
                {
                    return keyFactory(value);
                }

                return (TKey) rowKey;
            }

            _reader.NewValue += (rowKey, value) => Store[GetKey(rowKey, value)] = value;
            _reader.DeletedValue += (rowKey, value) => Store.Remove(GetKey(rowKey, value));
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished()
        {
            return _reader.WhenInitialReadFinished();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) Store).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => Store.Count;

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return Store.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return Store.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public TValue this[TKey key] => Store[key];

        /// <inheritdoc />
        public IEnumerable<TKey> Keys => Store.Keys;

        /// <inheritdoc />
        public IEnumerable<TValue> Values => Store.Values;
    }
}
