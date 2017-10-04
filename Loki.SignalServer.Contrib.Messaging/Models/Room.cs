using System;

namespace Loki.SignalServer.Contrib.Messaging.Models
{
    public class Room
    {
        public Guid RoomId { get; set; }
        public bool IsActive { get; set; }
        public DateTime TimestampCreated { get; set; }
        public DateTime TimestampClosed { get; set; }
    }
}
