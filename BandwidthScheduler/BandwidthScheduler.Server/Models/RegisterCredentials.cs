using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models
{
    public class RegisterCredentials : LoginCredentials
    {
        public string[] Roles { get; set; }

        public User ToDbUser()
        {
            return new User()
            {
                Email = Email,
            };
        }
    }
}
