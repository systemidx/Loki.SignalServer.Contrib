using System;
using System.Collections.Generic;
using System.Text;

namespace Loki.SignalServer.Contrib.Presence.Models
{
    public class UserPresence
    {
        public string EntityId { get; set; }
        public PresenceValue PresenceValue { get; set; }
    }
}
