using System;
using System.Collections.Generic;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Contrib.Common.Requests;
using Loki.SignalServer.Contrib.Common.Response;
using Loki.SignalServer.Contrib.Presence.Models;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Exceptions;
using Loki.SignalServer.Interfaces.Router;
using StackExchange.Redis;

namespace Loki.SignalServer.Contrib.Presence
{
    public class PresenceExtension : Extension
    {
        #region Constants

        /// <summary>
        /// The configuration key redis connection string
        /// </summary>
        private const string CONFIGURATION_KEY_REDIS_CONNECTION_STRING = "extensions:presence:config:cache:connection-string";

        /// <summary>
        /// The configuration key redis expiry
        /// </summary>
        private const string CONFIGURATION_KEY_REDIS_EXPIRY = "extensions:presence:config:cache:expiry";

        #endregion

        #region Readonly Variables

        /// <summary>
        /// The redis multiplexer
        /// </summary>
        private readonly IConnectionMultiplexer _redis;

        /// <summary>
        /// The cache
        /// </summary>
        private readonly IDatabase _cache;

        /// <summary>
        /// The expiry
        /// </summary>
        private readonly TimeSpan _expiry;

        private readonly ISignalRouter _router;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceExtension"/> class.
        /// </summary>
        /// <param name="extensionName">Name of the extension.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        /// <exception cref="InvalidExtensionException"></exception>
        public PresenceExtension(string extensionName, IDependencyUtility dependencyUtility) : base(extensionName, dependencyUtility)
        {
            _router = DependencyUtility.Resolve<ISignalRouter>();

            string connectionString = Config.Get(CONFIGURATION_KEY_REDIS_CONNECTION_STRING);

            _redis = ConnectionMultiplexer.Connect(connectionString);
            _cache = _redis.GetDatabase();
            _expiry = new TimeSpan(0, 0, Config.Get<int>(CONFIGURATION_KEY_REDIS_EXPIRY));

            if (_redis.IsConnected)
                Logger.Debug($"Established Redis connection for extension: {this.Name}");
            else
                throw new InvalidExtensionException($"Failed to establish Redis conenction for {this.Name}");

            this.RegisterAction("SetPresence", SetPresence);
            this.RegisterAction("BroadcastPresence", BroadcastPresence);
            this.RegisterAction("GetPresenceForSelf", GetPresenceForSelf);
            this.RegisterAction("GetPresenceForRoster", GetPresenceForRoster);
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
        /// Sets the presence.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal SetPresence(ISignal signal)
        {
            PresenceValue presence = signal.ResolvePayload<PresenceValue>();
            SetPresenceForEntity(signal.Sender, presence.ToString());

            BroadcastPresence(signal);

            return CreateResponse(signal, presence);
        }

        /// <summary>
        /// Gets the presence for self.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetPresenceForSelf(ISignal signal)
        {
            PresenceValue presence = GetPresenceForEntity(signal.Sender, PresenceValue.Available);
            
            return CreateResponse(signal, presence);
        }

        private ISignal BroadcastPresence(ISignal signal)
        {
            PresenceValue presence = GetPresenceForEntity(signal.Sender, PresenceValue.Available);
            RosterEntitiesResponse entities = GetEntityListForEntity(signal.Sender);

            _router.BroadcastSignal(entities.Entities, CreateResponse(signal, new UserPresence { EntityId = signal.Sender, PresenceValue = presence }, "UpdatedEntityPresence"));

            return null;
        }

        /// <summary>
        /// Gets the presence for roster.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private ISignal GetPresenceForRoster(ISignal signal)
        {
            PresenceRosterResponse response = GetRosterForEntity(signal.Sender);

            List<UserPresence> presences = new List<UserPresence>();
            foreach (string contactId in response.ContactIds)
                presences.Add(new UserPresence
                {
                    EntityId = contactId,
                    PresenceValue = GetPresenceForEntity(contactId)
                });

            return CreateResponse(signal, presences);
        }

        private PresenceRosterResponse GetRosterForEntity(string entityId)
        {
            PresenceRosterRequest requestPayload = new PresenceRosterRequest { EntityId = entityId };
            ISignal request = CreateCrossExtensionRequest("Roster/GetRosterForPresence", nameof(PresenceExtension), requestPayload);

            ISignal response = this.SendCrossExtensionRequest<PresenceRosterRequest>(request);
            return response.ResolvePayload<PresenceRosterResponse>();
        }

        private RosterEntitiesResponse GetEntityListForEntity(string entityId)
        {
            RosterEntitiesRequest requestPayload = new RosterEntitiesRequest { EntityId = entityId };
            ISignal request = CreateCrossExtensionRequest("Roster/GetRosterEntitiesForEntity", nameof(PresenceExtension), requestPayload);

            ISignal response = this.SendCrossExtensionRequest<RosterEntitiesResponse>(request);
            return response.ResolvePayload<RosterEntitiesResponse>();
        }

        #endregion

        #region Private Methods

        private void SetPresenceForEntity(string entityId, string presenceValue)
        {
            _cache.StringSet(entityId, presenceValue);//signal.ResolvePayload<string>());
            _cache.KeyExpire(entityId, _expiry);
        }

        /// <summary>
        /// Gets the presence for entity.
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="defaultPresence">The default presence.</param>
        /// <returns></returns>
        private PresenceValue GetPresenceForEntity(string entityId, PresenceValue defaultPresence = PresenceValue.Offline)
        {
            string cachedPresence = _cache.StringGet(entityId);
            if (string.IsNullOrEmpty(cachedPresence))
                return defaultPresence;

            return !Enum.TryParse(cachedPresence, true, out PresenceValue presence) ? defaultPresence : presence;
        }

        #endregion
    }
}
