using System;
using System.Collections.Concurrent;

namespace Loki.SignalServer.Contrib.DirectMessaging.Queues
{
    public class EventedConcurrentQueue<T>
    {
        #region Readonly Variables

        /// <summary>
        /// The backing queue
        /// </summary>
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        #endregion

        #region Public Variables

        /// <summary>
        /// Occurs when [changed].
        /// </summary>
        public event EventHandler Changed;

        #endregion

        #region Public Methods

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dequeues this instance.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            _queue.TryDequeue(out T result);

            return result;
        }

        #endregion
    }
}
