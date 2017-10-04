using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Contrib.Common.Requests;
using Loki.SignalServer.Contrib.Common.Response;
using Loki.SignalServer.Contrib.Rosters.Handlers;
using Loki.SignalServer.Contrib.Rosters.Models;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Contrib.Rosters
{
    public class RostersExtension : Extension
    {
        #region Readonly Variables

        /// <summary>
        /// The roster handler
        /// </summary>
        private readonly RosterHandler _rosterHandler;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RostersExtension"/> class.
        /// </summary>
        /// <param name="extensionName">Name of the extension.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public RostersExtension(string extensionName, IDependencyUtility dependencyUtility) : base(extensionName, dependencyUtility)
        {
            this.RegisterAction("GetRoster", GetRoster);
            this.RegisterAction("GetEntityRosters", GetEntityRosters);
            this.RegisterCrossExtensionAction("GetRosterEntitiesForEntity", GetRosterEntitiesForEntity);
            this.RegisterCrossExtensionAction("GetRosterForPresence", GetRosterForPresence);
            this.RegisterCrossExtensionAction("GetRosterForMetadata", GetRosterForMetadata);

            _rosterHandler = new RosterHandler(dependencyUtility);
        }
        
        #endregion

        #region Public Methods

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
        /// Gets the roster.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetRoster(ISignal signal)
        {
            Roster roster = _rosterHandler.GetRoster(signal.Sender);
            
            return CreateResponse(signal, roster);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        private ISignal GetRosterEntitiesForEntity(ISignal signal)
        {
            RosterEntitiesRequest request = signal.ResolvePayload<RosterEntitiesRequest>();

            IEnumerable<string> entities = _rosterHandler.SearchEntitiesForContact(request.EntityId);

            RosterEntitiesResponse response = new RosterEntitiesResponse {Entities = entities.ToList()};

            return CreateResponse(signal, response);
        }

        /// <summary>
        /// Gets the roster for presence.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetRosterForPresence(ISignal signal)
        {
            PresenceRosterRequest request = signal.ResolvePayload<PresenceRosterRequest>();
            if (request == null)
                return null;

            Roster roster = _rosterHandler.GetRoster(request.EntityId);
            PresenceRosterResponse response = new PresenceRosterResponse {ContactIds = roster?.Entries?.Select(y => y.ContactId).Distinct().ToList() };

            return CreateResponse(signal, response);
        }

        /// <summary>
        /// Gets the roster for metadata.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetRosterForMetadata(ISignal signal)
        {
            MetadataRosterRequest request = signal.ResolvePayload<MetadataRosterRequest>();
            if (request == null)
                return null;

            Roster roster = _rosterHandler.GetRoster(request.EntityId);
            MetadataRosterResponse response = new MetadataRosterResponse { ContactIds = roster?.Entries?.Select(y => y.ContactId).Distinct().ToList() };

            return CreateResponse(signal, response);
        }

        /// <summary>
        /// Gets the entity rosters.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetEntityRosters(ISignal signal)
        {
            return CreateResponse(signal, _rosterHandler.SearchEntitiesForContact(signal.Sender));
        }

        #endregion
    }
}