using System;
using System.Collections.Generic;

namespace Loki.SignalServer.Contrib.Messaging.Models
{
    public struct RoomAttendees
    {
        public Guid RoomId { get; set; }
        public List<string> Entities { get; set; }
    }
}
