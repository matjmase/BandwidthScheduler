namespace BandwidthScheduler.Server.Models.PublishController.Request
{
    public class ScheduleRecallRequest
    {
        public int TeamId { get; set; } 
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
