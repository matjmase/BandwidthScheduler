using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models.AvailabilityController.Request
{
    public class AvailabilityPutRequest
    {
        public DateTime DayRequested { get; set; }
        public DbModels.Availability[] Times { get; set; }
    }
}
