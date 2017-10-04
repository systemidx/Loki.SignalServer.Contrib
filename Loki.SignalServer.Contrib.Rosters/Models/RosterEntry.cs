namespace Loki.SignalServer.Contrib.Rosters.Models
{
    public class RosterEntry
    {
        /// <summary>
        /// Gets or sets the contact identifier.
        /// </summary>
        /// <value>
        /// The contact identifier.
        /// </value>
        public string ContactId { get; set; }

        /// <summary>
        /// Gets or sets the roster group.
        /// </summary>
        public string RosterGroupName { get; set; }
    }
}
