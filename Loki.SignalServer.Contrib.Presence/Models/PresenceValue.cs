using System.Runtime.Serialization;
namespace Loki.SignalServer.Contrib.Presence.Models
{
    public enum PresenceValue
    {
        [EnumMember(Value = "0")]
        Offline = 0,
        [EnumMember(Value = "1")]
        Available = 1,
        [EnumMember(Value = "2")]
        Busy = 2,
        [EnumMember(Value = "3")]
        Away = 3,
        [EnumMember(Value = "4")]
        Invisible = 4,
        [EnumMember(Value = "5")]
        TakingAnAssessment = 5
    }
}
