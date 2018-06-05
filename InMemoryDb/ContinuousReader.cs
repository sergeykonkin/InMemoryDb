using System;
using System.Linq;
using System.Threading.Tasks;

namespace InMemoryDb
{
    /// <inheritdoc />
    /// <summary>
    /// Provides functionality to continuously and incrementally read data from origin data source and map records to <typeparamref name="TValue" /> <see cref="T:System.Type" />.
    /// </summary>
    /// <typeparam name="TValue">Type of value data should be mapped to.</typeparam>
    public class ContinuousReader<TValue> : IContinuousReader<TValue>
        where TValue : new()
    {
        private readonly IBatchReader<TValue> _batchReader;
        private readonly Type _rowKeyType;
        private readonly int _delay;
        private readonly TaskCompletionSource<bool> _initialReadFinishedSource;

        /// <summary>
        /// Initializes new instance of <see cref="ContinuousReader{TValue}"/>
        /// </summary>
        /// <param name="batchReader">Data source batch reader.</param>
        /// <param name="delay">Delay (in milliseconds) between two requests when reader continuously polling origin data source.</param>
        public ContinuousReader(IBatchReader<TValue> batchReader, int delay = 200)
        {
            _batchReader = batchReader ?? throw new ArgumentNullException(nameof(batchReader));
            if (delay <= 0) throw new ArgumentOutOfRangeException(nameof(delay));
            _delay = delay;
            _initialReadFinishedSource = new TaskCompletionSource<bool>();
            InitialReadFinished += () => { _initialReadFinishedSource.SetResult(true); };
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns the task that will be completed when initial data read is finished.
        /// </summary>
        public Task WhenInitialReadFinished()
        {
            return _initialReadFinishedSource.Task;
        }

        /// <inheritdoc />
        /// <summary>
        /// Occurs when new value was read from origin data source.
        /// </summary>
        public event Action<IComparable, TValue> NewValue;

        /// <inheritdoc />
        /// <summary>
        /// Occurs when initial data read is finished.
        /// </summary>
        public event Action InitialReadFinished;

        /// <inheritdoc />
        /// <summary>
        /// Indicates that initial data read is finished and this reader now in polling mode.
        /// </summary>
        public bool IsInitialReadFinished { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// Starts data reading process.
        /// </summary>
        public virtual void Start()
        {
            Task.Run(async () =>
            {
                IComparable rowKey = 0;// (IComparable)Activator.CreateInstance(_rowKeyType);
                while (true)
                {
                    var results = _batchReader.ReadNextBatch(rowKey).ToList();
                    if (results.Count == 0)
                    {
                        if (!IsInitialReadFinished)
                        {
                            IsInitialReadFinished = true;
                            OnInitialReadFinished();
                        }

                        await Task.Delay(_delay);
                        continue;
                    }

                    foreach (var tuple in results)
                    {
                        OnNewValue(tuple.Item1, tuple.Item2);
                    }

                    rowKey = results.Select(t => t.Item1).Max();
                }
            });
        }

        /// <summary>
        /// Rises <see cref="NewValue"/> event.
        /// </summary>
        /// <param name="rowKey">This value's row key.</param>
        /// <param name="value">New value.</param>
        protected virtual void OnNewValue(IComparable rowKey, TValue value)
        {
            NewValue?.Invoke(rowKey, value);
        }

        /// <summary>
        /// Rises <see cref="InitialReadFinished"/> event.
        /// </summary>
        protected virtual void OnInitialReadFinished()
        {
            InitialReadFinished?.Invoke();
        }
    }
}
