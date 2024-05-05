using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models.AvailabilityController.Request
{
    public class AvailabilityPutRequest
    {
        public DateTime DayRequested { get; set; }
        public Availability[] Times { get; set; }
    }
}
