using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models
{
    public class StaffTeamChangeRequest
    {
        public Team CurrentTeam { get; set; }
        public User[] ToAdd { get; set; }
        public User[] ToRemove { get; set; }
    }
}
