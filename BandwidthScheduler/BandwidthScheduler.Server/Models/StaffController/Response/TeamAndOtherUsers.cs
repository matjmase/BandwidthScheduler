using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models.StaffController.Response
{
    public class TeamAndOtherUsers
    {
        public User[] TeamUsers { get; set; }
        public User[] AllOtherUsers { get; set; }
    }
}
