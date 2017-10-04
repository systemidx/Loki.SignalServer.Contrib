using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Contrib.Rosters.Models;
using Loki.SignalServer.Interfaces.Cache;
using Loki.SignalServer.Interfaces.Configuration;

namespace Loki.SignalServer.Contrib.Rosters.Handlers
{
    public class RosterHandler
    {
        #region Constants

        /// <summary>
        /// The configuration key SQL host
        /// </summary>
        private const string CONFIGURATION_KEY_SQL_HOST = "extensions:roster:config:sql:connection-string";

        /// <summary>
        /// The configuration key cache service
        /// </summary>
        private const string CONFIGURATION_KEY_ROSTER_CACHE_SERVICE = "extensions:roster:config:cache:user-rosters:service";
        private const string CONFIGURATION_KEY_CONTACT_CACHE_SERVICE = "extensions:roster:config:cache:user-contacts:service";

        /// <summary>
        /// The configuration key cache connection string
        /// </summary>
        private const string CONFIGURATION_KEY_ROSTER_CACHE_CONNECTIONSTRING = "extensions:roster:config:cache:user-rosters:connection-string";
        private const string CONFIGURATION_KEY_CONTACTS_CACHE_CONNECTIONSTRING = "extensions:roster:config:cache:user-contacts:connection-string";

        /// <summary>
        /// The configuration key cache expiry
        /// </summary>
        private const string CONFIGURATION_KEY_ROSTER_CACHE_EXPIRY = "extensions:roster:config:cache:user-rosters:expiry";
        private const string CONFIGURATION_KEY_CONTACTS_CACHE_EXPIRY = "extensions:roster:config:cache:user-contacts:expiry";

        #endregion

        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfigurationHandler _config;
        
        /// <summary>
        /// The in-memory roster cache
        /// </summary>
        private readonly ICache _rosterCache;

        /// <summary>
        /// The roster cache
        /// </summary>
        private readonly ICache _rosterCacheExternal;

        /// <summary>
        /// The roster contact cache
        /// </summary>
        private readonly ICache _rosterContactCache;

        /// <summary>
        /// The external roster contact cache
        /// </summary>
        private readonly ICache _rosterContactCacheExternal;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RosterHandler"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public RosterHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
            _config = _dependencyUtility.Resolve<IConfigurationHandler>();

            ICacheHandler cacheHandler = _dependencyUtility.Resolve<ICacheHandler>();
            cacheHandler.AddCache("roster-cache-redis", _config.Get<CacheService>(CONFIGURATION_KEY_ROSTER_CACHE_SERVICE), _config.Get<int>(CONFIGURATION_KEY_ROSTER_CACHE_EXPIRY), _config.Get<string>(CONFIGURATION_KEY_ROSTER_CACHE_CONNECTIONSTRING));
            cacheHandler.AddCache("contact-cache-redis", _config.Get<CacheService>(CONFIGURATION_KEY_CONTACT_CACHE_SERVICE), _config.Get<int>(CONFIGURATION_KEY_CONTACTS_CACHE_EXPIRY), _config.Get<string>(CONFIGURATION_KEY_CONTACTS_CACHE_CONNECTIONSTRING));
            cacheHandler.AddCache("roster-cache", CacheService.InMemory, 60);
            cacheHandler.AddCache("contact-cache", CacheService.InMemory, 60);

            _rosterCache = cacheHandler.GetCache("roster-cache");
            _rosterCacheExternal = cacheHandler.GetCache("roster-cache-redis");
            _rosterContactCache = cacheHandler.GetCache("contact-cache");
            _rosterContactCacheExternal = cacheHandler.GetCache("contact-cache-redis");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the roster.
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns></returns>
        public Roster GetRoster(string entityId)
        {
            Roster roster = GetFromInMemoryCache(entityId) ?? GetRosterFromCache(entityId);

            if (roster != null)
                return roster;

            roster = GetRosterFromDb(entityId);

            if (roster == null)
                return null;

            UpdateRosterCache(roster);

            return roster;
        }

        /// <summary>
        /// Searches the rosters for a given contact id.
        /// </summary>
        /// <param name="contactId">The contact identifier.</param>
        /// <returns></returns>
        public IEnumerable<string> SearchEntitiesForContact(string contactId)
        {
            List<string> contacts = _rosterContactCache.Get<List<string>>(contactId) ?? _rosterContactCacheExternal.Get<List<string>>(contactId);

            if (contacts == null)
            { 
                contacts = GetContactsFromDb(contactId);
                
                _rosterContactCache.Set(contactId, contacts);
                _rosterContactCacheExternal.Set(contactId, contacts);
            }

            return contacts;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the roster from an in-memory cache
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        private Roster GetFromInMemoryCache(string entityId)
        {
            return _rosterCache.Get<Roster>(entityId);
        }

        /// <summary>
        /// Gets the roster from cache.
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns></returns>
        private Roster GetRosterFromCache(string entityId)
        {
            return _rosterCacheExternal.Get<Roster>(entityId);
        }

        /// <summary>
        /// Gets the roster from database.
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns></returns>
        private Roster GetRosterFromDb(string entityId)
        {
            const string SPROC_NAME = "sp_Rosters_GetRosterByEntityId";

            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EntityId", entityId);

                db.Open();
                IEnumerable<RosterEntry> entries = db.Query<RosterEntry>(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure);
                db.Close();

                return new Roster
                {
                    EntityId = entityId,
                    Entries = entries.ToArray()
                };
            }
        }

        private List<string> GetContactsFromDb(string contactId)
        {
            const string SPROC_NAME = "sp_Rosters_GetEntityIdsForContact";

            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ContactId", contactId);

                db.Open();
                IEnumerable<string> entries = db.Query<string>(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure);
                db.Close();

                return entries?.ToList();
            }
        }

        /// <summary>
        /// Updates the roster cache.
        /// </summary>
        /// <param name="roster">The roster.</param>
        private void UpdateRosterCache(Roster roster)
        {
            _rosterCache.Set(roster.EntityId, roster);
            _rosterCacheExternal.Set(roster.EntityId, roster);
        }

        #endregion
    }
}