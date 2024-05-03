namespace BandwidthScheduler.Server.Models
{
    public class ScheduleProposal
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Employees { get; set; }
    }
}
