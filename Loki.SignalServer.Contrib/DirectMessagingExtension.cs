using System;
using System.Collections.Concurrent;
using System.Text;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Contrib.DirectMessaging.Queues;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Contrib.DirectMessaging
{
    public class DirectMessagingExtension : Extension
    {
        #region Readonly Variables

        /// <summary>
        /// The connections
        /// </summary>
        private IWebSocketConnectionManager _connections;

        /// <summary>
        /// The signal queues
        /// </summary>
        private readonly ConcurrentDictionary<string, EventedConcurrentQueue<ISignal>>_signalQueues = new ConcurrentDictionary<string, EventedConcurrentQueue<ISignal>>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectMessagingExtension"/> class.
        /// </summary>
        /// <param name="extensionName">Name of the extension.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public DirectMessagingExtension(string extensionName, IDependencyUtility dependencyUtility) : base(extensionName, dependencyUtility)
        {
            this.RegisterAction("SendMessage", SendMessage);
        }

        #endregion

        #region Actions

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal SendMessage(ISignal signal)
        {
            if (!_signalQueues.ContainsKey(signal.Recipient))
                _signalQueues[signal.Recipient] = new EventedConcurrentQueue<ISignal>();

            _signalQueues[signal.Recipient].Enqueue(signal);

            return null;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Registers the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public override void RegisterConnection(IWebSocketConnection connection)
        {
            if (!_signalQueues.ContainsKey(connection.UniqueClientIdentifier))
                _signalQueues[connection.UniqueClientIdentifier] = new EventedConcurrentQueue<ISignal>();

            _signalQueues[connection.UniqueClientIdentifier].Changed += OnEnqueue;
        }

        /// <summary>
        /// Unregisters the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public override void UnregisterConnection(IWebSocketConnection connection)
        {
            if (!_signalQueues.ContainsKey(connection.UniqueClientIdentifier))
                return;

            _signalQueues.TryRemove(connection.UniqueClientIdentifier, out _);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Called when [enqueue].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnEnqueue(object sender, EventArgs eventArgs)
        {
            ISignal signal = ((EventedConcurrentQueue<ISignal>) sender).Dequeue();
            if (signal == null)
                return;

            if (_connections == null)
                _connections = DependencyUtility.Resolve<IWebSocketConnectionManager>();

            IWebSocketConnection[] connections = _connections.GetConnectionsByClientIdentifier(signal.Recipient);
            foreach (IWebSocketConnection connection in connections)
            {
                string text = Encoding.UTF8.GetString(signal.Payload);

                connection.SendText(text);
            }
        }

        #endregion
    }
}
