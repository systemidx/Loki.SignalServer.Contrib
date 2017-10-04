using System;

namespace Loki.SignalServer.Contrib.Messaging.Models
{
    public class Attendee
    {
        public Guid RoomId { get; set; }
        public string EntityId { get; set; }
        public DateTime TimestampJoined { get; set; }
        public DateTime TimestampLeft { get; set; }
    }
}