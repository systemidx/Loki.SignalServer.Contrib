namespace Loki.SignalServer.Contrib.Rosters.Models
{
    public class Roster
    {
        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        /// <value>
        /// The entity identifier.
        /// </value>
        public string EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entries.
        /// </summary>
        /// <value>
        /// The entries.
        /// </value>
        public RosterEntry[] Entries { get; set; }
    }
}