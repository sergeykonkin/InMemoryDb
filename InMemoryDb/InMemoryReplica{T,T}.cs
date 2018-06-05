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
        private readonly IDictionary<TKey, TValue> _store;

        /// <summary>
        /// Initializes new instance of <see cref="InMemoryReplica{TKey,TValue}"/>
        /// </summary>
        /// <param name="reader">Reader of original data source.</param>
        /// <param name="keyFactory">Required when table key and increment row key differs.</param>
        public InMemoryReplica(IContinuousReader<TValue> reader, Func<TValue, TKey> keyFactory = null)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _store = new ConcurrentDictionary<TKey, TValue>();

            TKey GetKey(IComparable rowKey, TValue value)
            {
                if (keyFactory != null)
                {
                    return keyFactory(value);
                }

                return (TKey) rowKey;
            }

            _reader.NewValue += (rowKey, value) => _store[GetKey(rowKey, value)] = value;
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
            return _store.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _store).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => _store.Count;

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return _store.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _store.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public TValue this[TKey key] => _store[key];

        /// <inheritdoc />
        public IEnumerable<TKey> Keys => _store.Keys;

        /// <inheritdoc />
        public IEnumerable<TValue> Values => _store.Values;
    }
}
