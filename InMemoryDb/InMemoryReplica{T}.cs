﻿using System;
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
    /// <typeparam name="TValue">Type of the data value.</typeparam>
    public class InMemoryReplica<TValue> : IReadOnlyCollection<TValue>
        where TValue : new()
    {
        private readonly IContinuousReader<TValue> _reader;
        private readonly ConcurrentBag<TValue> _store;

        /// <summary>
        /// Initializes new instance of <see cref="InMemoryReplica{TValue}"/>
        /// </summary>
        /// <param name="reader">Reader of original data source.</param>
        public InMemoryReplica(IContinuousReader<TValue> reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _store = new ConcurrentBag<TValue>();
            _reader.NewValue += (key, value) => _store.Add(value);
        }

        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished()
        {
            return _reader.WhenInitialReadFinished();
        }

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator()
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
    }
}