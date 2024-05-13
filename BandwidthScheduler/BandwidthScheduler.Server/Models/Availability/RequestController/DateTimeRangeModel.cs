using Microsoft.AspNetCore.Mvc;

namespace BandwidthScheduler.Server.Models.Availability.RequestController
{
    public class DateTimeRangeModel
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
