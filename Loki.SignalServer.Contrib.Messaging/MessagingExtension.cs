using System;
using System.Collections.Generic;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Contrib.Messaging.Handlers;
using Loki.SignalServer.Contrib.Messaging.Models;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Contrib.Messaging
{
    public class MessagingExtension : Extension
    {
        private MessagingHandler _messagingHandler;
        private ISignalRouter _router;

        public MessagingExtension(string extensionName, IDependencyUtility dependencyUtility) : base(extensionName, dependencyUtility)
        {
            this.RegisterAction("CreateRoom", CreateRoom);
            this.RegisterAction("JoinRoom", JoinRoom);
            this.RegisterAction("LeaveRoom", LeaveRoom);
            this.RegisterAction("InviteToRoom", InviteToRoom);
            this.RegisterAction("SendMessage", SendMessage);
        }

        public override void Initialize()
        {
            _messagingHandler = new MessagingHandler(DependencyUtility);
            _router = DependencyUtility.Resolve<ISignalRouter>();

            base.Initialize();
        }

        private ISignal InviteToRoom(ISignal signal)
        {
            Guid roomId = signal.ResolvePayload<Guid>();

            return null;
        }

        private ISignal LeaveRoom(ISignal signal)
        {
            Guid roomId = signal.ResolvePayload<Guid>();

            _messagingHandler.LeaveRoom(roomId, signal.Sender);
            
            BroadcastLatestRoomAttendees(signal, roomId);

            return CreateResponse(signal, true);
        }

        private ISignal SendMessage(ISignal signal)
        {
            _messagingHandler.SendMessage(signal);

            return null;
        }

        private ISignal JoinRoom(ISignal signal)
        {
            Guid roomId = signal.ResolvePayload<Guid>();

            bool isJoined = _messagingHandler.JoinRoom(roomId, signal.Sender);
            if (!isJoined)
                return null;

            BroadcastLatestRoomAttendees(signal, roomId);

            return CreateResponse(signal, true);
        }

        private ISignal CreateRoom(ISignal signal)
        {
            Guid roomId = _messagingHandler.CreateRoom(signal.Sender);

            return CreateResponse(signal, roomId);
        }
        

        public override void RegisterConnection(IWebSocketConnection connection)
        {
        }

        public override void UnregisterConnection(IWebSocketConnection connection)
        {
        }

        private void BroadcastLatestRoomAttendees(ISignal signal, Guid roomId)
        {
            List<string> entities = _messagingHandler.GetActiveRoomEntities(roomId);
            _router.BroadcastSignal(entities, CreateResponse(signal, new RoomAttendees { Entities = entities, RoomId = roomId }, "RoomAttendeesUpdated"));
        }
    }
}
