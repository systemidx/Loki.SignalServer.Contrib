using System;
using System.Collections.Concurrent;
using System.Text;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Contrib.DirectMessaging
{
    public class WrappedConcurrentQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public event EventHandler Changed;

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public T Dequeue()
        {
            _queue.TryDequeue(out T result);
            return result;
        }
    }

    public class DirectMessagingExtension : Extension
    {
        private IWebSocketConnectionManager _connections;
        private readonly ConcurrentDictionary<string, WrappedConcurrentQueue<ISignal>>_signalQueues = new ConcurrentDictionary<string, WrappedConcurrentQueue<ISignal>>();

        public DirectMessagingExtension(string extensionName, IDependencyUtility dependencyUtility) : base(extensionName, dependencyUtility)
        {
            this.RegisterAction("SendMessage", SendMessage);
        }

        private ISignal SendMessage(ISignal signal)
        {
            if (!_signalQueues.ContainsKey(signal.Recipient))
                _signalQueues[signal.Recipient] = new WrappedConcurrentQueue<ISignal>();

            _signalQueues[signal.Recipient].Enqueue(signal);

            return null;
        }

        public override void RegisterConnection(IWebSocketConnection connection)
        {
            string id = $"{connection.ClientIdentifier}";

            if (!_signalQueues.ContainsKey(id))
                _signalQueues[id] = new WrappedConcurrentQueue<ISignal>();

            _signalQueues[id].Changed += OnEnqueue;
        }

        public override void UnregisterConnection(IWebSocketConnection connection)
        {
        }

        private void OnEnqueue(object sender, EventArgs eventArgs)
        {
            ISignal signal = ((WrappedConcurrentQueue<ISignal>) sender).Dequeue();
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
    }
}
