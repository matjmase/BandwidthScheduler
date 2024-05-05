namespace BandwidthScheduler.Server.Models.PublishController.Request
{
    public class ScheduleProposalAmount
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Employees { get; set; }
    }
}
