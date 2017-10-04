using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Common.Queues;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Contrib.Messaging.Models;
using Loki.SignalServer.Contrib.Messaging.Models.Client;
using Loki.SignalServer.Interfaces.Cache;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Router;
using RabbitMQ.Client;

namespace Loki.SignalServer.Contrib.Messaging.Handlers
{
    public class MessagingHandler
    {
        private const string CONFIGURATION_KEY_SQL_HOST = "extensions:messaging:config:sql:connection-string";
        private const string CONFIGURATION_KEY_ATTENDEE_CACHE_HOST = "extensions:messaging:config:cache:connection-string";
        private const string CONFIGURATION_KEY_ATTENDEE_CACHE_EXPIRY = "extensions:messaging:config:cache:expiry";
        private const string CONFIGURATION_KEY_QUEUE_HOST = "extensions:messaging:config:messaging-queue:host";
        private const string CONFIGURATION_KEY_QUEUE_VHOST = "extensions:messaging:config:messaging-queue:vhost";
        private const string CONFIGURATION_KEY_QUEUE_USERNAME = "extensions:messaging:config:messaging-queue:username";
        private const string CONFIGURATION_KEY_QUEUE_PASSWORD = "extensions:messaging:config:messaging-queue:password";

        private readonly IDependencyUtility _dependencyUtility;
        private readonly IConfigurationHandler _config;
        private readonly ILogger _logger;
        private readonly ISignalRouter _router;
        
        //private readonly ICache _attendeeCache;
        //private readonly ICache _attendeeCacheExternal;

        private IEventedQueueHandler<ISignal> _queueHandler;
        private readonly ICache _queueCache;

        public MessagingHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;

            _config = _dependencyUtility.Resolve<IConfigurationHandler>();
            _logger = _dependencyUtility.Resolve<ILogger>();
            _router = _dependencyUtility.Resolve<ISignalRouter>();

            ICacheHandler cacheHandler = _dependencyUtility.Resolve<ICacheHandler>();
            cacheHandler.AddCache("attendee-cache-inmem", CacheService.InMemory, 60);
            cacheHandler.AddCache("attendee-cache-redis", CacheService.Redis, _config.Get<int>(CONFIGURATION_KEY_ATTENDEE_CACHE_EXPIRY), _config.Get(CONFIGURATION_KEY_ATTENDEE_CACHE_HOST));

            //_attendeeCache = cacheHandler.GetCache("attendee-cache-inmem");
            //_attendeeCacheExternal = cacheHandler.GetCache("attendee-cache-redis");

            cacheHandler.AddCache("messaging-queue-cache", CacheService.InMemory, Int32.MaxValue);
            _queueCache = cacheHandler.GetCache("messaging-queue-cache");
        }
        

        public Guid CreateRoom(string entityId)
        {
            if (_queueHandler == null)
            { 
                _queueHandler = new EventedQueueHandler<ISignal>(_dependencyUtility);
                _queueHandler.Start();
            }

            Guid roomId = Guid.NewGuid();

            GenerateRoomQueueAndExchange(entityId, roomId);

            //CreateRoomFromDatabase(roomId);

            return roomId;
        }

        private IEventedQueue<ISignal> GenerateRoomQueueAndExchange(string entityId, Guid roomId)
        {
            IEventedQueueParameters parameters = GenerateRoomQueueAndExchangeParameters(entityId, roomId);
            IEventedQueue<ISignal> queue = _queueHandler.CreateQueue(parameters);

            _queueCache.Set(roomId.ToString(), queue);

            return queue;
        }

        private IEventedQueueParameters GenerateRoomQueueAndExchangeParameters(string entityId, Guid roomId)
        {
            IEventedQueueParameters parameters = new EventedQueueParameters();
            parameters["Host"] = _config.Get(CONFIGURATION_KEY_QUEUE_HOST);
            parameters["VirtualHost"] = _config.Get(CONFIGURATION_KEY_QUEUE_VHOST);
            parameters["Username"] = _config.Get(CONFIGURATION_KEY_QUEUE_USERNAME);
            parameters["Password"] = _config.Get(CONFIGURATION_KEY_QUEUE_PASSWORD);
            parameters["ExchangeId"] = roomId;
            parameters["ExchangeType"] = "topic";
            parameters["QueueId"] = entityId;
            parameters["RouteKey"] = roomId.ToString();
            parameters["Durable"] = true;
            parameters["Transient"] = false;
            parameters["AutoDelete"] = true;

            return parameters;
        }

