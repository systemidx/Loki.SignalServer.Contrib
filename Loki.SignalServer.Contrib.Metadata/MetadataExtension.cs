using System.Collections.Generic;
using System.Linq;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Contrib.Common.Requests;
using Loki.SignalServer.Contrib.Common.Response;
using Loki.SignalServer.Contrib.Metadata.Handlers;
using Loki.SignalServer.Contrib.Metadata.Models;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Contrib.Metadata
{
    public class MetadataExtension: Extension
    {
        #region Member Variables

        /// <summary>
        /// The metadata handler
        /// </summary>
        private MetadataHandler _handler;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataExtension"/> class.
        /// </summary>
        /// <param name="extensionName">Name of the extension.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public MetadataExtension(string extensionName, IDependencyUtility dependencyUtility) : base(extensionName, dependencyUtility)
        {
            this.RegisterAction("GetMetadataForSelf", GetMetadataForSelf);
            this.RegisterAction("GetMetadataForRoster", GetMetadataForRoster);
        }
        
        #endregion

        #region Extension Methods

        public override void Initialize()
        {
            _handler = new MetadataHandler(DependencyUtility);

            base.Initialize();
        }

        /// <summary>
        /// Registers the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public override void RegisterConnection(IWebSocketConnection connection)
        {
        }

        /// <summary>
        /// Unregisters the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public override void UnregisterConnection(IWebSocketConnection connection)
        {
        }

        #endregion

        #region Actions

        /// <summary>
        /// Gets the metadata for self.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetMetadataForSelf(ISignal signal)
        {
            UserMetadata metadata = _handler.GetMetadata(signal.Sender);
            if (metadata == null)
                return null;

            return CreateResponse(signal, metadata);
        }

        /// <summary>
        /// Gets the metadata for roster.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetMetadataForRoster(ISignal signal)
        {
            MetadataRosterRequest requestPayload = new MetadataRosterRequest { EntityId = signal.Sender };
            ISignal request = CreateCrossExtensionRequest("Roster/GetRosterForMetadata", nameof(MetadataExtension), requestPayload);

            ISignal response = this.SendCrossExtensionRequest<MetadataRosterResponse>(request);
            MetadataRosterResponse responsePayload = response.ResolvePayload<MetadataRosterResponse>();

            if (responsePayload == null || responsePayload.ContactIds == null || !responsePayload.ContactIds.Any())
                return null;

            List<UserMetadata> payload = new List<UserMetadata>();
            foreach (string entityId in responsePayload.ContactIds)
                payload.Add(_handler.GetMetadata(entityId));

            return CreateResponse(signal, payload);
        }

        #endregion
    }
}
