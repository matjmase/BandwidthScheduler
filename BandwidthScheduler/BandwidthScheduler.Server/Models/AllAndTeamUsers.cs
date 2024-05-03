using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models
{
    public class AllAndTeamUsers
    {
        public User[] TeamUsers { get; set; }
        public User[] AllOtherUsers { get; set; }
    }
}
