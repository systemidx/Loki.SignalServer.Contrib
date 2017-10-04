using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Contrib.Metadata.Models;
using Loki.SignalServer.Interfaces.Cache;
using Loki.SignalServer.Interfaces.Configuration;
using Newtonsoft.Json;

namespace Loki.SignalServer.Contrib.Metadata.Handlers
{
    public class MetadataHandler
    {
        /// <summary>
        /// The configuration key SQL host
        /// </summary>
        private const string CONFIGURATION_KEY_SQL_HOST = "extensions:metadata:config:sql:connection-string";

        /// <summary>
        /// The configuration key cache service
        /// </summary>
        private const string CONFIGURATION_KEY_CACHE_SERVICE = "extensions:metadata:config:cache:service";

        /// <summary>
        /// The configuration key cache connection string
        /// </summary>
        private const string CONFIGURATION_KEY_CACHE_CONNECTIONSTRING = "extensions:metadata:config:cache:connection-string";

        /// <summary>
        /// The configuration key cache expiry
        /// </summary>
        private const string CONFIGURATION_KEY_CACHE_EXPIRY = "extensions:metadata:config:cache:expiry";

        private readonly IDependencyUtility _dependencyUtility;
        private readonly IConfigurationHandler _config;
        private readonly ICacheHandler _cacheHandler;
        private readonly ICache _metadataCache;
        private readonly ILogger _logger;

        public MetadataHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
            _config = _dependencyUtility.Resolve<IConfigurationHandler>();
            _cacheHandler = _dependencyUtility.Resolve<ICacheHandler>();
            _logger = _dependencyUtility.Resolve<ILogger>();

            _cacheHandler.AddCache("metadata-cache", _config.Get<CacheService>(CONFIGURATION_KEY_CACHE_SERVICE), _config.Get<int>(CONFIGURATION_KEY_CACHE_EXPIRY), _config.Get<string>(CONFIGURATION_KEY_CACHE_CONNECTIONSTRING));
            _metadataCache = _cacheHandler.GetCache("metadata-cache");
        }

        public UserMetadata GetMetadata(string entityId)
        {
            UserMetadata metadata = GetMetadataFromCache(entityId);
            if (metadata != null)
                return metadata;

            metadata = GetMetadataFromDb(entityId);
            if (metadata == null)
                return null;

            UpdateMetadataCache(entityId, metadata);

            return metadata;
        }

        private UserMetadata GetMetadataFromCache(string entityId)
        {
            return _metadataCache.Get<UserMetadata>(entityId);
        }

        private UserMetadata GetMetadataFromDb(string entityId)
        {
            const string SPROC_NAME = "sp_Metadata_GetMetadataByEntityId";

            IEnumerable<string> entries;
            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EntityId", entityId);

                db.Open();
                entries = db.Query<string>(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure);
                db.Close();
            }

            string[] metadata = entries as string[] ?? entries.ToArray();

            if (!metadata.Any())
            {
                _logger.Warn($"Requested Metadata for {entityId} and none was received from the database.");
                return new UserMetadata { Username = entityId };
            }

            try
            {
                return JsonConvert.DeserializeObject<UserMetadata>(metadata[0]);
            }
            catch (JsonSerializationException ex)
            {
                _logger.Error(ex);
            }

            return null;
        }

        private void UpdateMetadataCache(string entityId, UserMetadata metadata)
        {
            _metadataCache.Set(entityId, metadata);
        }
    }
}
