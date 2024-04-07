using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models
{
    public class AvailabilityPutRequest
    {
        public DateTime DayRequested { get; set; }   
        public Availability[] Times { get; set; }   
    }
}