        public bool JoinRoom(Guid roomId, string entityId)
        {
            IEventedQueue<ISignal> queue = _queueCache.Get<IEventedQueue<ISignal>>(roomId.ToString()) ?? GenerateRoomQueueAndExchange(entityId, roomId);
            queue.Dequeued += (sender, signal) =>
            {
                _router.BroadcastSignal(entityId, signal);
            };

            return true;
            //return JoinRoomFromCache(roomId, entityId) && JoinRoomFromDatabase(roomId, entityId);
        }

        public void LeaveRoom(Guid roomId, string entityId)
        {
            IEventedQueue<ISignal> queue = _queueCache.Get<IEventedQueue<ISignal>>(roomId.ToString()) ?? GenerateRoomQueueAndExchange(entityId, roomId);
            
            //LeaveRoomFromCache(roomId, entityId);
            //LeaveRoomFromDatabase(roomId, entityId);
        }

        public void SendMessage(ISignal signal)
        {
            RoomMessage message = signal.ResolvePayload<RoomMessage>();
            message.Timestamp = DateTime.UtcNow;
            message.Sender = signal.Sender;

            IEventedQueue<ISignal> queue = _queueCache.Get<IEventedQueue<ISignal>>(message.RoomId.ToString()) ?? GenerateRoomQueueAndExchange(signal.Sender, message.RoomId);
            queue.Enqueue(signal);

            ////Get Room Attendees
            //IEnumerable<string> entities = GetActiveRoomEntities(message.RoomId);

            //if (entities == null || !entities.Any())
            //    return;

            //_router.BroadcastSignal(entities, signal);
        }

        //public List<string> GetActiveRoomEntities(Guid roomId)
        //{
        //    return GetActiveRoomEntitiesFromCache(roomId) ?? GetActiveRoomEntitiesFromDatabase(roomId);
        //}

        #region Data Access

        private void CreateRoomFromDatabase(Guid roomId)
        {
            const string SPROC_NAME = "sp_Messaging_CreateRoom";

            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RoomId", roomId.ToString());

                db.Open();
                db.Query(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure);
                db.Close();
            }
        }

        //private bool JoinRoomFromCache(Guid roomId, string entityId)
        //{
        //    string key = roomId.ToString();
        //    List<string> entities = _attendeeCache.Get<List<string>>(key) ?? _attendeeCacheExternal.Get<List<string>>(key);

        //    if (entities == null)
        //        entities = new List<string> { entityId };
        //    else
        //    {
        //        if (entities.Contains(entityId))
        //            return false;
        //        entities.Add(entityId);
        //    }

        //    _attendeeCache.Set(key, entities);
        //    _attendeeCacheExternal.Set(key, entities);

        //    return true;
        //}

        private bool JoinRoomFromDatabase(Guid roomId, string entityId)
        {
            const string SPROC_NAME = "sp_Messaging_JoinRoom";

            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EntityId", entityId);
                parameters.Add("@RoomId", roomId.ToString());

                db.Open();
                db.Query(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure);
                db.Close();
            }

            return true;
        }

        //private void LeaveRoomFromCache(Guid roomId, string entityId)
        //{
        //    string key = roomId.ToString();
        //    List<string> entities = _attendeeCacheExternal.Get<List<string>>(key);

        //    if (!entities.Any())
        //        return;
            
        //    if (entities.Contains(entityId))
        //        entities.RemoveAll(x => x == entityId);

        //    _attendeeCache.Set(key, entities);
        //    _attendeeCacheExternal.Set(key, entities);
        //}

        private void LeaveRoomFromDatabase(Guid roomId, string entityId)
        {
            const string SPROC_NAME = "sp_Messaging_LeaveRoom";

            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RoomId", roomId);
                parameters.Add("@EntityId", entityId);

                db.Open();
                db.Query(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure);
                db.Close();
            }
        }

        //private List<string> GetActiveRoomEntitiesFromCache(Guid roomId)
        //{
        //    string key = roomId.ToString();

        //    return _attendeeCache.Get<List<string>>(key) ?? _attendeeCacheExternal.Get<List<string>>(key);
        //}

        private List<string> GetActiveRoomEntitiesFromDatabase(Guid roomId)
        {
            const string SPROC_NAME = "sp_Messaging_GetAttendeesByRoomId";
            List<string> entities = null;

            using (IDbConnection db = new SqlConnection(_config.Get(CONFIGURATION_KEY_SQL_HOST)))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RoomId", roomId.ToString());

                db.Open();
                entities = db.Query<string>(SPROC_NAME, parameters, commandType: CommandType.StoredProcedure)?.ToList();
                db.Close();
            }

            return entities;
        }

        #endregion
    }
}
