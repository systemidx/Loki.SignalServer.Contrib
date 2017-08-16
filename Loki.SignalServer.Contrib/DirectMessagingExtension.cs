using System;
using System.Text;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Contrib.DirectMessaging
{
    public class DirectMessagingExtension : Extension
    {
        #region Constants

        private const string QUEUE_EXCHANGE_ID = "directmessaging";

        #endregion

        #region Readonly Variables

        /// <summary>
        /// The connections
        /// </summary>
        private IWebSocketConnectionManager _connections;

        /// <summary>
        /// The queue handler
        /// </summary>
        private readonly IEventedQueueHandler<ISignal> _queueHandler;

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

            _connections = dependencyUtility.Resolve<IWebSocketConnectionManager>();
            _queueHandler = dependencyUtility.Resolve<IEventedQueueHandler<ISignal>>();
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
            _queueHandler.Enqueue(QUEUE_EXCHANGE_ID, signal.Recipient, signal);

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
            if (_connections == null)
                _connections = DependencyUtility.Resolve<IWebSocketConnectionManager>();

            _queueHandler.CreateQueue(QUEUE_EXCHANGE_ID, connection.ClientIdentifier);
            _queueHandler.AddEvent(QUEUE_EXCHANGE_ID, connection.ClientIdentifier, OnDequeue);
        }

        /// <summary>
        /// Unregisters the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public override void UnregisterConnection(IWebSocketConnection connection)
        {
            _queueHandler.RemoveEvent(QUEUE_EXCHANGE_ID, connection.ClientIdentifier, OnDequeue);
            _queueHandler.RemoveQueue(QUEUE_EXCHANGE_ID, connection.ClientIdentifier);
        }

        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Called when [dequeue].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="signal">The signal1.</param>
        private void OnDequeue(object sender, ISignal signal)
        {
            if (signal == null)
                return;

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
