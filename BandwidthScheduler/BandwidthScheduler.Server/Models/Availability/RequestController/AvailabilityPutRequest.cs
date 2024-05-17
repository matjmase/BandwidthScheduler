using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.Availability.RequestController;

namespace BandwidthScheduler.Server.Models.AvailabilityController.Request
{
    public class AvailabilityPutRequest
    {
        public DateTimeRangeModel RangeRequested { get; set; }
        public DbModels.Availability[] Times { get; set; }
    }
}
